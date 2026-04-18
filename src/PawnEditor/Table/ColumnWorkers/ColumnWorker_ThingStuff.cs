using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table.ColumnWorkers;

public class ColumnWorker_ThingStuff<T>(TrackSizingFunction trackSize) : ColumnWorker<T> where T : Thing
{
    protected override string HeaderLabel => "Stuff";
    public override TrackSizingFunction TrackSize => trackSize;

    public override int Compare(T a, T b) =>
        string.Compare(a.Stuff?.LabelCap, b.Stuff?.LabelCap, StringComparison.CurrentCultureIgnoreCase);

    public override bool Sortable => true;

    public override void DrawCell(TaffyBuilder grid, T thing)
    {
        if (thing.def.stuffCategories == null || thing.Stuff == null)
        {
            grid.Text("No stuff", color: ColoredText.SubtleGrayColor);
        }
        else
        {
            // Only a single child allowed per column.
            grid.Div(inner =>
            {
                inner.Item(r => Verse.Widgets.ThingIcon(r, thing.Stuff),
                    new StyleOverride
                    {
                        margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapSmall, 0f, 0f),
                        width = GenUI.SmallIconSize
                    });
                inner.Text(thing.Stuff.LabelCap);
            });
        }
    }
}