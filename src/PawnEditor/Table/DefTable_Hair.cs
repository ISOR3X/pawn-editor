using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefTable_Hair(TableDef def, Func<IEnumerable<Def>> thingsGetter, Def defaultThing)
    : DefTable(def, thingsGetter, defaultThing)
{
    protected override void OnSelectChanged(Def thing)
    {
        var p = Window_Editor.GetSelectedPawn();
        if (p == null) return;
        switch (thing)
        {
            case HairDef hairDef:
                AppearanceUtility.TrySetHairFor(hairDef, p);
                break;
            case BeardDef beardDef:
                AppearanceUtility.TrySetBeardFor(beardDef, p);
                break;
        }
    }

    protected override void DoRowHover(Rect inRect, Def thing)
    {
        var p = Window_Editor.GetSelectedPawn();
        if (p == null) return;
        UIUtility.DefIconPreview(inRect, thing, p.story.HairColor);
    }
}