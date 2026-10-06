using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.Taffy;

namespace PawnEditor.v2;

public class SectionWorker_Age : SectionWorker<Pawn>
{
    public override void DoSectionContents(UIBranch b, Pawn pawn)
    {
        b.SectionLabel("Age");
        var tracker = pawn.ageTracker;

        b.Text("Biological");
        b.InputNumber(tracker.AgeBiologicalYears, t => SetAge(bio: t), style: new Style { margin = new TaffyEdges(Dimension.Px(0f), Dimension.Px(GenUI.Gap), Dimension.Px(0f), Dimension.Px(0f)) });

        b.Text("Chronological");
        b.InputNumber(tracker.AgeChronologicalYears, t => SetAge(chrono: t));

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
