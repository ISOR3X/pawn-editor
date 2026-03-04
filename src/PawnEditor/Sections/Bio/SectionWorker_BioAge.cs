using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioAge(SectionDef def) : SectionWorker(def)
{
    private static readonly string?[] TextfieldBuffers = new string[2];
    
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        listing.LabelH2("Age");
        DoAgeInputRect(listing, pawn);
    }


    private static void DoAgeInputRect(Listing_Standard listing, Pawn pawn)
    {
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
            var rect1 = listing.RectLabeled(bioLabel, labelWidth: labelWidth);

            var bioAgeMin = 0;
            if (pawn.ageTracker.Adult)
                bioAgeMin = (int)pawn.ageTracker.CurLifeStageRace.minAge;

            bioAge = UIComponents.DelayedTextFieldNumeric(rect1, bioAge, ref TextfieldBuffers[0], bioAgeMin, 9999, null,
                true);
            if (bioAge != pawn.ageTracker.AgeBiologicalYears) pawn.ageTracker.AgeBiologicalTicks = bioAge * GenDate.TicksPerYear;

            listing.Gap(listing.verticalSpacing);
            
            // Chronological
            var rect2 = listing.RectLabeled(chronoLabel, labelWidth: labelWidth);
            chronoAge = UIComponents.DelayedTextFieldNumeric(rect2, chronoAge, ref TextfieldBuffers[1], 0, 9999, null,
                true);
            if (chronoAge != pawn.ageTracker.AgeChronologicalYears)
                pawn.ageTracker.AgeChronologicalTicks = chronoAge * GenDate.TicksPerYear;
        }
    }
}