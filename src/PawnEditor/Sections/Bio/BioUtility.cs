using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public static class BioUtility
{
    private const int SkillColCount = 2;
    private static readonly string?[] TextfieldBuffers = new string[2];

    // Attributes
    public static readonly float SkillRowHeight;

    static BioUtility()
    {
        SkillRowHeight = Mathf.FloorToInt(DefDatabase<SkillDef>.DefCount / (float)SkillColCount) * (24f + 3f);
    }


    // REF: CharacterCardUtility.DrawCharacterCard
    public static void DoNameInputRect(Rect inRect, Pawn pawn, bool advanced = false)
    {
        // TODO: Add proper icon
        if (advanced)
        {
            var iconRect = inRect.TakeRightPart(WidgetRow.IconSize);
            if (Widgets.ButtonImage(iconRect.CenteredVertically(WidgetRow.IconSize), TexButton.Add))
                FloatWindow.ToggleState<FloatWindow_NamePawn>(iconRect);
        }

        var thirdWidth = inRect.width / 3f;
        var rect1 = inRect.TakeLeftPart(thirdWidth);
        if (pawn.Name is NameTriple tripl)
        {
            var rect2 = inRect.TakeLeftPart(thirdWidth);
            var rect3 = inRect.TakeLeftPart(thirdWidth);
            var first = tripl.First;
            var nick = tripl.Nick;
            var last = tripl.Last;
            CharacterCardUtility.DoNameInputRect(rect1, ref first, 12);
            if (tripl.Nick == tripl.First || tripl.Nick == tripl.Last) GUI.color = new Color(1f, 1f, 1f, 0.5f);
            CharacterCardUtility.DoNameInputRect(rect2, ref nick, 16);
            GUI.color = Color.white;
            CharacterCardUtility.DoNameInputRect(rect3, ref last, 12);
            if (tripl.First != first || tripl.Nick != nick || tripl.Last != last)
                pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);

            TooltipHandler.TipRegionByKey(rect2, "ShortIdentifierDesc");
            TooltipHandler.TipRegionByKey(rect3, "LastNameDesc");
        }
        else if (pawn.Name is NameSingle single)
        {
            var first = single.ToStringFull;
            CharacterCardUtility.DoNameInputRect(rect1, ref first, 16);
            if (pawn.Name.ToStringFull != first) pawn.Name = new NameSingle(first);
        }
        else
        {
            Widgets.Label(rect1, pawn.Name.ToStringFull);
        }

        TooltipHandler.TipRegionByKey(rect1, "FirstNameDesc");
    }

    // REF: CharacterCardUtility.DoLeftSection
    public static void DoBackstoryRect(Rect inRect, Pawn pawn)
    {
        UIComponents.WidgetLabel(inRect.TakeTopPart(UIUtility.ButtonHeight), "Backstory");
        inRect.Indent();
        string childhoodLabel = "Childhood".Translate();
        string adulthoodLabel = "Adulthood".Translate();
        var labelWidth = Mathf.Max(childhoodLabel.GetWidthCached(), adulthoodLabel.GetWidthCached());
        labelWidth += UIUtility.LabelPadding * 3f;
        foreach (BackstorySlot backstorySlot in Enum.GetValues(typeof(BackstorySlot)))
        {
            var backstory = pawn.story.GetBackstory(backstorySlot);
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                if (backstory != null)
                {
                    var rect1 = inRect.TakeTopPart(UIUtility.ButtonHeight);
                    inRect.Gap();
                    Widgets.Label(rect1.TakeLeftPart(labelWidth),
                        backstorySlot == BackstorySlot.Adulthood ? adulthoodLabel : childhoodLabel);
                    var backstoryLabel = backstory.TitleCapFor(pawn.gender);
                    Widgets.ButtonText(rect1, backstoryLabel.Truncate(rect1.width - UIUtility.ButtonPadding));

                    if (Mouse.IsOver(rect1))
                    {
                        var tip = backstoryLabel.Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                        var desc = backstory.FullDescriptionFor(pawn).Resolve();
                        TooltipHandler.TipRegion(rect1, tip + desc);
                    }
                }
            }
        }
    }

    public static void DoAgeInputRect(Rect inRect, Pawn pawn)
    {
        UIComponents.WidgetLabel(inRect.TakeTopPart(UIUtility.ButtonHeight), "Age");
        inRect.Indent();

        var bioAge = pawn.ageTracker.AgeBiologicalYears;
        var chronoAge = pawn.ageTracker.AgeChronologicalYears;

        const string bioLabel = "Biological";
        const string chronoLabel = "Chronological";
        var labelWidth = Mathf.Max(bioLabel.GetWidthCached(), chronoLabel.GetWidthCached());
        labelWidth += UIUtility.LabelPadding * 3f;

        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            // Biological
            // TODO: Add event when age is changed to below adult age.
            var rect1 = inRect.TakeTopPart(UIUtility.ButtonHeight);
            inRect.Gap();
            Widgets.Label(rect1.TakeLeftPart(labelWidth), bioLabel);

            var bioAgeMin = 0;
            if (pawn.ageTracker.Adult)
                bioAgeMin = (int)pawn.ageTracker.CurLifeStageRace.minAge;

            bioAge = UIComponents.DelayedTextFieldNumeric(rect1, bioAge, ref TextfieldBuffers[0], bioAgeMin, 9999, null,
                true);
            if (bioAge != pawn.ageTracker.AgeBiologicalYears) pawn.ageTracker.AgeBiologicalTicks = bioAge * 3600000L;

            // Chronological
            var rect2 = inRect.TakeTopPart(UIUtility.ButtonHeight);
            inRect.Gap();
            Widgets.Label(rect2.TakeLeftPart(labelWidth), chronoLabel);
            chronoAge = UIComponents.DelayedTextFieldNumeric(rect2, chronoAge, ref TextfieldBuffers[1], 0, 9999, null,
                true);
            if (chronoAge != pawn.ageTracker.AgeChronologicalYears)
                pawn.ageTracker.AgeChronologicalTicks = chronoAge * 3600000L;
        }
    }

    public static void DoFavColorInputRect(Rect inRect, Pawn pawn)
    {
        inRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        var oldColor = pawn.story.favoriteColor?.color ?? Color.white;
        var favColorRect = inRect.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);

        Widgets.DrawLightHighlight(favColorRect);
        favColorRect = favColorRect.ContractedBy(2f);
        Widgets.DrawRectFast(favColorRect, pawn.story.favoriteColor?.color ?? Color.white);
        inRect.xMax -= 2f;
        if (Widgets.ButtonText(inRect, "Choose color"))
            Find.WindowStack.Add(new Dialog_ColorPicker(c => pawn.story.favoriteColor?.color = c, oldColor));
    }

    public static void DoSkillsRect(Rect inRect, Pawn pawn)
    {
        UIComponents.WidgetLabel(inRect.TakeTopPart(UIUtility.ButtonHeight), "Skills");
        inRect.Indent();

        var listing = new Listing_Standard
        {
            ColumnWidth = inRect.width / SkillColCount - 17f
        };

        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            listing.Begin(inRect);

            foreach (var skillDef in SkillUI.skillDefsInListOrderCached)
            {
                var r = listing.GetRect(24f);
                r.xMin -= 2f; // Compensate for the 6f offset from SkillUI.DrawSkill.
                listing.Gap(4f);

                var skillRecord = pawn.skills.GetSkill(skillDef);
                var passionVal = (int)skillRecord.passion;
                var newPassionVal = passionVal;
                var levelVal = skillRecord.GetLevel();
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

    // REF: CharacterCardUtility.DoLeftSection
    public static void DoAbilitiesRect(Rect inRect, Pawn pawn, out float totalHeight)
    {
        const float abilityHeight = 36f;

        totalHeight = UIUtility.ButtonHeight * 2 + 8f;

        UIComponents.WidgetLabel(inRect.TakeTopPart(UIUtility.ButtonHeight), "Abilities");
        inRect.Indent();

        var addLabel = "Add ability";
        Widgets.ButtonText(
            inRect.TakeBottomPart(UIUtility.ButtonHeight)
                .LeftPartPixels(addLabel.GetWidthCached() + UIUtility.ButtonPadding), addLabel);

        List<Ability> abilities = pawn.abilities.AllAbilitiesForReading.Where(a => a.def.showOnCharacterCard)
            .OrderBy(a => a.def.level).ThenBy(a => a.def.EntropyGain).ToList();
        inRect = inRect.ContractedBy(0, 4f);
        GUI.DrawTexture(inRect, InspectPaneFiller.HealthTex);

        var innerRect = inRect.ContractedBy(4f);

        if (abilities.NullOrEmpty())
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(innerRect.TakeTopPart(UIUtility.ButtonHeight),
                    "None".Translate().Colorize(ColoredText.SubtleGrayColor));
            }

            totalHeight += 30f;
        }
        else
        {
            var currentY = innerRect.y;
            var stackRect = GenUI.DrawElementStack(new Rect(innerRect.x, currentY, innerRect.width - 5f, abilityHeight),
                abilityHeight, abilities, (r, abil) =>
                {
                    GUI.DrawTexture(r, BaseContent.ClearTex);
                    if (Mouse.IsOver(r))
                        Widgets.DrawHighlight(r);
                    if (Widgets.ButtonImage(r, abil.def.uiIcon, false))
                    {
                        if (Event.current.shift)
                            TryDeleteAbility(abil.def, pawn);
                        else Find.WindowStack.Add(new Dialog_InfoCard(abil.def));
                    }

                    if (!Mouse.IsOver(r))
                        return;
                    var tip = new TipSignal(() =>
                            abil.Tooltip + "\n\n" + "ClickToLearnMore".Translate().Colorize(ColoredText.SubtleGrayColor)
                            + "\n" + "Shift + left click to delete.".Colorize(ColoredText.SubtleGrayColor),
                        (int)currentY * 37);
                    TooltipHandler.TipRegion(r, tip);
                }, _ => abilityHeight);
            totalHeight += stackRect.height + 8f;
        }
    }

    private static void TryDeleteAbility(AbilityDef abilityDef, Pawn pawn)
    {
        var ability = pawn.abilities.abilities.FirstOrDefault(x => x.def == abilityDef);
        if (ability == null)
        {
            Messages.Message($"Failed to delete ability {abilityDef.defName} (it was not related to a def)",
                MessageTypeDefOf.RejectInput);
            return;
        }

        pawn.abilities.RemoveAbility(abilityDef);
    }

    public static void DoTraitsRect(Rect inRect, Pawn pawn, out float totalHeight)
    {
        UIComponents.WidgetLabel(inRect.TakeTopPart(UIUtility.ButtonHeight), "Traits");
        inRect.Indent();

        var addLabel = "Add trait";
        Widgets.ButtonText(
            inRect.TakeBottomPart(UIUtility.ButtonHeight)
                .LeftPartPixels(addLabel.GetWidthCached() + UIUtility.ButtonPadding), addLabel);

        List<Trait> traits = pawn.story.traits.TraitsSorted;

        inRect = inRect.ContractedBy(0, 4f);
        GUI.DrawTexture(inRect, InspectPaneFiller.HealthTex);

        var innerRect = inRect.ContractedBy(4f);

        var currentY = innerRect.y;
        if (traits == null || traits.Count == 0)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(innerRect.TakeTopPart(UIUtility.ButtonHeight),
                    (pawn.DevelopmentalStage.Baby() ? "TraitsDevelopLaterBaby".Translate() : "None".Translate())
                    .Colorize(ColoredText.SubtleGrayColor));
            }

            TooltipHandler.TipRegionByKey(innerRect, "None");
            totalHeight = 22f + 8f;
        }
        else
        {
            var stackRect = GenUI.DrawElementStack(new Rect(innerRect.x, currentY, innerRect.width - 5f, 32f), 22f,
                traits,
                (r, trait) =>
                {
                    using (new GUIColor(CharacterCardUtility.StackElementBackground))
                    {
                        GUI.DrawTexture(r, BaseContent.WhiteTex);
                    }

                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
                    if (trait.Suppressed) GUI.color = ColoredText.SubtleGrayColor;
                    else if (trait.sourceGene != null) GUI.color = ColoredText.GeneColor;

                    Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), trait.LabelCap);

                    GUI.color = Color.white;
                    if (!Mouse.IsOver(r))
                        return;
                    var tip = new TipSignal(() => trait.TipString(pawn), (int)currentY * 37);
                    TooltipHandler.TipRegion(r, tip);
                }, trait => trait.LabelCap.GetWidthCached() + 10f, allowOrderOptimization: false);
            totalHeight = stackRect.height + 8f;
        }
    }

    public static void DoIncapableOfRect(Rect inRect, Pawn pawn, out float totalHeight)
    {
        UIComponents.WidgetLabel(inRect.TakeTopPart(UIUtility.ButtonHeight), "Incapable of");
        inRect.Indent();

        var incapableOf = CharacterCardUtility.WorkTagsFrom(pawn.CombinedDisabledWorkTags).ToList();

        inRect = inRect.ContractedBy(0, 4f);
        GUI.DrawTexture(inRect, InspectPaneFiller.HealthTex);

        var innerRect = inRect.ContractedBy(4f);

        var currentY = innerRect.y;
        if (incapableOf.Count == 0)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(innerRect.TakeTopPart(UIUtility.ButtonHeight),
                    "None".Translate().Colorize(ColoredText.SubtleGrayColor));
            }

            totalHeight = 22f + 8f;
        }
        else
        {
            var stackRect = GenUI.DrawElementStack(new Rect(innerRect.x, currentY, innerRect.width - 5f, 32f), 22f,
                incapableOf,
                (r, workTag) =>
                {
                    using (new GUIColor(CharacterCardUtility.StackElementBackground))
                    {
                        GUI.DrawTexture(r, BaseContent.WhiteTex);
                    }

                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    using (new GUIColor(CharacterCardUtility.GetDisabledWorkTagLabelColor(pawn, workTag)))
                    {
                        Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height),
                            workTag.LabelTranslated().CapitalizeFirst());
                    }

                    GUI.color = Color.white;
                    if (!Mouse.IsOver(r))
                        return;
                    var tip = new TipSignal(
                        () => CharacterCardUtility.GetWorkTypeDisabledCausedBy(pawn, workTag) + "\n" +
                              CharacterCardUtility.GetWorkTypesDisabledByWorkTag(workTag),
                        (int)currentY * 32);
                    TooltipHandler.TipRegion(r, tip);
                }, workTag => workTag.LabelTranslated().CapitalizeFirst().GetWidthCached() + 10f,
                allowOrderOptimization: false);
            totalHeight = stackRect.height + 8f;
        }
    }

    // TODO: Fix skill column count, elementstack drawing (caching of maxHeight for listing).
}