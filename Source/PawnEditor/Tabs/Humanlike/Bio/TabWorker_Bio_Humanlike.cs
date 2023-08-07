﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public partial class TabWorker_Bio_Humanlike : TabWorker<Pawn>
{
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
        DoButtons(headerRect.TakeRightPart(212).TopPartPixels(150), pawn);
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
        yield return new SaveLoadItem<AppearanceInfo>("Appearance".Translate(), AppearanceInfo.CreateFrom(pawn), new SaveLoadParms<AppearanceInfo>
        {
            OnLoad = info => info.CopyTo(pawn)
        });
    }

    private void DoSkills(Rect inRect, Pawn pawn)
    {
        var headerRect = inRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "Skills".Translate().Colorize(ColoredText.TipSectionTitleColor));
        GUI.color = Color.white;
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), "PawnEditor.Preset".Translate()))
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), 0), GetSetDelegate(pawn, false, 0)),
                new("PawnEditor.SetAllTo".Translate("Skills".Translate().ToLower(), "PawnEditor.Max".Translate()), GetSetDelegate(pawn, false, 20)),
                new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), Passion.None.GetLabel()), GetSetDelegate(pawn, false, 0)),
                new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), Passion.Minor.GetLabel()), GetSetDelegate(pawn, false, 1)),
                new("PawnEditor.SetAllTo".Translate("PawnEditor.Passions".Translate(), Passion.Major.GetLabel()), GetSetDelegate(pawn, false, 2))
            }));

        inRect.xMin += 4;
        var leftWidth = SkillUI.skillDefsInListOrderCached.Select(def => Text.CalcSize(def.LabelCap.Resolve()).x).Max() + 4;
        using (new TextBlock(TextAnchor.MiddleLeft))
            foreach (var def in SkillUI.skillDefsInListOrderCached)
            {
                var rect = inRect.TakeTopPart(30);
                var skill = pawn.skills.GetSkill(def);
                Widgets.DrawHighlightIfMouseover(rect);
                TooltipHandler.TipRegion(rect, () => SkillUI.GetSkillDescription(skill), def.GetHashCode() * 397945);
                Widgets.Label(rect.TakeLeftPart(leftWidth), def.LabelCap);
                if (Widgets.ButtonImage(rect.TakeLeftPart(30), skill.passion switch
                    {
                        Passion.None => TexPawnEditor.PassionEmptyTex,
                        Passion.Minor => SkillUI.PassionMinorIcon,
                        Passion.Major => SkillUI.PassionMajorIcon,
                        _ => throw new ArgumentOutOfRangeException()
                    }))
                    skill.passion = skill.passion switch
                    {
                        Passion.None => Passion.Minor,
                        Passion.Minor => Passion.Major,
                        Passion.Major => Passion.None,
                        _ => Passion.None
                    };
                var level = skill.GetLevel();
                var disabled = skill.TotallyDisabled;
                if (!disabled) Widgets.FillableBar(rect, Mathf.Max(0.01f, level / 20f), SkillUI.SkillBarFillTex, TexPawnEditor.SkillBarBGTex, false);
                rect.xMin += 3;
                Widgets.Label(rect, disabled ? "-" : level.ToString());
                if (!disabled && Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(5), TexButton.Plus)) skill.Level++;
                if (!disabled && Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(5), TexButton.Minus)) skill.Level--;
            }
    }

    private static Action GetSetDelegate(Pawn pawn, bool passions, int value)
    {
        return () =>
        {
            foreach (var skillRecord in pawn.skills.skills)
                if (passions)
                    skillRecord.passion = (Passion)value;
                else
                    skillRecord.Level = value;
        };
    }

    [HotSwappable]
    private class AppearanceInfo : IExposable
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

            pawn.drawer.renderer.graphics.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(pawn);
        }
    }
}