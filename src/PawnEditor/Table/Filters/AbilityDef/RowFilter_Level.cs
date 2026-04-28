using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

[StaticConstructorOnStartup]
public class RowFilter_Level : RowFilter<RimWorld.AbilityDef>
{
    public static readonly IntRange MinMaxRange;
    private IntRange _range = new(MinMaxRange.min, MinMaxRange.max);

    static RowFilter_Level()
    {
        var levels = DefDatabase<RimWorld.AbilityDef>.AllDefsListForReading.Select(d => d.level).ToArray();
        MinMaxRange = new IntRange(levels.Min(), levels.Max());
    }

    public override bool Passes(RimWorld.AbilityDef row)
    {
        return row.level >= _range.min && row.level <= _range.max;
    }

    public override void DrawFilter(TaffyBuilder builder)
    {
        builder.Text("Level range", style: new StyleOverride { fontSize = GameFont.Tiny });
        builder.Item(
            r =>
            {
                var prev = _range;
                Verse.Widgets.IntRange(r, 5174, ref _range, MinMaxRange.min, MinMaxRange.max);
                if (_range != prev) MarkDirty();
            },
            new StyleOverride
            {
                width = Dimension.Percent(1f),
                height = UIUtility.ButtonHeight,
                margin = new Rect<LengthPercentageAuto>(0f, 0f, 0f, GenUI.GapSmall)
            }
        );
    }
}
