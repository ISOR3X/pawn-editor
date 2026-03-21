using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class DefTableWorker(DefTableDef def, Func<IEnumerable<Def>> thingsGetter, Def? defaultThing = null)
    : TableWorker<Def>(def, thingsGetter, defaultThing)
{
    protected override IEnumerable<ColumnWorker<Def>> AllColumns => def.columns.Select(c => c.Worker);
    
    protected override void DoRowHover(Rect inRect, Def thing)
    {
        base.DoRowHover(inRect, thing);
        if (Mouse.IsOver(inRect))
            TooltipHandler.TipRegion(inRect, GetTooltipFor(thing));
    }

    private static string GetTooltipFor(Def thing)
    {
        var text = thing.LabelCap.Colorize(ColoredText.TipSectionTitleColor);

        if (thing.description != null)
        {
            text += "\n\n" + thing.description;
        }
        
        return text;
    }
}