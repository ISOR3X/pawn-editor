using UnityEngine;
using Verse;

namespace PawnEditor;

public class ThingColumnWorker_Icon : ColumnWorker_Icon<Thing>
{
    protected override void DrawIcon(Rect inRect, Thing thing, TableWorker<Thing> table)
    {
        Verse.Widgets.ThingIcon(GetCellRect(inRect, table), thing);
    }
}