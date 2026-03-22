using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public class DefTableFilter_ContentSource : TableFilter
{
    protected override string Label => "Content source";

    private List<ModContentPack> options =
        LoadedModManager.runningMods.Where(m => m.AllDefs.OfType<ThingDef>().Any()).ToList();

    private ModContentPack? selected = null;


    protected override void DrawFilterWidget(Listing_Standard listing)
    {
        if (listing.ButtonText(selected?.Name ?? "Any"))
        {
            var opts = options.Select(o => new FloatMenuOption(o.Name, () => selected = o)).ToList();
            opts.Add(new FloatMenuOption("None", () => selected = null));
            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }

    public override bool Matches(Def thing)
    {
        if (selected == null) return true;
        return thing.modContentPack == selected;
    }
}