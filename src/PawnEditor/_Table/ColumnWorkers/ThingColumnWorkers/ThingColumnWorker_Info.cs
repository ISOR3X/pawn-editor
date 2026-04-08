using HotSwap;
using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Info : ThingColumnWorker
{
    protected override void DrawCellContent(Rect r, Thing row)
    {
        Verse.Widgets.InfoCardButton(r.x, r.y + 3f, row);
    }
}
