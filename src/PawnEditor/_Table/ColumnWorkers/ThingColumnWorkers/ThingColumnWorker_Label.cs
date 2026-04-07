using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ThingColumnWorker_Label : ThingColumnWorker
{
    public override bool Sortable => true;

    public override int Compare(Thing a, Thing b)
        => string.Compare(a.LabelCap, b.LabelCap, StringComparison.CurrentCultureIgnoreCase);

    protected override void DrawCellContent(Rect r, Thing row)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(r, row.LabelCap);
    }
}
