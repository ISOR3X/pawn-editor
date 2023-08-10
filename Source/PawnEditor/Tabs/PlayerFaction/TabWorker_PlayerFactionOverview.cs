using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class TabWorker_PlayerFactionOverview : TabWorker<Faction>
{
    private readonly QuickSearchWidget searchWidget = new();
    private List<SkillDef> skillsForSummary;
    private int skillsPerColumn = -1;

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoHeading(inRect.TakeTopPart(35), faction);
        DoTeamSkills(inRect.TakeBottomPart(120).ContractedBy(5), faction);
        DoBottomButtons(inRect.TakeBottomPart(40), faction);
    }

    private void DoBottomButtons(Rect inRect, Faction faction)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5), "PawnEditor.AddColonist".Translate())) PawnEditor.AddPawn();
        searchWidget.OnGUI(inRect.TakeRightPart(250).ContractedBy(5));
    }

    private void DoTeamSkills(Rect inRect, Faction faction)
    {
        Widgets.DrawLightHighlight(inRect);
        using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "TeamSkills".Translate());
        skillsForSummary ??= DefDatabase<SkillDef>.AllDefsListForReading.Where(sd => sd.pawnCreatorSummaryVisible).ToList();
        if (skillsPerColumn < 0) skillsPerColumn = Mathf.CeilToInt(skillsForSummary.Count / 4f);

        for (var i = 0; i < skillsForSummary.Count; i++)
        {
            var skillDef = skillsForSummary[i];
            var r = new Rect(inRect.x + inRect.width * (i / skillsPerColumn), inRect.y + inRect.height * (i % skillsPerColumn), inRect.width - 4, 24);
            var pawn = FindBestSkillOwner(skillDef, faction);
            SkillUI.DrawSkill(pawn.skills.GetSkill(skillDef), r.Rounded(), SkillUI.SkillDrawMode.Menu,
                pawn.Name.ToString().Colorize(ColoredText.TipSectionTitleColor));
        }
    }

    private static Pawn FindBestSkillOwner(SkillDef skill, Faction faction)
    {
        var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        var pawns = map?.mapPawns.PawnsInFaction(faction) ?? Find.GameInitData.startingAndOptionalPawns;
        var pawn = pawns[0];
        var skillRecord = pawn.skills.GetSkill(skill);
        for (var i = 1; i < pawns.Count; i++)
        {
            var skill2 = pawns[i].skills.GetSkill(skill);
            if (skillRecord.TotallyDisabled || skill2.Level > skillRecord.Level || (skill2.Level == skillRecord.Level && skill2.passion > skillRecord.passion))
            {
                pawn = pawns[i];
                skillRecord = skill2;
            }
        }

        return pawn;
    }

    private void DoHeading(Rect inRect, Faction faction)
    {
        var factionName = "PawnEditor.FactionName".Translate();
        var settlementName = "PawnEditor.SettlementName".Translate();
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var leftLeftWidth = UIUtility.ColumnWidth(6, factionName);
            var rightLeftWidth = UIUtility.ColumnWidth(6, settlementName);
            var factionNameRect = inRect.LeftHalf();
            Widgets.Label(factionNameRect.TakeLeftPart(leftLeftWidth), factionName);
            faction.Name = Widgets.TextField(factionNameRect, faction.Name);

            var settlementNameRect = inRect.RightHalf();
            Widgets.Label(settlementNameRect.TakeLeftPart(rightLeftWidth), settlementName);
            if ((Find.CurrentMap ?? Find.AnyPlayerHomeMap)?.Parent is Settlement settlement)
                settlement.Name = Widgets.TextField(settlementNameRect, settlement.Name);
        }
    }
}
