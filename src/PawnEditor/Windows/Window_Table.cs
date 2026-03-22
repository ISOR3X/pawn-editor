using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
// TODO: Make generic.
public class Window_Table : Window
{
    protected virtual DefTableDef TableDef => TableDefOf.PawnEditor_ThingDef;
    public override Vector2 InitialSize => new(UI.screenWidth - 100f, 500f);

    private static IEnumerable<Def> Apparel => DefDatabase<ThingDef>.AllDefs
        .Where(td => td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult)).ToList();

    private List<TableFilter> _filters = [new DefTableFilter_ContentSource()];

    private DefTableWorker Table => field ??= (DefTableWorker)Activator.CreateInstance(
        TableDef.workerClass, TableDef, (Func<IEnumerable<Def>>)(() => Apparel), null);

    public override void DoWindowContents(Rect inRect)
    {
        var leftPart = inRect.TakeLeftPart(200f);
        var listing = new Listing_Standard();
        listing.Begin(leftPart);
        if (listing.ButtonText("Add filter"))
        {
        }

        foreach (var filter in _filters)
        {
            filter.DrawFilter(listing);
        }
        
        listing.End();

        inRect.Indent();

        Table.TableOnGUI(inRect);
    }

    protected virtual void DoFilters(Listing_Standard listing)
    {
    }
}