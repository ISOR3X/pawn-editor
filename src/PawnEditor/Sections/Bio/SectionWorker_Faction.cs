using System.Linq;
using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Faction(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return p.def.CanHaveFaction && base.ShowSection(p);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);

        var r = listing.GetRect(UIUtility.ButtonHeight);

        if (UIUtility.ButtonTextLabeled_WithIcon(r, "Faction", label, icon, color))
            Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsInViewOrder.Select(f =>
            {
                var (l, i, c) = FactionUtility.GetFactionMeta(f);
                return new FloatMenuOption(l, () => { FactionUtility.SetFaction(pawn, f); }, i,
                    c);
            }).ToList()));

        if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction));
    }
}