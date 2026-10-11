using RimWorld;
using UnityEngine;
using Verse;
using Void.Native;

namespace PawnEditor.v2;

public class SectionWorker_Age : SectionWorker<Pawn>
{
    public override void DoSectionContents(UIBranch b, Pawn pawn)
    {
        b.SectionLabel("Age");
        var tracker = pawn.ageTracker;

        b.Div(b2 =>
        {
            b2.LabeledWidget("Biological", b3 => b3.InputNumber(tracker.AgeBiologicalYears, t => SetAge(bio: t)));
            b2.LabeledWidget("Chronological", b3 => b3.InputNumber(tracker.AgeChronologicalYears, t => SetAge(chrono: t)));
        }, style: new Style { display = TaffyDisplay.Flex, flexWrap = TaffyFlexWrap.Wrap, gap = new TaffyAxes(16f, 8f) });

        void SetAge(int? bio = null, int? chrono = null)
        {
            if (bio is { } b)
            {
                var min = tracker.Adult ? Mathf.FloorToInt(tracker.AdultMinAge) : 0;
                tracker.AgeBiologicalTicks = (long)Mathf.Max(b, min) * GenDate.TicksPerYear;
            }

            if (chrono is { } c)
                tracker.AgeChronologicalTicks = (long)c * GenDate.TicksPerYear;

            // Same as <see cref="PawnGenerator.GenerateRandomAge"/>
            if (tracker.AgeBiologicalTicks > tracker.AgeChronologicalTicks)
                tracker.AgeChronologicalTicks = tracker.AgeBiologicalTicks;
            tracker.ResetAgeReversalDemand(Pawn_AgeTracker.AgeReversalReason.Initial, true);
        }
    }
}
