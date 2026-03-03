using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefTable_Hair : DefTable
{
    public DefTable_Hair(TableDef def, Func<IEnumerable<Def>> thingsGetter, Def defaultThing) : base(def, thingsGetter, defaultThing)
    {
    }

    protected override void OnSelectChanged(Def thing)
    {
        if (Window_Editor.GetSelectedPawn() != null)
        {
            if (thing is HairDef hairDef)
            {
                AppearanceUtility.TrySetHairFor(hairDef, Window_Editor.GetSelectedPawn());
            }
            else if (thing is BeardDef beardDef)
            {
                AppearanceUtility.TrySetBeardFor(beardDef, Window_Editor.GetSelectedPawn());
            }
        }
    }

    protected override void DoRowHover(Rect inRect, Def thing)
    {
        UIUtility.DefIconPreview(inRect, thing, Window_Editor.GetSelectedPawn()?.story.hairColor);
    }
}