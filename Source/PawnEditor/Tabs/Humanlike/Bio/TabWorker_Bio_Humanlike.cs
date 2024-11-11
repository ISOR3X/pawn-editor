using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public partial class TabWorker_Bio_Humanlike : TabWorker<Pawn>
{
    private readonly Dictionary<string, string> truncateCache = new();
    private float abilitiesLastHeight = 120;
    private float incapableLastHeight = 60;
    private float leftLastHeight;
    private Vector2 leftScrollPos;
    private float traitsLastHeight = 60;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);
        var buttonRect = headerRect.TakeRightPart(212);
        DoButtons(ref buttonRect, pawn);
        headerRect.xMin += 3;
        DoBasics(headerRect.ContractedBy(5, 0), pawn);
        rect.yMin += 20;
        var (left, skills, groups) = rect.Split1D(3, false, 15);
        DoLeft(left, pawn);
        DoSkills(skills, pawn);
        DoGroups(groups, pawn);
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn)
    {
        yield return new SaveLoadItem<Pawn_AbilityTracker>("Abilities".Translate(), pawn.abilities);
        yield return new SaveLoadItem<AppearanceInfo>("Appearance".Translate(), AppearanceInfo.CreateFrom(pawn), new()
        {
            OnLoad = info => info.CopyTo(pawn)
        });
        yield return new SaveItem("PawnEditor.ExportImage".Translate(), () =>
        {
            var text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (text.NullOrEmpty()) text = GenFilePaths.SaveDataFolderPath;
            text = Path.Combine(text, "PawnEditor", pawn.Name.ToStringShort + "-" + DateTime.Now.ToFileTimeUtc());
            if (!Directory.Exists(text)) Directory.CreateDirectory(text);

            for (var i = 0; i < 4; i++)
            {
                var rot = new Rot4(i);
                PawnEditor.SavePawnTex(pawn, Path.Combine(text, rot.ToStringHuman() + ".png"), rot);
            }

            Messages.Message("PawnEditor.ImageExported".Translate(pawn.Name.ToStringFull, text), MessageTypeDefOf.TaskCompletion, false);
        });
    }

    private void DoSkills(Rect inRect, Pawn pawn)
    {
        var headerRect = inRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "Skills".Translate().Colorize(ColoredText.TipSectionTitleColor));
        GUI.color = Color.white;
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), "PawnEditor.Preset".Translate()))
            if (VSECompat.Active)
            {
                var floatMenuList = new List<FloatMenuOption>
                {
                    new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), 0), GetSetDelegate(pawn, false, 0)),
                    new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), "PawnEditor.Max".Translate()), GetSetDelegate(pawn, false, 20)),
                };
                VSECompat.AddPassionPresets(floatMenuList, pawn);
                Find.WindowStack.Add(new FloatMenu(floatMenuList));
            }
            else
            {
                Find.WindowStack.Add(new FloatMenu(new()
                {
                    new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), 0), GetSetDelegate(pawn, false, 0)),
                    new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), "PawnEditor.Max".Translate()), GetSetDelegate(pawn, false, 20)),
                    new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), Passion.None.GetLabel()), GetSetDelegate(pawn, true, 0)),
                    new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), Passion.Minor.GetLabel()), GetSetDelegate(pawn, true, 1)),
                    new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), Passion.Major.GetLabel()), GetSetDelegate(pawn, true, 2))
                }));
            }

        inRect.xMin += 4;
        inRect.yMin += 4f;
        var leftWidth = SkillUI.skillDefsInListOrderCached.Select(def => Text.CalcSize(def.LabelCap.Resolve()).x).Max() + 16f;
        using (new TextBlock(TextAnchor.MiddleLeft))
            foreach (var def in SkillUI.skillDefsInListOrderCached)
            {
                var rect = inRect.TakeTopPart(26f);
                var skill = pawn.skills.GetSkill(def);
                Widgets.DrawHighlightIfMouseover(rect);
                TooltipHandler.TipRegion(rect, () => SkillUI.GetSkillDescription(skill), def.GetHashCode() * 397945);
                Widgets.Label(rect.TakeLeftPart(leftWidth), def.LabelCap);
                if (VSECompat.Active) {
                    if (Widgets.ButtonImage(rect.TakeLeftPart(30), VSECompat.GetPassionIcon(skill.passion)))
                    {
                        var newPassion = VSECompat.ChangePassion(skill.passion);
                        VSECompat.ClearCacheFor(skill, newPassion);
                        skill.passion = newPassion;
                    }
                } else {
                    if (Widgets.ButtonImage(rect.TakeLeftPart(30), skill.passion switch
                        {
                            Passion.None => TexPawnEditor.PassionEmptyTex,
                            Passion.Minor => SkillUI.PassionMinorIcon,
                            Passion.Major => SkillUI.PassionMajorIcon,
                            _ => SkillUI.PassionMajorIcon
                        }))
                        skill.passion = skill.passion switch
                        {
                            Passion.None => Passion.Minor,
                            Passion.Minor => Passion.Major,
                            Passion.Major => Passion.None,
                            _ => Passion.None
                        };
                }

                var level = skill.GetLevel();
                var disabled = skill.TotallyDisabled;
                var texture2D = SkillUI.SkillBarFillTex;
                if (ModsConfig.BiotechActive)
                {
                    if (skill.Aptitude > 0)
                        texture2D = SkillUI.SkillBarAptitudePositiveTex;
                    else if (skill.Aptitude < 0)
                        texture2D = SkillUI.SkillBarAptitudeNegativeTex;
                }

                if (!disabled) Widgets.FillableBar(rect, Mathf.Max(0.01f, level / 20f), texture2D, TexPawnEditor.SkillBarBGTex, false);
                rect.xMin += 3;
                Widgets.Label(rect, disabled ? "-" : level.ToString());
                if (!disabled && Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(5), TexButton.Plus))
                {
                    skill.Level = skill.GetLevel(false) + 1;
                    PawnEditor.Notify_PointsUsed();
                }

                if (!disabled && Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(5), TexButton.Minus))
                {
                    skill.Level = skill.GetLevel(false) - 1;
                    PawnEditor.Notify_PointsUsed();
                }

                inRect.yMin += 4;
            }
    }

    public static Action GetSetDelegate(Pawn pawn, bool passions, int value)
    {
        return () =>
        {
            foreach (var skillRecord in pawn.skills.skills)
                if (passions)
                    skillRecord.passion = (Passion)value;
                else
                    skillRecord.Level = value;

            PawnEditor.Notify_PointsUsed();
        };
    }

    [HotSwappable]
    internal class AppearanceInfo : IExposable, ISaveable
    {
        public BeardDef beard;
        public TattooDef bodyTattoo;
        public BodyTypeDef bodyType;
        public TattooDef faceTattoo;
        public GeneDef hairColorGene;
        public Color? hairColorOverride;
        public HairDef hairDef;
        public HeadTypeDef headType;
        public GeneDef melaninGene;
        public Color? skinColorOverride;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref beard, nameof(beard));
            Scribe_Defs.Look(ref hairDef, nameof(hairDef));
            Scribe_Defs.Look(ref bodyType, nameof(bodyType));
            Scribe_Defs.Look(ref headType, nameof(headType));
            Scribe_Defs.Look(ref faceTattoo, nameof(faceTattoo));
            Scribe_Defs.Look(ref bodyTattoo, nameof(bodyTattoo));
            Scribe_Defs.Look(ref melaninGene, nameof(melaninGene));
            Scribe_Defs.Look(ref hairColorGene, nameof(hairColorGene));
            Scribe_Values.Look(ref skinColorOverride, nameof(skinColorOverride));
            Scribe_Values.Look(ref hairColorOverride, nameof(hairColorOverride));
        }

        public string DefaultFileName() => "Appearance";

        public static AppearanceInfo CreateFrom(Pawn pawn)
        {
            var result = new AppearanceInfo();
            result.CopyFrom(pawn);
            return result;
        }

        public void CopyFrom(Pawn pawn)
        {
            bodyType = pawn.story.bodyType;
            headType = pawn.story.headType;
            faceTattoo = pawn.style.FaceTattoo;
            bodyTattoo = pawn.style.BodyTattoo;
            hairDef = pawn.story.hairDef;
            beard = pawn.style.beardDef;
            melaninGene = pawn.genes?.GetMelaninGene();
            hairColorGene = pawn.genes?.GetHairColorGene();
            skinColorOverride = pawn.story.skinColorOverride;
            hairColorOverride = pawn.story.HairColor;
            if (hairColorGene?.hairColorOverride == hairColorOverride) hairColorOverride = null;
        }

        public void CopyTo(Pawn pawn)
        {
            pawn.story.bodyType = bodyType;
            pawn.story.headType = headType;
            pawn.story.hairDef = hairDef;
            if (pawn.genes.GetMelaninGene() is { } geneDef1 && pawn.genes.GetGene(geneDef1) is { } gene1) pawn.genes.RemoveGene(gene1);
            if (melaninGene != null) pawn.genes.AddGene(melaninGene, false);
            pawn.story.skinColorOverride = skinColorOverride;
            if (pawn.genes.GetHairColorGene() is { } geneDef2 && pawn.genes.GetGene(geneDef2) is { } gene2) pawn.genes.RemoveGene(gene2);
            if (hairColorGene != null) pawn.genes.AddGene(hairColorGene, false);
            if (hairColorOverride is { } hairColor) pawn.story.HairColor = hairColor;
            if (ModLister.IdeologyInstalled)
            {
                pawn.style.FaceTattoo = faceTattoo;
                pawn.style.BodyTattoo = bodyTattoo;
                pawn.style.beardDef = beard;
            }

            pawn.drawer.renderer.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(pawn);
        }
    }
}
