using System;
using System.Collections.Generic;
using Verse;

namespace PawnEditor;

public class DefTable_Tattoos : DefTable
{
    public DefTable_Tattoos(TableDef def, Func<IEnumerable<Def>> thingsGetter, Def defaultThing) : base(def,
        thingsGetter, defaultThing)
    {
    }

    protected override void OnSelectChanged(Def thing)
    {
        if (Window_Editor.GetSelectedPawn() != null)
        {
        }
    }
}