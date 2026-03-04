using System;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioSkills(SectionDef def) : SectionWorker(def)
{
    public const int SkillColCount = 3;
    public const float SkillHeight = 24f;
    public const float SkillGap = 3f;

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        listing.LabelH2("Skills");
        var skillRect = listing.GetRect(GetSkillsHeight());
        DoSkillsRect(skillRect, pawn);
    }
    
    private static float GetSkillsHeight()
    {
        return Mathf.FloorToInt(DefDatabase<SkillDef>.DefCount / (float)SkillColCount) * (SkillHeight + SkillGap);
    }
    
    private static void DoSkillsRect(Rect inRect, Pawn pawn)
    {
        var skills = SkillUI.skillDefsInListOrderCached;
        var skillsPerColumn = Mathf.CeilToInt(skills.Count / (float)SkillColCount);

        var listing = new Listing_Standard
        {
            ColumnWidth = inRect.width / SkillColCount - Listing.ColumnSpacing,
        };

        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            listing.Begin(inRect);

            for (var i = 0; i < skills.Count; i++)
            {
                if (i > 0 && i % skillsPerColumn == 0)
                    listing.NewColumn();

                var skillDef = skills[i];
                
                var r = listing.GetRect(SkillHeight);
                
                r.xMin -= 6f; // Compensate for the 6f offset from SkillUI.DrawSkill.
                listing.Gap(SkillGap);

                var skillRecord = pawn.skills.GetSkill(skillDef);
                var passionVal = (int)skillRecord.passion;
                var levelVal = skillRecord.GetLevel();
                var newPassionVal = passionVal;
                var newLevelVal = levelVal;

                // Recreate the rects for the skill sections so we can draw in our own widgets but still use SkillUI.DrawSkill
                var r2 = r;
                r2.TakeLeftPart(SkillUI.levelLabelWidth);
                r2.xMin += 12f;
                var passionRect = r2.TakeLeftPart(24f);
                if (skillRecord.passion <= Passion.None || skillRecord.TotallyDisabled)
                {
                    passionRect = passionRect.CenteredVertically(24f);
                    Widgets.DrawTextureFitted(passionRect, Widgets.PlaceholderIconTex, 1f);
                }

                // Increment passion level.
                if (Widgets.ButtonInvisible(passionRect)) newPassionVal++;
                newPassionVal = UIUtility.IncrementWithScroll(passionRect, newPassionVal);

                GUI.DrawTexture(r2, InspectPaneFiller.HealthTex);

                if (Mathf.Approximately(SkillUI.levelLabelWidth, -1))
                    SkillUI.levelLabelWidth =
                        DefDatabase<SkillDef>.AllDefsListForReading.Max(s => s.skillLabel.GetWidthCached());
                SkillUI.DrawSkill(skillRecord, r, SkillUI.SkillDrawMode.Gameplay);

                // Increment skill level
                newLevelVal = UIUtility.IncrementWithScroll(r2, newLevelVal, 5);
                if (Widgets.ButtonImage(r2.TakeRightPart(24f).CenteredVertically(24f).ContractedBy(2f), TexButton.Plus))
                {
                    if (Event.current.shift)
                        newLevelVal += 5;
                    else newLevelVal++;
                }

                r2.xMax -= 4f;
                if (Widgets.ButtonImage(r2.TakeRightPart(24f).CenteredVertically(24f).ContractedBy(2f),
                        TexButton.Minus))
                {
                    if (Event.current.shift)
                        newLevelVal -= 5;
                    else newLevelVal--;
                }

                var (min, max) = GetPassionRange();
                if (newPassionVal > max) newPassionVal = min;
                else if (newPassionVal < min) newPassionVal = max;

                if (skillRecord.TotallyDisabled)
                {
                    if (passionVal != newPassionVal)
                        Messages.Message("Can't change passion level of disabled skill", MessageTypeDefOf.RejectInput);
                    if (levelVal != newLevelVal)
                        Messages.Message("Can't change skill level of disabled skill", MessageTypeDefOf.RejectInput);
                }
                else
                {
                    skillRecord.passion = (Passion)newPassionVal;
                    skillRecord.levelInt = Mathf.Clamp(newLevelVal, 0, 20);
                }
            }

            listing.End();
        }
    }

    private static (int, int) GetPassionRange()
    {
        var min = 0;
        var max = 2;

        // TODO: Convert into a proper check for the mod.
        if (!ModsConfig.IsActive("vanillaexpanded.skills")) return (min, max);
        min = 0;
        max = 5;

        return (min, max);
    }
    
}