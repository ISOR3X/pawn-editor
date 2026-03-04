using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioTraits(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(ref Rect rect, Pawn pawn)
    {
        var traitsHeight = GetTraitsHeight(pawn, (listing.ColumnWidth - Listing.ColumnSpacing) / 2);
        var sectionHeight = traitsHeight + Text.LineHeight + listing.verticalSpacing;
        
        var r = listing.GetRect(sectionHeight);
        var traitsListing = new Listing_Standard {ColumnWidth = r.width / 2 - Listing.ColumnSpacing};
        
        traitsListing.Begin(r);
        
        traitsListing.LabelH2("Traits");
        
        var traitRect = traitsListing.GetRect(traitsHeight);
        DoTraitsRect(traitRect, pawn);
        
        traitsListing.NewColumn(); 
        
        traitsListing.LabelH2("Incapable of");
        var incapableOfRect = traitsListing.GetRect(traitsHeight);
        DoIncapableOfRect(incapableOfRect, pawn);

        traitsListing.End();
        Log.Message($"after EndSection curY={listing.CurHeight} listingRect.height={listing.listingRect.height}");
        listing.Gap(listing.verticalSpacing);
        Log.Message($"after Gap curY={listing.CurHeight}");
        listing.ButtonText_Fit("Add trait");
        Log.Message($"after Button curY={listing.CurHeight}");
        
    }

    private static float GetTraitsHeight(Pawn pawn, float width)
    {
        var traits = pawn.story.traits.TraitsSorted;
        var incapableOf = CharacterCardUtility.WorkTagsFrom(pawn.CombinedDisabledWorkTags).ToList();
        
        // Match the width reductions applied in DrawElementStackSection:
        // ContractedBy(4f) removes 8f total, then DrawElementStack uses width - 5f
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
                    GUI.DrawTexture(r, BaseContent.WhiteTex);
                if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
                if (trait.Suppressed) GUI.color = ColoredText.SubtleGrayColor;
                else if (trait.sourceGene != null) GUI.color = ColoredText.GeneColor;
                Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height), trait.LabelCap);
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
                    GUI.DrawTexture(r, BaseContent.WhiteTex);
                if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
                using (new GUIColor(CharacterCardUtility.GetDisabledWorkTagLabelColor(pawn, workTag)))
                    Widgets.Label(new Rect(r.x + 5f, r.y, r.width - 10f, r.height),
                        workTag.LabelTranslated().CapitalizeFirst());
                GUI.color = Color.white;
                if (Mouse.IsOver(r))
                    TooltipHandler.TipRegion(r, new TipSignal(
                        () => CharacterCardUtility.GetWorkTypeDisabledCausedBy(pawn, workTag) + "\n" +
                              CharacterCardUtility.GetWorkTypesDisabledByWorkTag(workTag),
                        (int)r.y * 32));
            },
            workTag => workTag.LabelTranslated().CapitalizeFirst().GetWidthCached() + 10f);
    }
}