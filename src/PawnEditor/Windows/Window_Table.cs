using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
// TODO: Make generic.
public class Window_Table : Window
{
    protected virtual DefTableDef TableDef => TableDefOf.PawnEditor_ThingDef;
    public override Vector2 InitialSize => new(800f, 500f);

    private static IEnumerable<Def> Apparel => DefDatabase<ThingDef>.AllDefs
        .Where(td => td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult)).ToList();

    private DefTableWorker Table => field ??= (DefTableWorker)Activator.CreateInstance(
        TableDef.workerClass, TableDef, (Func<IEnumerable<Def>>)(() => Apparel), null);

    public override void DoWindowContents(Rect inRect)
    {
        var leftPart = inRect.TakeLeftPart(200f);
        var listing = new Listing_Standard();
        listing.Begin(leftPart);
        DoFilters(listing);
        listing.End();
        
        inRect.Indent();

        Table.TableOnGUI(inRect);
    }

    protected virtual void DoFilters(Listing_Standard listing)
    {
    }
}