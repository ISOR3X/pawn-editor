using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Xenotype(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var r = listing.GetRect(UIUtility.ButtonHeight);

        if (UIUtility.ButtonTextLabeled_WithIcon(r, "Xenotype", pawn.genes.XenotypeLabelCap,
                pawn.genes.XenotypeIcon))
        {
        }
    }
}