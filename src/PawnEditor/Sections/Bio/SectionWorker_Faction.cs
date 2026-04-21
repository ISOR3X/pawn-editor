using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor;

public class SectionWorker_Faction(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return base.ShowSection(p) && p.def.CanHaveFaction;
    }


    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);

        builder.Text("Faction",
            style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
        builder.Button(label, icon, color, _ =>
            {
                Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsInViewOrder.Select(f =>
                {
                    var (l, i, c) = FactionUtility.GetFactionMeta(f);
                    return new FloatMenuOption(l, () => { FactionUtility.SetFaction(pawn, f); }, i, c);
                }).ToList()));
            }, r => { TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction)); }
            , style: new StyleOverride { width = 200f });
    }
}