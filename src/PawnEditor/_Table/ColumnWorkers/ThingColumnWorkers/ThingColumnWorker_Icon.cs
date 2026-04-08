using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ThingColumnWorker_Icon : ThingColumnWorker
{
    protected override void DrawCellContent(Rect r, Thing row)
    {
        Verse.Widgets.ThingIcon(r.ContractedBy(2f), row);
    }
}
