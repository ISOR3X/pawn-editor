using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Info : ColumnWorker_Icon<Thing>
{
    protected override void DrawIcon(Rect inRect, Thing thing, TableWorker<Thing> table)
    {
        Verse.Widgets.InfoCardButton(inRect.x, inRect.y + 3f, thing);
    }
}