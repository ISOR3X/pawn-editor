using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class SectionWorker_Skills(SectionDef def) : SectionWorker(def)
{
    /// <summary>
    ///     Based on <see cref="GenUI.DrawSkill" />
    /// </summary>
    private static readonly Vector2 SkillRectSize = new(230f, 24f);

    private static readonly float LevelLabelWidth =
        DefDatabase<SkillDef>.AllDefsListForReading.Max(s => s.skillLabel.GetWidthCached()) + GenUI.GapLabel;

    private static readonly IntRange PassionRange;

    /// <summary>
    ///     List of preset options for skills. Made public so any mod can add onto it.
    /// </summary>
    public static Func<Pawn, List<FloatMenuOption>> Presets;

    static SectionWorker_Skills()
    {
        var passions = (Passion[])Enum.GetValues(typeof(Passion));
        PassionRange = new IntRange(passions.Min(p => (int)p), passions.Max(p => (int)p));

        Presets = p =>
        {
            var skills = SkillUI.skillDefsInListOrderCached;
            return
            [
                new FloatMenuOption("Minimize skill levels",
                    () => skills.ForEach(sd => UpdateSkill(sd, p, s => GetMinMaxForSkill(s).min))),
                new FloatMenuOption("Maximize skill levels",
                    () => skills.ForEach(sd => UpdateSkill(sd, p, s => GetMinMaxForSkill(s).max))),
                new FloatMenuOption("Randomize skill levels",
                    () => skills.ForEach(sd => UpdateSkill(sd, p, s => GetMinMaxForSkill(s).RandomInRange)),
                    TexPawnEditor.Randomize, Color.white,
                    MenuOptionPriority.VeryLow),
                new FloatMenuOption("Minimize passion levels",
                    () => skills.ForEach(sd => UpdateSkill(sd, p, passion: PassionRange.min))),
                new FloatMenuOption("Maximize passion levels",
                    () => skills.ForEach(sd => UpdateSkill(sd, p, passion: PassionRange.max))),
                new FloatMenuOption("Randomize passion levels",
                    () => skills.ForEach(sd => UpdateSkill(sd, p, passion: PassionRange.RandomInRange)),
                    TexPawnEditor.Randomize, Color.white,
                    MenuOptionPriority.VeryLow)
            ];
        };
        return;

        static void UpdateSkill(SkillDef skillDef, Pawn pawn, Func<SkillRecord, int>? level = null, int? passion = null)
        {
            var s = pawn.skills.GetSkill(skillDef);
            if (level != null) SetSkill(s, level(s));
            if (passion != null) SetPassion(s, passion.Value);
        }
    }

    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var skills = SkillUI.skillDefsInListOrderCached;

        layout.ComponentById<DivElement>("skills_grid").Children = b => skills.ForEach(sd => DrawSkill(b, pawn, sd));
        layout.ComponentById<ButtonElement>("presets").OnClick =
            _ => Find.WindowStack.Add(new FloatMenu(Presets(pawn)));
    }

    // REF: SkillUI.DrawSkill
    private static void DrawSkill(TaffyBuilder builder, Pawn pawn, SkillDef skillDef)
    {
        var skill = pawn.skills.GetSkill(skillDef);

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
                    style: new StyleOverride { width = Dimension.Px(LevelLabelWidth) });
                row.Button(icon: GetTexForPassion(skill.passion),
                    variant: TaffyExtensions.ButtonVariant.Ghost,
                    style: new StyleOverride { width = Dimension.Px(24f), height = Dimension.Px(24f) },
                    onClick: _ =>
                    {
                        {
                            var passion = (int)skill.passion;
                            passion += Event.current.shift ? -1 : 1;
                            SetPassion(skill, passion);
                        }
                    });
                if (skill.TotallyDisabled)
                {
                    row.Text("-", color: SkillUI.DisabledSkillColor);
                }
                else
                {
                    var level = skill.GetLevel();
                    var minMax = GetMinMaxForSkill(skill);
                    row.InputNumber(ref level, minMax.min, minMax.max,
                        variant: TaffyExtensions.InputVariant.Ghost, id: skill.def.defName, draw:
                        r =>
                        {
                            var skillProgressPct = Mathf.Max(0, skill.GetLevel() / (float)SkillRecord.MaxLevel);

                            Verse.Widgets.FillableBar(r, skillProgressPct, GetTexForSkill(skill),
                                InspectPaneFiller.HealthTex, false);
                        },
                        style: new StyleOverride
                            { height = Dimension.Px(SkillRectSize.y), flexGrow = 1f, color = GetColorTextForSkill(skill) });
                    SetSkill(skill, level);
                }
            },
            new StyleOverride
            {
                display = TaffyDisplay.Flex,
                minWidth = Dimension.Px(SkillRectSize.x),
                height = Dimension.Px(SkillRectSize.y),
                gap = Void.Taffy.Gap(GenUI.GapTiny)
            });
    }

    private static Color GetColorTextForSkill(SkillRecord skill)
    {
        var color = Color.white;

        if (skill.TotallyDisabled)
        {
            color = SkillUI.DisabledSkillColor;
        }
        else
        {
            if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && skill.GetLevel() == 0 && skill.Aptitude != 0)
                color = skill.Aptitude > 0 ? ColorLibrary.BrightGreen : ColorLibrary.RedReadable;
        }

        return color;
    }

    private static IntRange GetMinMaxForSkill(SkillRecord skill)
    {
        var min = Math.Max(SkillRecord.MinLevel, SkillRecord.MinLevel + skill.Aptitude);
        var max = Math.Min(SkillRecord.MaxLevel, SkillRecord.MaxLevel + skill.Aptitude);

        return new IntRange(min, max);
    }

    private static Texture2D GetTexForSkill(SkillRecord skill)
    {
        var tex = SkillUI.SkillBarFillTex;
        if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && skill.Aptitude != 0)
            tex = skill.Aptitude > 0
                ? SkillUI.SkillBarAptitudePositiveTex
                : SkillUI.SkillBarAptitudeNegativeTex;
        return tex;
    }


    private static Texture2D GetTexForPassion(Passion passion)
    {
        return passion switch
        {
            Passion.None => Verse.Widgets.PlaceholderIconTex,
            Passion.Minor => SkillUI.PassionMinorIcon,
            Passion.Major => SkillUI.PassionMajorIcon,
            _ => Verse.Widgets.PlaceholderIconTex
        };
    }

    private static void SetPassion(SkillRecord skill, int passion)
    {
        if (skill.TotallyDisabled)
        {
            Messages.Message("Can't change skill level of disabled skill", MessageTypeDefOf.RejectInput);
            return;
        }

        var range = PassionRange.max - PassionRange.min + 1;
        var wrappedPassion = ((passion - PassionRange.min) % range + range) % range + PassionRange.min;
        skill.passion = (Passion)wrappedPassion;
    }

    private static void SetSkill(SkillRecord skill, int level)
    {
        if (level == skill.GetLevel()) return;

        if (skill.TotallyDisabled)
        {
            Messages.Message("Can't change the passion level of disabled skill", MessageTypeDefOf.RejectInput);
            return;
        }

        skill.Level = Mathf.Clamp(level - skill.Aptitude, SkillRecord.MinLevel, SkillRecord.MaxLevel);
    }
}