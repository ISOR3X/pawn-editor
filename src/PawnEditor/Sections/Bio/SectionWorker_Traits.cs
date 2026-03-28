using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Traits(SectionDef def) : SectionWorker(def)
{
    private float _traitsHeight;

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        var traitsHeight = _traitsHeight > 0f ? _traitsHeight : 50f;

        col.Row(grow: 1f, build: row =>
        {
            row.Column(grow: 1f, build: left =>
            {
                left.Item(height: Text.LineHeight, draw: r => r.LabelH2("Traits"));
                left.Item(height: traitsHeight, draw: r =>
                {
                    DoTraitsRect(r, pawn);
                    _traitsHeight = GetTraitsHeight(pawn, r.width);
                });
            });
            row.Column(grow: 1f, build: right =>
            {
                right.Item(height: Text.LineHeight, draw: r => r.LabelH2("Incapable of"));
                right.Item(height: traitsHeight, draw: r => DoIncapableOfRect(r, pawn));
            });
        });

        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            var w = Text.CalcSize("Add trait").x + 32f;
            Verse.Widgets.ButtonText(r.TakeLeftPart(w), "Add trait");
        });
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
