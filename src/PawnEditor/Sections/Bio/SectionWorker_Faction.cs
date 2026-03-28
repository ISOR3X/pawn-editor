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

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);

        builder.Text("Faction", color: ColoredText.TipSectionTitleColor);
        builder.Button(label, icon, color, onClick: _ =>
            {
                Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsInViewOrder.Select(f =>
                {
                    var (l, i, c) = FactionUtility.GetFactionMeta(f);
                    return new FloatMenuOption(l, () => { FactionUtility.SetFaction(pawn, f); }, i, c);
                }).ToList()));
            }, onHover: r => { TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction)); }
        );
    }
}