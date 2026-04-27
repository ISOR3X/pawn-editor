using RimWorld;
using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor;

public class SectionWorker_Age(SectionDef def) : SectionWorker(def)
{
    // TODO: Convert to XML Components
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Age", color: ColoredText.TipSectionTitleColor);
        builder.Div(row =>
        {
            DoAgeItem(row, pawn, "Biological", false);
            DoAgeItem(row, pawn, "Chronological", true);
        }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapSmall, GenUI.GapTiny), flexWrap = FlexWrap.Wrap });
    }

    private static void DoAgeItem(TaffyBuilder col, Pawn pawn, string label, bool isChrono)
    {
        var value = isChrono ? pawn.ageTracker.AgeChronologicalYears : pawn.ageTracker.AgeBiologicalYears;
        var min = !isChrono && pawn.ageTracker.Adult ? (int)pawn.ageTracker.CurLifeStageRace.minAge : 0;

        col.Div(row =>
        {
            row.Text(label);
            row.InputNumber(ref value, min, 9999, id: label);
        }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapLabel) });

        if (isChrono)
        {
            if (value != pawn.ageTracker.AgeChronologicalYears)
                pawn.ageTracker.AgeChronologicalTicks = (long)value * GenDate.TicksPerYear;
        }
        else
        {
            if (value != pawn.ageTracker.AgeBiologicalYears)
                pawn.ageTracker.AgeBiologicalTicks = (long)value * GenDate.TicksPerYear;
        }
    }
}