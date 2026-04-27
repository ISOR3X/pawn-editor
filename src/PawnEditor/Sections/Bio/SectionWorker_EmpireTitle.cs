using RimWorld;
using Verse;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_EmpireTitle(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p) => base.ShowSection(p) && Faction.OfEmpire != null;

    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var empire = Faction.OfEmpire;
        var curTitle = pawn.royalty.GetCurrentTitle(empire);
        layout.ComponentById<ButtonElement>("role").Label = curTitle?.GetLabelCapFor(pawn) ?? "None".Translate();
        layout.ComponentById<ButtonElement>("role").OnClick = _ =>
        {
            List<FloatMenuOption> opts =
            [
                ..empire.def.RoyalTitlesAllInSeniorityOrderForReading.Select(royalTitle =>
                    new FloatMenuOption(royalTitle.GetLabelCapFor(pawn),
                        () =>
                        {
                            pawn.royalty.SetTitle(empire, royalTitle, true, false, false);
                            pawn.royalty.ResetPermitsAndPoints(empire, royalTitle);
                        })),
                new("None".Colorize(ColoredText.SubtleGrayColor), () => { pawn.royalty.SetTitle(empire, null, false, false, false); })
            ];
            Find.WindowStack.Add(new FloatMenu(opts));
        };
        layout.ComponentById<ButtonElement>("role").OnHover = r =>
        {
            if (curTitle != null)
            {
                var tip = CharacterCardUtility.GetTitleTipString(pawn, empire,
                    pawn.royalty.GetCurrentTitleInFaction(empire), pawn.royalty.GetFavor(empire));
                if (tip != null) TooltipHandler.TipRegion(r, tip);
            }
        };
    }
}