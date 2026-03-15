using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Edit : ColumnWorker_Icon<Thing>
{
    protected override void DrawIcon(Rect inRect, Thing thing, TableWorker<Thing> table)
    {
        var iconRect = new Rect(table.BoundRect.x, inRect.y, table.BoundRect.width, inRect.height);
        if (Verse.Widgets.ButtonImage(inRect, TexButton.Add, tooltip: "Edit item."))
        {
            FloatWindow.ToggleState(iconRect, () => new FloatWindow_EditThing(iconRect, thing));
        }
    }
}