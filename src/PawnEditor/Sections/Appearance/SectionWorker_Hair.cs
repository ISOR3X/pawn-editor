using HotSwap;
using PawnEditor.Table;
using PawnEditor.Table.ColumnWorkers;
using RimWorld;
using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_Hair<T>(SectionDef def) : SectionWorker(def) where T : StyleItemDef
{
    protected abstract List<T> TableItems { get; }
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
                ColumnWorker<T>.Create<PawnContext>(
                    Void.Taffy.Px(36f),
                    (grid, def, ctx) => grid.Icon(def.Icon, ctx.Value.story.HairColor, UIUtility.ComponentSize.Large)
                ),
                ColumnWorker<T>.CreateText(
                    Void.Taffy.Fr(3), def => def.LabelCap, "Label"
                ),
                new ColumnWorker_Gender<T>(),
                ColumnWorker<T>.CreateText(Void.Taffy.Fr(), def => def.StyleItemCategory.LabelCap, "Style"),
                ColumnWorker<T>.CreateText(
                    Void.Taffy.Fr(),
                    def => def.modContentPack.Name,
                    "Source",
                    color: ColoredText.SubtleGrayColor
                ),
            ],
            onRowClick: row => onRowClick?.Invoke(row),
            onRowHover: (rowRect, styleItemDef, ctx) =>
            {
                if (ctx is not PawnContext pawnContext) return;
                UIUtility.DefIconPreview(rowRect, styleItemDef, pawnContext.Value.story.HairColor);
            },
            context: new PawnContext(pawn),
            filters: [new RowFilter_DefContentSource<T>()],
            searchProjection: def => def.LabelCap
        );
    }

    private void DrawTable(TaffyBuilder builder, List<T> items, Pawn pawn, Action<T?>? onRowClick = null)
    {
        builder.Item(GetCachedTable(pawn, items, onRowClick).Draw,
            new StyleOverride { minWidth = 400, minHeight = 400 , width = Dimension.Percent(1)});
    }

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text(TableTitle, color: ColoredText.TipSectionTitleColor);
        DrawTable(builder, TableItems, pawn);
    }
}

public class SectionWorker_Hair(SectionDef def) : SectionWorker_Hair<HairDef>(def)
{
    protected override List<HairDef> TableItems => DefDatabase<HairDef>.AllDefsListForReading;
    protected override string TableTitle => "Hair";
}