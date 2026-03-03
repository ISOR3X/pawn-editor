using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioBasic : SectionWorker
{
    private readonly Listing_Horizontal listing = new();

    public SectionWorker_BioBasic(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        var isHuman = PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike;

        listing.Begin(inRect);

        // Pawns without a faction (mostly wildlife) don't have a name.
        if (pawn.Faction != null)
        {
            var nameRect = listing.RectLabeled("Name", 6);
            var advanced = PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike;
            BioUtility.DoNameInputRect(nameRect, pawn, advanced);
            listing.NewRow();
        }

        if (isHuman)
        {
            var backstoryRect = listing.GetRect(4, 3 * UIUtility.ButtonHeight);
            BioUtility.DoBackstoryRect(backstoryRect, pawn);
        }

        var ageRect = listing.GetRect(4, 3 * 30f);
        BioUtility.DoAgeInputRect(ageRect, pawn);

        if (isHuman && ModsConfig.IdeologyActive)
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                var favColorRect = listing.RectLabeled("Favorite color", 4);
                BioUtility.DoFavColorInputRect(favColorRect, pawn);
            }


        listing.End();
        inRect.TakeTopPart(listing.TotalHeight);
    }
}