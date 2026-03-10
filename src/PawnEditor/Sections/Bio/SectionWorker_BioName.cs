using HotSwap;
using PawnEditor.Extensions;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioName(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p) => p is { Faction: not null, Name: not null } && base.ShowSection(p);

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var isHuman = PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike;
        var nameRect = listing.RectLabeled("Name");
        BioUtility.DoNameInputRect(nameRect, pawn, isHuman);
    }
}