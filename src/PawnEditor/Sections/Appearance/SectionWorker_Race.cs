using System.Collections.Generic;
using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Race(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return base.ShowSection(p) && GetRacesForPawn(p).Any();
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var r = listing.GetRect(UIUtility.ButtonHeight);

        if (UIUtility.ButtonTextLabeled(r, "Race", pawn.kindDef.race.LabelCap))
        {
        }
    }

    private static List<ThingDef> GetRacesForPawn(Pawn pawn)
    {
        return [];
    }
}