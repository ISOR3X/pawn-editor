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
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), add)) { }

        var traitRect = viewRect.TakeTopPart(traitsLastHeight + 14).ContractedBy(7);
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

                    Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), trait.LabelCap);
                    if (Mouse.IsOver(r))
                    {
                        var trLocal = trait;
                        TooltipHandler.TipRegion(r, () => trLocal.TipString(pawn), r.GetHashCode());
                        if (Widgets.ButtonImage(r.RightPartPixels(r.height).ContractedBy(4), TexButton.DeleteX)) pawn.story.traits.RemoveTrait(trait, true);
                    }
                }, trait => Text.CalcSize(trait.LabelCap).x + 10f, 4f, 5f, false)
               .height;

        traitsLastHeight = Mathf.Max(45, traitsLastHeight);

        GUI.color = Color.white;


        headerRect = viewRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "IncapableOf".Translate().Colorize(ColoredText.TipSectionTitleColor));
        var disabledTags = pawn.CombinedDisabledWorkTags;
        var disabledTagsList = CharacterCardUtility.WorkTagsFrom(disabledTags).ToList();
        var disabledRect = viewRect.TakeTopPart(incapableLastHeight + 14).ContractedBy(7);
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

                    Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), tag.LabelTranslated().CapitalizeFirst());
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
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), add)) { }

        var abilities = (from a in pawn.abilities.abilities
            orderby a.def.level, a.def.EntropyGain
            select a).ToList();
        var abilitiesRect = viewRect.TakeTopPart(abilitiesLastHeight + 14).ContractedBy(7);
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
                    GUI.DrawTexture(r, Command.BGTexShrunk);
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    if (Widgets.ButtonImage(r, abil.def.uiIcon, false)) Find.WindowStack.Add(new Dialog_InfoCard(abil.def));

                    if (Mouse.IsOver(r))
                    {
                        var abilCapture = abil;
                        TooltipHandler.TipRegion(r, () => abilCapture.Tooltip + "\n\n" + "ClickToLearnMore".Translate().Colorize(ColoredText.SubtleGrayColor),
                            r.GetHashCode());
                    }
                }, _ => 32f)
               .height;

        abilitiesLastHeight = Mathf.Max(100, abilitiesLastHeight);

        GUI.color = Color.white;

        headerRect = viewRect.TakeTopPart(Text.LineHeight);
        Widgets.Label(headerRect, "Relations".Translate().Colorize(ColoredText.TipSectionTitleColor));
        if (Widgets.ButtonText(headerRect.TakeRightPart(60), add)) { }

        var relations = pawn.relations.DirectRelations.OrderBy(r => r.startTicks).ToList();
        float relationsHeight;
        if (relations.Count == 0)
        {
            relationsHeight = Text.LineHeight;
            var relationsRect = viewRect.TakeTopPart(relationsHeight + 14).ContractedBy(7);
            GUI.color = Color.gray;
            if (Mouse.IsOver(relationsRect)) Widgets.DrawHighlight(relationsRect);

            Widgets.Label(relationsRect, "None".Translate());
            TooltipHandler.TipRegionByKey(relationsRect, "None");
        }
        else
        {
            relationsHeight = relations.Count * (Text.LineHeight + 4);
            var relationsRect = viewRect.TakeTopPart(relationsHeight + 14).ContractedBy(7);
            foreach (var relation in relations)
            {
                var relationRect = relationsRect.TakeTopPart(Text.LineHeight + 4).ContractedBy(2);
                Widgets.DrawHighlightIfMouseover(relationRect);
                using (new TextBlock(TextAnchor.MiddleLeft))
                    Widgets.Label(relationRect,
                        (relation.def.GetGenderSpecificLabelCap(relation.otherPawn) + ": ").Colorize(ColoredText.SubtleGrayColor)
                      + relation.otherPawn.Name.ToStringFull);

                using (new TextBlock(TextAnchor.MiddleRight))
                {
                    var opinionOf = relation.otherPawn.relations.OpinionOf(pawn);
                    var opinionFrom = pawn.relations.OpinionOf(relation.otherPawn);
                    Widgets.Label(relationRect,
                        opinionOf.ToStringWithSign().Colorize(opinionOf < 0 ? ColorLibrary.RedReadable : opinionOf > 0 ? ColorLibrary.Green : Color.white) + "("
                      + opinionFrom.ToStringWithSign().Colorize(opinionFrom < 0 ? ColorLibrary.RedReadable : opinionFrom > 0 ? ColorLibrary.Green : Color.white)
                      + ")");
                }

                if (Mouse.IsOver(relationRect) && Widgets.ButtonImage(relationRect.RightPartPixels(relationRect.height).ContractedBy(4), TexButton.DeleteX))
                    pawn.relations.RemoveDirectRelation(relation);
            }
        }

        leftLastHeight = Text.LineHeight * 4 + traitsLastHeight + incapableLastHeight + abilitiesLastHeight + relationsHeight + 56;

        Widgets.EndScrollView();
    }
}
