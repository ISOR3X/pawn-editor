using System;
using System.Collections.Generic;
using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class DefTableWorker_Hair(DefTableDef def, Func<IEnumerable<Def>> thingsGetter, Def? defaultThing = null)
    : DefTableWorker(def, thingsGetter, defaultThing)
{
    protected override void OnSelectChanged(Def? thing)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p == null) return;
        switch (thing)
        {
            case HairDef hairDef:
                AppearanceUtility.TrySetHairFor(hairDef, p);
                break;
            case BeardDef beardDef:
                AppearanceUtility.TrySetBeardFor(beardDef, p);
                break;
            case TattooDef tattooDef:
                AppearanceUtility.TrySetTattooFor(tattooDef, p);
                break;
        }
    }

    protected override void DoRowHover(Rect inRect, Def thing)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p == null) return;
        UIUtility.DefIconPreview(inRect, thing, p.story.HairColor);
    }
}