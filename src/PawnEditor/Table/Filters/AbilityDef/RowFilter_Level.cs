using Taffy;
using Verse;

namespace PawnEditor.Table.Filters.AbilityDef;

[StaticConstructorOnStartup]
public class RowFilter_Level : IRowFilter<RimWorld.AbilityDef>
{
    private IntRange _range = new(MinMaxRange.min, MinMaxRange.max);
    private static readonly IntRange MinMaxRange;

    static RowFilter_Level()
    {
        var levels = DefDatabase<RimWorld.AbilityDef>.AllDefsListForReading.Select(d => d.level).ToArray();
        MinMaxRange = new IntRange(levels.Min(), levels.Max());
    }

    public bool Passes(RimWorld.AbilityDef row, ITableContext? ctx) =>
        row.level >= _range.min && row.level <= _range.max;

    public void DrawFilter(TaffyBuilder builder, Table<RimWorld.AbilityDef> table)
    {
        builder.Text("Level range", GameFont.Tiny);
        builder.Item(
            new Style
            {
                size = new Size<Dimension>(Dimension.Percent(1f), UIUtility.ButtonHeight),
                margin = new Rect<LengthPercentageAuto>(0f, 0f, 0f, GenUI.GapSmall)
            },
            r =>
            {
                var prev = _range;
                Verse.Widgets.IntRange(r, 5174, ref _range, MinMaxRange.min, MinMaxRange.max);
                if (_range != prev) table.SetDirty();
            });
    }
}