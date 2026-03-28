using System.Linq;
using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Faction(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return base.ShowSection(p) && p.def.CanHaveFaction;
    }

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);

            if (UIUtility.ButtonTextLabeled_WithIcon(r, "Faction", label, icon, color))
                Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsInViewOrder.Select(f =>
                {
                    var (l, i, c) = FactionUtility.GetFactionMeta(f);
                    return new FloatMenuOption(l, () => { FactionUtility.SetFaction(pawn, f); }, i, c);
                }).ToList()));

            if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction));
        });
    }
}
