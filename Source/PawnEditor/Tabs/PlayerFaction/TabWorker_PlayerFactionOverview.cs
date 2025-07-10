using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_PlayerFactionOverview : TabWorker_FactionOverview
{
    private static List<SkillDef> skillsForSummary;
    private static int skillsPerColumn = -1;

    public override void Initialize()
    {
        base.Initialize();
        var allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
        for (var i = 0; i < allDefsListForReading.Count; i++)
        {
            var x = Text.CalcSize(allDefsListForReading[i].skillLabel.CapitalizeFirst()).x;
            if (x > SkillUI.levelLabelWidth) SkillUI.levelLabelWidth = x;
        }
    }

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoHeading(inRect.TakeTopPart(35), faction);
        DoTeamSkills(inRect.TakeBottomPart(120), faction);
        inRect.yMax -= 8f;
        DoBottomButtons(inRect.TakeBottomPart(UIUtility.RegularButtonHeight));
        DrawPawnTables(inRect, faction);
    }

    private static void DoBottomButtons(Rect inRect)
    {
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.AddColonist".Translate()))
        {
            PawnEditor.AddPawn(PawnCategory.Humans);
        }

        DoSearch(ref inRect);
    }

    private static void DoTeamSkills(Rect inRect, Faction faction)
    {
        Widgets.DrawLightHighlight(inRect);
        inRect = inRect.ContractedBy(7);
        using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "TeamSkills".Translate());
        skillsForSummary ??= DefDatabase<SkillDef>.AllDefsListForReading.Where(sd => sd.pawnCreatorSummaryVisible).ToList();
        if (skillsPerColumn < 0) skillsPerColumn = Mathf.CeilToInt(skillsForSummary.Count / 4f);
        var columnWidth = inRect.width / Mathf.FloorToInt((float)skillsForSummary.Count / skillsPerColumn);

        for (var i = 0; i < skillsForSummary.Count; i++)
        {
            var skillDef = skillsForSummary[i];
            var r = new Rect(inRect.x + columnWidth * Mathf.FloorToInt((float)i / skillsPerColumn), inRect.y + 24 * (i % skillsPerColumn), columnWidth, 24);

            var skillRecord = new SkillRecord { cachedTotallyDisabled = BoolUnknown.True, def = skillsForSummary[i], cachedPermanentlyDisabled = BoolUnknown.True};
            var tooltipPrefix = "";

            var pawn = FindBestSkillOwner(skillDef, faction);
            if (pawn != null)
            {
                skillRecord = pawn.skills.GetSkill(skillDef);
                tooltipPrefix = pawn.Name.ToString().Colorize(ColoredText.TipSectionTitleColor);
            }

            SkillUI.DrawSkill(skillRecord, r.Rounded(), SkillUI.SkillDrawMode.Menu, tooltipPrefix);
        }
    }

    private static Pawn FindBestSkillOwner(SkillDef skill, Faction faction)
    {
        var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
        var pawns = PawnEditor.Pregame
            ? Find.GameInitData.startingAndOptionalPawns
            : map?.mapPawns?.PawnsInFaction(faction) ?? PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
        pawns.RemoveAll(pawn => pawn.skills == null);
        if (pawns.NullOrEmpty()) return null;
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

    private static void DoHeading(Rect inRect, Faction faction)
    {
        var factionName = "PawnEditor.FactionName".Translate();
        var settlementName = "PawnEditor.SettlementName".Translate();
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var leftLeftWidth = UIUtility.ColumnWidth(6, factionName);
            var rightLeftWidth = UIUtility.ColumnWidth(6, settlementName);
            var factionNameRect = inRect.LeftHalf();
            factionNameRect.width *= 0.75f;
            Widgets.Label(factionNameRect.TakeLeftPart(leftLeftWidth), factionName);
            var tmpName = Widgets.TextField(factionNameRect, faction.Name);
            if (tmpName != faction.def.LabelCap)
            {
                faction.Name = tmpName;
            }

            if (!PawnEditor.Pregame)
            {
                var settlementNameRect = inRect.RightHalf();
                settlementNameRect.width *= 0.75f;
                Widgets.Label(settlementNameRect.TakeLeftPart(rightLeftWidth), settlementName);
                if ((Find.CurrentMap ?? Find.AnyPlayerHomeMap)?.Parent is Settlement settlement)
                    settlement.Name = Widgets.TextField(settlementNameRect, settlement.Name);
            }
        }
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction)
    {
        foreach (var option in base.GetRandomizationOptions(faction)) yield return option;

        if (!PawnEditor.Pregame && (Find.CurrentMap ?? Find.AnyPlayerHomeMap)?.Parent is Settlement settlement)
            yield return new("PawnEditor.SettlementName".Translate(),
                () => { settlement.Name = NameGenerator.GenerateName(faction.def.settlementNameMaker, NamePlayerSettlementDialogUtility.IsValidName); });
    }
}