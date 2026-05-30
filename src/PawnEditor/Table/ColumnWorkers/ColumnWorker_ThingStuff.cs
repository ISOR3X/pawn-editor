using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table.ColumnWorkers;

public class ColumnWorker_ThingStuff<T>(TaffyTrackSizingFunction trackSize) : ColumnWorker<T> where T : Thing
{
    protected override string HeaderLabel => "Stuff";
    public override TaffyTrackSizingFunction TrackSize => trackSize;

    public override bool Sortable => true;

    public override int Compare(T a, T b)
    {
        return string.Compare(a.Stuff?.LabelCap, b.Stuff?.LabelCap, StringComparison.CurrentCultureIgnoreCase);
    }

    public override void DrawCell(TaffyBuilder grid, T thing)
    {
        if (thing.def.stuffCategories == null || thing.Stuff == null)
            grid.Text("No stuff", color: ColoredText.SubtleGrayColor);
        else
            // Only a single child allowed per column.
            grid.Div(inner =>
            {
                var t = thing.Stuff.LabelCap;
                inner.Item(r => Verse.Widgets.ThingIcon(r, thing.Stuff),
                    new StyleOverride
                    {
                        margin = new TaffyEdges(Dimension.Px(0), Dimension.Px(GenUI.GapSmall), Dimension.Px(0), Dimension.Px(0)),
                        width = Dimension.Px(GenUI.SmallIconSize)
                    });
                inner.Text(t, wrap: false,
                    onHover: r =>
                    {
                        if (Text.CalcSize(t).x > r.width)
                            TooltipHandler.TipRegion(r, t);
                    });
            });
    }
}