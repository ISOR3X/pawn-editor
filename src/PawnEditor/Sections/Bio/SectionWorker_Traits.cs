using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Traits(SectionDef def) : SectionWorker(def)
{
    private float? _traitsHeight;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var traitsHeight = _traitsHeight ?? 50f;

        builder.Div(
            new Style
            {
                flexDirection = FlexDirection.Row, gap = Taffy.Gap(GenUI.GapSmall), flexGrow = 1f,
                size = new Size<Dimension>(Dimension.Percent(1f), Dimension.AUTO)
            },
            row =>
            {
                row.Div(
                    new Style
                    {
                        minSize = new Size<Dimension>(200f, Dimension.AUTO), flexDirection = FlexDirection.Column,
                        flexBasis = Dimension.Percent(0.5f)
                    }, build: left =>
                    {
                        left.Text("Traits", color: ColoredText.TipSectionTitleColor);
                        left.Item(
                            new Style
                            {
                                size = new Size<Dimension>(Dimension.Percent(1f), traitsHeight), flexGrow = 1f
                            },
                            draw: r =>
                            {
                                DoTraitsRect(r, pawn);
                                _traitsHeight = GetTraitsHeight(pawn, r.width);
                            });
                    });
                row.Div(
                    new Style
                    {
                        minSize = new Size<Dimension>(200f, Dimension.AUTO), flexDirection = FlexDirection.Column,
                        flexBasis = Dimension.Percent(0.5f)
                    }, build: right =>
                    {
                        right.Text("Incapable of", color: ColoredText.TipSectionTitleColor);
                        right.Item(
                            new Style
                            {
                                size = new Size<Dimension>(Dimension.Percent(1f), traitsHeight), flexGrow = 1f
                            },
                            draw: r => DoIncapableOfRect(r, pawn));
                    });
            });

        builder.Button("Add trait");
    }

    private static float GetTraitsHeight(Pawn pawn, float width)
    {
        var traits = pawn.story.traits.TraitsSorted;
        var incapableOf = CharacterCardUtility.WorkTagsFrom(pawn.CombinedDisabledWorkTags).ToList();

        // Match the width reductions applied in DrawElementStackSection (ContractedBy(4f) removes 8f total)
        var effectiveWidth = width - 8f;

        var traitsHeight = UIUtility.DrawElementStackSectionHeight(
            traits,
            trait => trait.LabelCap.GetWidthCached() + 10f,
            effectiveWidth);

        var incapableHeight = UIUtility.DrawElementStackSectionHeight(
            incapableOf,
            workTag => workTag.LabelTranslated().CapitalizeFirst().GetWidthCached() + 10f,
            effectiveWidth);

        return Mathf.Max(traitsHeight, incapableHeight);
    }

    private static void DoTraitsRect(Rect inRect, Pawn pawn)
    {
        var traits = pawn.story.traits.TraitsSorted;
        var emptyLabel = pawn.DevelopmentalStage.Baby()
            ? "TraitsDevelopLaterBaby".Translate()
            : "None".Translate();

        UIUtility.DrawElementStackSection(inRect, traits,
            (r, trait) =>
            {
                using (new GUIColor(CharacterCardUtility.StackElementBackground))
                {
                    GUI.DrawTexture(r, BaseContent.WhiteTex);
                }

                if (Mouse.IsOver(r)) Verse.Widgets.DrawHighlight(r);
                if (trait.Suppressed) GUI.color = ColoredText.SubtleGrayColor;
                else if (trait.sourceGene != null) GUI.color = ColoredText.GeneColor;
                Verse.Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), trait.LabelCap);
                GUI.color = Color.white;
                if (Mouse.IsOver(r))
                    TooltipHandler.TipRegion(r, new TipSignal(() => trait.TipString(pawn), (int)r.y * 37));
            },
            trait => trait.LabelCap.GetWidthCached() + 10f,
            emptyLabel: emptyLabel);
    }

    private static void DoIncapableOfRect(Rect inRect, Pawn pawn)
    {
        var incapableOf = CharacterCardUtility.WorkTagsFrom(pawn.CombinedDisabledWorkTags).ToList();

        UIUtility.DrawElementStackSection(inRect, incapableOf,
            (r, workTag) =>
            {
                using (new GUIColor(CharacterCardUtility.StackElementBackground))
                {
                    GUI.DrawTexture(r, BaseContent.WhiteTex);
                }

                if (Mouse.IsOver(r)) Verse.Widgets.DrawHighlight(r);
                using (new GUIColor(CharacterCardUtility.GetDisabledWorkTagLabelColor(pawn, workTag)))
                {
                    Verse.Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height),
                        workTag.LabelTranslated().CapitalizeFirst());
                }

                if (Mouse.IsOver(r))
                    TooltipHandler.TipRegion(r, new TipSignal(
                        () => CharacterCardUtility.GetWorkTypeDisabledCausedBy(pawn, workTag) + "\n" +
                              CharacterCardUtility.GetWorkTypesDisabledByWorkTag(workTag),
                        (int)r.y * 32));
            },
            workTag => workTag.LabelTranslated().CapitalizeFirst().GetWidthCached() + 10f);
    }
}