using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    private void DoLeft(Rect inRect, Pawn pawn)
    {
        if (leftLastHeight == 0) leftLastHeight = inRect.height;
        var viewRect = new Rect(0, 0, inRect.width - 20, leftLastHeight);
        Widgets.BeginScrollView(inRect, ref leftScrollPos, viewRect);
        var add = "Add".Translate().CapitalizeFirst();
        var headerRect = viewRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "Traits".Translate().Colorize(ColoredText.TipSectionTitleColor));
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), add)) Find.WindowStack.Add(new ListingMenu_Trait(pawn));

        var traitRect = viewRect.TakeTopPart(traitsLastHeight + 14).ContractedBy(6);
        var traits = pawn.story.traits.allTraits;
        if (traits == null || traits.Count == 0)
        {
            GUI.color = Color.gray;
            Widgets.Label(traitRect, pawn.DevelopmentalStage.Baby() ? "TraitsDevelopLaterBaby".Translate() : "None".Translate());
            TooltipHandler.TipRegionByKey(traitRect, "None");
            traitsLastHeight = Text.LineHeight;
        }
        else
            traitsLastHeight = GenUI.DrawElementStack(traitRect, 24, pawn.story.traits.TraitsSorted, delegate(Rect r, Trait trait)
                {
                    GUI.color = CharacterCardUtility.StackElementBackground;
                    GUI.DrawTexture(r, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    if (trait.Suppressed) GUI.color = ColoredText.SubtleGrayColor;
                    else if (trait.sourceGene != null) GUI.color = ColoredText.GeneColor;

                    Widgets.Label(new(r.x + 5f, r.y, r.width - 10f, r.height), trait.LabelCap);
                    if (Mouse.IsOver(r))
                    {
                        var trLocal = trait;
                        TooltipHandler.TipRegion(r, () => trLocal.TipString(pawn), r.GetHashCode());
                        if (Widgets.ButtonImage(r.RightPartPixels(r.height).ContractedBy(4), TexButton.Delete))
                        {
                            pawn.story.traits.RemoveTrait(trait, true);
                            PawnEditor.Notify_PointsUsed();
                        }
                    }
                }, WidthGetter, 4f, 5f, false)
               .height;

        traitsLastHeight = Mathf.Max(45, traitsLastHeight);

        GUI.color = Color.white;


        headerRect = viewRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "IncapableOf".Translate().Colorize(ColoredText.TipSectionTitleColor));
        var disabledTags = pawn.CombinedDisabledWorkTags;
        var disabledTagsList = CharacterCardUtility.WorkTagsFrom(disabledTags).ToList();
        var disabledRect = viewRect.TakeTopPart(incapableLastHeight + 14).ContractedBy(6);
        if (disabledTags == WorkTags.None)
        {
            GUI.color = Color.gray;
            if (Mouse.IsOver(disabledRect)) Widgets.DrawHighlight(disabledRect);

            Widgets.Label(disabledRect, "None".Translate());
            TooltipHandler.TipRegionByKey(disabledRect, "None");
            incapableLastHeight = Text.LineHeight;
        }
        else
            incapableLastHeight = GenUI.DrawElementStack(disabledRect, 22f, disabledTagsList, delegate(Rect r, WorkTags tag)
                {
                    GUI.color = CharacterCardUtility.StackElementBackground;
                    GUI.DrawTexture(r, BaseContent.WhiteTex);
                    GUI.color = CharacterCardUtility.GetDisabledWorkTagLabelColor(pawn, tag);
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    Widgets.Label(new(r.x + 5f, r.y, r.width - 10f, r.height), tag.LabelTranslated().CapitalizeFirst());
                    if (Mouse.IsOver(r))
                    {
                        var tagLocal = tag;
                        TooltipHandler.TipRegion(r, () => CharacterCardUtility.GetWorkTypeDisabledCausedBy(pawn, tagLocal) + "\n"
                          + CharacterCardUtility.GetWorkTypesDisabledByWorkTag(
                                tagLocal), r.GetHashCode());
                    }
                }, tag => Text.CalcSize(tag.LabelTranslated().CapitalizeFirst()).x + 10f, 5f)
               .height;

        incapableLastHeight = Mathf.Max(45, incapableLastHeight);

        GUI.color = Color.white;

        headerRect = viewRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "Abilities".Translate().Colorize(ColoredText.TipSectionTitleColor));
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), add)) Find.WindowStack.Add(new ListingMenu_Abilities(pawn));

        var abilities = (from a in pawn.abilities.abilities
            orderby a.def.level, a.def.EntropyGain
            select a).ToList();
        var abilitiesRect = viewRect.TakeTopPart(abilitiesLastHeight + 14).ContractedBy(6);
        if (abilities.Count == 0)
        {
            GUI.color = Color.gray;
            if (Mouse.IsOver(abilitiesRect)) Widgets.DrawHighlight(abilitiesRect);

            Widgets.Label(abilitiesRect, "None".Translate());
            TooltipHandler.TipRegionByKey(abilitiesRect, "None");
            abilitiesLastHeight = Text.LineHeight;
        }
        else
            abilitiesLastHeight = GenUI.DrawElementStack(abilitiesRect, 32f, abilities, delegate(Rect r, Ability abil)
                {
                    // GUI.DrawTexture(r, Command.BGTexShrunk);
                    if (Mouse.IsOver(r))
                    {
                        Widgets.DrawHighlight(r);
                        if (Widgets.ButtonImage(r.TopPart(0.3f).RightPart(0.3f), TexButton.Delete)) pawn.abilities.RemoveAbility(abil.def);
                    }


                    if (Widgets.ButtonImage(r, abil.def.uiIcon, false)) Find.WindowStack.Add(new Dialog_InfoCard(abil.def));

                    if (Mouse.IsOver(r))
                    {
                        var abilCapture = abil;
                        TooltipHandler.TipRegion(r, () => abilCapture.Tooltip + "\n\n" + "ClickToLearnMore".Translate().Colorize(ColoredText.SubtleGrayColor),
                            r.GetHashCode());
                    }
                }, _ => 32f)
               .height;

        abilitiesLastHeight = Mathf.Max(45, abilitiesLastHeight);

        GUI.color = Color.white;

        // Extras group
        headerRect = viewRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "PawnEditor.Extras".Translate().Colorize(ColoredText.TipSectionTitleColor));

        // Favourite color
        var colorRect = viewRect.TakeTopPart(30);
        colorRect.xMin += 6f;
        string label = "PawnEditor.FavColor".Translate();
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(colorRect.TakeLeftPart(Text.CalcSize(label).x + 4), label);
        Widgets.DrawBoxSolid(colorRect.TakeRightPart(30).ContractedBy(2.5f), pawn.story.favoriteColor?.color ?? Color.white);
        if (Widgets.ButtonText(colorRect, "PawnEditor.PickColor".Translate()))
        {
            var currentColor = pawn.story.favoriteColor?.color ?? Color.white;
            Find.WindowStack.Add(new Dialog_ColorPicker(color => pawn.story.favoriteColor.color = color, DefDatabase<ColorDef>.AllDefs
                   .Select(cd => cd.color)
                   .ToList(),
                currentColor));
        }

        var extrasHeight = 30f;

        leftLastHeight = Text.LineHeight * 4 + traitsLastHeight + incapableLastHeight + abilitiesLastHeight + extrasHeight + 56;

        Widgets.EndScrollView();
    }

    private static float WidthGetter(Trait trait) => Text.CalcSize(trait.LabelCap).x + 10f;
}
