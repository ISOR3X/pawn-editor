using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioFavColor(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var favColorRect = listing.RectLabeled("Favorite color");
            BioUtility.DoFavColorInputRect(favColorRect, pawn);
        }
    }
}