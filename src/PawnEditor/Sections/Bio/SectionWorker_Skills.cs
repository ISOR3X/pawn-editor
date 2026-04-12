using HotSwap;
using PawnEditor.Extensions;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Skills(SectionDef def) : SectionWorker(def)
{
    private static readonly int PassionMin;
    private static readonly int PassionMax;

    static SectionWorker_Skills()
    {
        var passions = (Passion[])Enum.GetValues(typeof(Passion));
        PassionMin = passions.Min(p => (int)p);
        PassionMax = passions.Max(p => (int)p);
    }

    private static readonly Vector2 SkillRectSize = new(230f, 24f); // REF: GenUI.DrawSkill(... Vector2)

    private static readonly float LevelLabelWidth =
        DefDatabase<SkillDef>.AllDefsListForReading.Max(s => s.skillLabel.GetWidthCached()) + GenUI.GapLabel;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Skills", color: ColoredText.TipSectionTitleColor);
        builder.Div(
            new Style
            {
                flexGrow = 1f, flexDirection = FlexDirection.Row, flexWrap = FlexWrap.Wrap,
                minSize = new Size<Dimension>(Dimension.Percent(1f), Dimension.AUTO),
                gap = Taffy.Gap(GenUI.GapSmall, GenUI.GapTiny)
            },
            col =>
            {
                var skills = SkillUI.skillDefsInListOrderCached;
                foreach (var skillDef in skills)
                {
                    DrawSkill(col, pawn, skillDef);
                }
            });
    }

    // REF: SkillUI.DrawSkill
    private static void DrawSkill(TaffyBuilder builder, Pawn pawn, SkillDef skillDef)
    {
        var skill = pawn.skills.GetSkill(skillDef);
        var newSkillLevel = skill.GetLevel();
        var newPassionLevel = (int)skill.passion;
        builder.Div(
            r =>
            {
                Verse.Widgets.DrawHighlightIfMouseover(r);
                var text = SkillUI.GetSkillDescription(skill);
                TooltipHandler.TipRegion(r, new TipSignal(text));
            },
            row =>
            {
                row.Text(skillDef.LabelCap,
                    style: new StyleOverride { width = LevelLabelWidth });
                row.Button(icon: GetTextureForPassion(pawn.skills.GetSkill(skillDef).passion), drawGraphic: false,
                    style: new StyleOverride { width = 24f, height = 24f }, onClick: (_) => { newPassionLevel++; });
                row.Item(r =>
                {
                    var r2 = r.TakeRightPart(r.height / 2f);
                    r2.SplitHorizontallyEqual(out var upRect, out var downRect);

                    if (Widgets.ButtonImageWithHold(upRect, TexPawnEditor.Up,
                            $"{builder.ContextKey}:{skillDef.defName}:up")) newSkillLevel++;
                    if (Widgets.ButtonImageWithHold(downRect, TexPawnEditor.Down,
                            $"{builder.ContextKey}:{skillDef.defName}:down")) newSkillLevel--;

                    var skillProgressPct = Mathf.Max(0.0f, skill.GetLevel() / (float)SkillRecord.MaxLevel);
                    var texture2D = SkillUI.SkillBarFillTex;
                    if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && skill.Aptitude != 0)
                        texture2D = skill.Aptitude > 0
                            ? SkillUI.SkillBarAptitudePositiveTex
                            : SkillUI.SkillBarAptitudeNegativeTex;
                    var fillTex = texture2D;
                    Verse.Widgets.FillableBar(r, skillProgressPct, fillTex, InspectPaneFiller.HealthTex, false);

                    DrawSkillLevelLabel(r with { xMin = r.xMin + GenUI.GapTiny }, skill);
                    TrySetSkill(skill, newSkillLevel, newPassionLevel);
                }, new StyleOverride { flexGrow = 1f });
            },
            new StyleOverride
            {
                width = SkillRectSize.x,
                height = SkillRectSize.y,
                gap = Taffy.Gap(GenUI.GapTiny)
            });
    }

    private static void DrawSkillLevelLabel(Rect inRect, SkillRecord skill)
    {
        string label;
        var color = Color.white;
        var level = skill.GetLevel();
        if (skill.TotallyDisabled)
        {
            color = SkillUI.DisabledSkillColor;
            label = "-";
        }
        else
        {
            if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && level == 0 && skill.Aptitude != 0)
                color = skill.Aptitude > 0 ? ColorLibrary.BrightGreen : ColorLibrary.RedReadable;
            label = level.ToStringCached();
        }

        using (new TextBlock(TextAnchor.MiddleLeft))
        using (new GUIColor(color))
            Verse.Widgets.Label(inRect, label);
    }

    private static Texture2D GetTextureForPassion(Passion passion)
    {
        return passion switch
        {
            Passion.None => Verse.Widgets.PlaceholderIconTex,
            Passion.Minor => SkillUI.PassionMinorIcon,
            Passion.Major => SkillUI.PassionMajorIcon,
            _ => Verse.Widgets.PlaceholderIconTex
        };
    }


    private static void TrySetSkill(SkillRecord skill, int level, int passion)
    {
        if (skill.TotallyDisabled)
        {
            if (skill.GetLevel() != level)
                Messages.Message("Can't change the passion level of disabled skill", MessageTypeDefOf.RejectInput);
            if ((int)skill.passion != passion)
                Messages.Message("Can't change skill level of disabled skill", MessageTypeDefOf.RejectInput);
        }
        else
        {
            if (passion != (int)skill.passion)
            {
                var range = PassionMax - PassionMin + 1;
                var wrappedPassion = ((passion - PassionMin) % range + range) % range + PassionMin;
                skill.passion = (Passion)wrappedPassion;
            }

            if (level != skill.GetLevel())
                skill.levelInt = Mathf.Clamp(level, SkillRecord.MinLevel, SkillRecord.MaxLevel);
        }
    }
}