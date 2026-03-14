using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ThingTableWorker_Apparel(
    ThingTableDef def,
    Func<IEnumerable<Thing>> thingsGetter,
    Thing? defaultThing = null)
    : ThingTableWorker(def, thingsGetter, defaultThing)
{
    protected override void OnSelectChanged(Thing thing)
    {
        // var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        // if (pawn == null || thing is not Apparel apparel) return;
        // pawn.apparel.Remove(apparel);
        // SetDirty();
    }

    protected override void DoRowHover(Rect inRect, Thing thing)
    {
        base.DoRowHover(inRect, thing);
        if (Mouse.IsOver(inRect))
            TooltipHandler.TipRegion(inRect, thing.GetTooltip());
    }

    protected override void OnRowClicked(Thing thing)
    {
        return;
    }
}