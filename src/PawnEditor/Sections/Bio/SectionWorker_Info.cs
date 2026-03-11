using PawnEditor.Extensions;
using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_Info(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var inspectPaneRect = listing.GetRect(120f);
        // TODO: This shows Allowed area selector even for pawns off the map.
        Widgets.InspectPane(inspectPaneRect.TakeLeftPart(400f), pawn);
    }
}