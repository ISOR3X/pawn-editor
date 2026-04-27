using PawnEditor.Table;
using PawnEditor.Table.ColumnWorkers;
using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Col = PawnEditor.Table.ColumnWorker<Verse.ThingDef>;

namespace PawnEditor;

public abstract class SectionWorker_ThingTable<T>(SectionDef def) : SectionWorker(def) where T : Thing
{
    private Pawn? _cachedPawn;

    private Table<T>? _cachedTable;
    protected abstract Func<Pawn, List<T>> TableItems { get; }
    protected abstract string TableTitle { get; }


    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var table = GetCachedTable(pawn, TableItems(pawn));
        layout.ComponentById<TextElement>("text").Content = TableTitle;
        layout.ComponentById<DivElement>("table").Draw = r => table.Draw(r, false);
        layout.ComponentById<DivElement>("search").Draw = table.DrawSearchWidget;
        var btn = layout.ComponentById<ButtonElement>("add");
        btn.Label = $"Add {TableTitle.ToLower()}";
        btn.OnClick = _ => Find.WindowStack.Add(new Window_Table<ThingDef>(GetThingDefTable(),
            Find.WindowStack.WindowOfType<Window_Editor>(),
            selectedItemSlot: (b, i) => { b.Text("Selected: " + (i?.LabelCap ?? "None")); }));
    }

    private Table<T> GetCachedTable(Pawn pawn, List<T> defs, Action<T?>? onRowClick = null)
    {
        if (_cachedTable == null || !ReferenceEquals(_cachedPawn, pawn))
        {
            _cachedTable = ConstructStyleItemTable(pawn, defs, onRowClick);
            _cachedPawn = pawn;
        }

        return _cachedTable;
    }

    private static Table<T> ConstructStyleItemTable(Pawn pawn, List<T> defs, Action<T?>? onRowClick = null)
    {
        return new Table<T>(
            defs,
            [
                ColumnWorker<T>.Create(
                    Void.Taffy.Px(GenUI.SmallIconSize),
                    (grid, thing) => grid.Item(r => Verse.Widgets.ThingIcon(r, thing),
                        new StyleOverride { width = GenUI.SmallIconSize, height = GenUI.SmallIconSize })
                ),
                ColumnWorker<T>.CreateText(
                    Void.Taffy.Fr(2), thing => thing.LabelCap, "Label"
                ),
                new ColumnWorker_ThingStuff<T>(Void.Taffy.Fr()),
                new ColumnWorker_ThingMass<T>(Void.Taffy.Px(100f)),
                ColumnWorker<T>.Create(Void.Taffy.Px(GenUI.SmallIconSize),
                    (builder, thing) =>
                    {
                        builder.Button(icon: TexButton.NewItem,
                            onClick: r =>
                            {
                                FloatWindow.ToggleState(r,
                                    () => new FloatWindow_EditThing(r, thing,
                                        Find.WindowStack.WindowOfType<Window_Editor>()));
                            }, variant: TaffyExtensions.ButtonVariant.Ghost);
                    }),
                ColumnWorker<T>.Create(Void.Taffy.Px(GenUI.SmallIconSize),
                    (builder, thing) => { builder.Item(r => Verse.Widgets.InfoCardButtonCentered(r, thing)); })
            ],
            onRowClick: row => onRowClick?.Invoke(row),
            onRowHover: (rowRect, thing, _) => { TooltipHandler.TipRegion(rowRect, thing.GetTooltip()); },
            filters: [],
            searchProjection: def => def.LabelCap
        );
    }


    private static Table<ThingDef> GetThingDefTable()
    {
        return new Table<ThingDef>(
            [
                .. DefDatabase<ThingDef>.AllDefs.Where(td =>
                    td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult))
            ],
            [
                Col.Create(
                    Void.Taffy.Px(20f),
                    (grid, def) => { grid.Icon(def.uiIcon, iconColor: GetShowColorForDef(def)); }
                ),
                Col.CreateText(
                    Void.Taffy.Fr(),
                    def => def.LabelCap,
                    "Label"
                ),
                Col.CreateText(
                    Void.Taffy.Fr(),
                    def => def.modContentPack?.Name ?? "",
                    "Source",
                    ColoredText.SubtleGrayColor
                )
            ],
            filters: [new RowFilter_DefContentSource<ThingDef>()],
            onRowHover: (rowRect, def, _) => { UIUtility.DefIconPreview(rowRect, def, GetShowColorForDef(def), 0.6f); },
            searchProjection: def => def.label
        );
    }

    private static Color GetShowColorForDef(ThingDef def)
    {
        var color = Color.white;
        color = GenStuff.AllowedStuffsFor(def).FirstOrDefault()?.stuffProps.color ?? color;
        if (def.colorGenerator != null)
            color = def.colorGenerator.ExemplaryColor;
        return color;
    }
}