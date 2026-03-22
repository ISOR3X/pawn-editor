using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;
using L = PawnEditor.Layout.LayoutHelper;

namespace PawnEditor;

[HotSwappable]
public class Window_AddItem(
    DefTableDef tableDef,
    Func<IEnumerable<Def>> itemsGetter,
    IEnumerable<(string Label, Func<TableFilter> Factory)>? availableFilters = null)
    : Window
{
    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    private readonly IReadOnlyList<(string Label, Func<TableFilter> Factory)> _availableFilters =
        availableFilters?.ToList() ?? [];

    private FilteredDefTableWorker Table => field ??= new FilteredDefTableWorker(tableDef, itemsGetter);

    public override void DoWindowContents(Rect inRect)
    {
        var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
        inRect.Gap();
        var leftRect = inRect.TakeLeftPart(200f);
        var listing = new Listing_Standard();

        listing.Begin(leftRect);
        if (listing.ButtonText("Add filter"))
        {
            var opts = _availableFilters
                .Select(f => new FloatMenuOption(f.Label, () => Table.AddFilter(f.Factory())))
                .ToList();
            if (opts.Count > 0) Find.WindowStack.Add(new FloatMenu(opts));
            else Messages.Message("No filters available", MessageTypeDefOf.RejectInput);
        }

        foreach (var filter in Table.Filters.ToList())
            filter.DrawFilter(listing, Table.SetDirty, () => Table.RemoveFilter(filter));
        listing.End();

        inRect.Indent();

        Table.TableOnGUI(inRect);

        Verse.Widgets.Label(footerRect, Table.Selected?.LabelCap ?? "No item selected");
    }
}