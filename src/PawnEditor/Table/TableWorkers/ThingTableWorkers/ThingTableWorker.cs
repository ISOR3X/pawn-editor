using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor.Extensions;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ThingTableWorker(ThingTableDef def, Func<IEnumerable<Thing>> thingsGetter, Thing? defaultThing = null)
    : TableWorker<Thing>(def, thingsGetter, defaultThing)
{
    protected override IEnumerable<ColumnWorker<Thing>> AllColumns => def.columns.Select(c => c.Worker);

    protected override void DoRowHover(Rect inRect, Thing thing)
    {
        base.DoRowHover(inRect, thing);
        if (Mouse.IsOver(inRect))
            TooltipHandler.TipRegion(inRect, thing.GetTooltip());
    }

    protected override Rect DoFooter(Rect inRect)
    {
        var footerRect = base.DoFooter(inRect);

        if (Verse.Widgets.ButtonText(footerRect.TakeLeftPart(100f), "Add item"))
        {
            Find.WindowStack.Add(new Window_AddItem(
                new FilteredDefTableWorker(
                    TableDefOf.PawnEditor_DefTable_ThingDef,
                    () => DefDatabase<ThingDef>.AllDefs
                        .Where(td => td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult))
                        .Cast<Def>()
                        .ToList()),
                [
                    ("Content source", () => new DefTableFilter_ContentSource()),
                    ("Stuff category", () => new DefTableFilter_StuffCategory())
                ]
            ));
        }

        return footerRect;
    }
}