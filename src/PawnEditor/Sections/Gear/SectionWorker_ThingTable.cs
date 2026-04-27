using PawnEditor.Table;
using PawnEditor.Table.ColumnWorkers;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;

namespace PawnEditor;

public abstract class SectionWorker_ThingTable<T>(SectionDef def) : SectionWorker(def) where T : Thing
{
    private Pawn? _cachedPawn;

    private Table<T>? _cachedTable;
    protected abstract Func<Pawn, List<T>> TableItems { get; }
    protected abstract string TableTitle { get; }


    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = TableTitle;
        layout.ComponentById<TextElement>("text").Color = ColoredText.TipSectionTitleColor;
        layout.ComponentById<DivElement>("table").Draw = GetCachedTable(pawn, TableItems(pawn)).Draw;
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
}