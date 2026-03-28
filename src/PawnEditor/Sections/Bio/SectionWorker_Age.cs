using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Age(SectionDef def) : SectionWorker(def)
{
    private static readonly string?[] TextfieldBuffers = new string[2];

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        col.Item(height: Text.LineHeight, draw: r => r.LabelH2("Age"));
        DoAgeInputItems(col, pawn);
    }

    private static void DoAgeInputItems(TaffyBuilder col, Pawn pawn)
    {
        const string bioLabel = "Biological";
        const string chronoLabel = "Chronological";
        var labelWidth = Mathf.Max(bioLabel.GetWidthCached(), chronoLabel.GetWidthCached()) + UIUtility.LabelOffset;

        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                var bioAgeMin = 0;
                if (pawn.ageTracker.Adult)
                    bioAgeMin = (int)pawn.ageTracker.CurLifeStageRace.minAge;
                var right = UIUtility.RectLabeled(r, bioLabel, labelWidth);
                var ba = pawn.ageTracker.AgeBiologicalYears;
                ba = Widgets.DelayedTextFieldNumeric(right, ba, ref TextfieldBuffers[0], bioAgeMin, 9999, null, true);
                if (ba != pawn.ageTracker.AgeBiologicalYears)
                    pawn.ageTracker.AgeBiologicalTicks = ba * GenDate.TicksPerYear;
            }
        });

        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                var right = UIUtility.RectLabeled(r, chronoLabel, labelWidth);
                var ca = pawn.ageTracker.AgeChronologicalYears;
                ca = Widgets.DelayedTextFieldNumeric(right, ca, ref TextfieldBuffers[1], 0, 9999, null, true);
                if (ca != pawn.ageTracker.AgeChronologicalYears)
                    pawn.ageTracker.AgeChronologicalTicks = ca * GenDate.TicksPerYear;
            }
        });
    }
}
