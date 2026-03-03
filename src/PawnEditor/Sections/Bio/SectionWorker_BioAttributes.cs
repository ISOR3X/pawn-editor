using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioAttributes : SectionWorker
{
    private readonly Listing_Horizontal listing = new Listing_Horizontal();
    private float abilitiesHeight;
    private float traitsHeight;
    private float incapableOfHeight;

    public SectionWorker_BioAttributes(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        listing.Begin(inRect);

        if (PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike)
        {
            Rect skillRect = listing.GetRect(8, BioUtility.skillRowHeight + UIUtility.ButtonHeight);
            BioUtility.DoSkillsRect(skillRect, pawn);

            Rect abilityRect = listing.GetRect(4, abilitiesHeight);
            BioUtility.DoAbilitiesRect(abilityRect, pawn, out abilitiesHeight);
            
            var height = Mathf.Max(traitsHeight, incapableOfHeight);
            height += UIUtility.ButtonHeight * 2 + 8f;
            Rect traitRect = listing.GetRect(4, height);
            BioUtility.DoTraitsRect(traitRect, pawn, out traitsHeight);
            
            height -= UIUtility.ButtonHeight;
            Rect incapableOfRect = listing.GetRect(4, height);
            BioUtility.DoIncapableOfRect(incapableOfRect, pawn, out incapableOfHeight);
        }

        listing.End();
        inRect.TakeTopPart(listing.totalHeight);

        Rect footerRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        WidgetRow row = new WidgetRow(footerRect.x, footerRect.y, UIDirection.RightThenDown);
        if (row.ButtonText("Presets", fixedWidth: "Presets".GetWidthCached() + (UIUtility.ButtonPadding)))
        {
            listing.ClearCache();
        }
    }
}