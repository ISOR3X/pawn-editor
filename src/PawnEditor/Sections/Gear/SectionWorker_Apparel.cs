using HotSwap;
using PawnEditor.Table;
using PawnEditor.Table.ColumnWorkers;
using RimWorld;
using Taffy;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_Apparel<T>(SectionDef def) : SectionWorker(def) where T : Thing
{
    protected abstract Func<Pawn, List<T>> TableItems { get; }
    protected abstract string TableTitle { get; }

    private Table<T>? _cachedTable;
    private Pawn? _cachedPawn;

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
            rows: defs,
            columns:
            [
                ColumnWorker<T>.Create(
                    Taffy.Px(GenUI.SmallIconSize),
                    (grid, thing) => grid.Item(r => Verse.Widgets.ThingIcon(r, thing),
                        new StyleOverride { width = GenUI.SmallIconSize, height = GenUI.SmallIconSize })
                ),
                ColumnWorker<T>.CreateText(
                    Taffy.Fr(), thing => thing.LabelCap, "Label"
                ),
                new ColumnWorker_ThingStuff<T>(Taffy.Fr()),
                new ColumnWorker_ThingMass<T>(Taffy.Fr()),
                ColumnWorker<T>.Create(Taffy.Px(GenUI.SmallIconSize),
                    (builder, thing) =>
                    {
                        builder.Button(icon: TexButton.NewItem,
                            onClick: r =>
                            {
                                FloatWindow.ToggleState(r,
                                    () => new FloatWindow_EditThing(r, thing,
                                        Find.WindowStack.WindowOfType<Window_Editor>()));
                            });
                    }),
                ColumnWorker<T>.Create(Taffy.Px(GenUI.SmallIconSize),
                    (builder, thing) => { builder.Item(r => Verse.Widgets.InfoCardButtonCentered(r, thing)); })
            ],
            onRowClick: row => onRowClick?.Invoke(row),
            onRowHover: (rowRect, thing, _) => { TooltipHandler.TipRegion(rowRect, thing.GetTooltip()); },
            filters: [],
            searchProjection: def => def.LabelCap
        );
    }

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text(TableTitle, color: ColoredText.TipSectionTitleColor);
        GetCachedTable(pawn, TableItems(pawn)).Draw(builder);
    }
}

public class SectionWorker_Apparel(SectionDef def) : SectionWorker_Apparel<Apparel>(def)
{
    protected override Func<Pawn, List<Apparel>> TableItems => p => p.apparel.WornApparel;
    protected override string TableTitle => "Apparel";
}