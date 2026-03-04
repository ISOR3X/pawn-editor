using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_BioInfo(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var inspectPaneRect = listing.GetRect(120f);
        UIComponents.InspectPane(inspectPaneRect.TakeLeftPart(400f), pawn);
    }
}