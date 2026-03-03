using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioAttributes : SectionWorker
{
    private readonly Listing_Horizontal listing = new();
    private float abilitiesHeight;
    private float incapableOfHeight;
    private float traitsHeight;

    public SectionWorker_BioAttributes(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        listing.Begin(inRect);

        if (PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike)
        {
            var skillRect = listing.GetRect(8, BioUtility.SkillRowHeight + UIUtility.ButtonHeight);
            BioUtility.DoSkillsRect(skillRect, pawn);

            var abilityRect = listing.GetRect(4, abilitiesHeight);
            BioUtility.DoAbilitiesRect(abilityRect, pawn, out abilitiesHeight);

            var height = Mathf.Max(traitsHeight, incapableOfHeight);
            height += UIUtility.ButtonHeight * 2 + 8f;
            var traitRect = listing.GetRect(4, height);
            BioUtility.DoTraitsRect(traitRect, pawn, out traitsHeight);

            height -= UIUtility.ButtonHeight;
            var incapableOfRect = listing.GetRect(4, height);
            BioUtility.DoIncapableOfRect(incapableOfRect, pawn, out incapableOfHeight);
        }

        listing.End();
        inRect.TakeTopPart(listing.TotalHeight);

        var footerRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        var row = new WidgetRow(footerRect.x, footerRect.y, UIDirection.RightThenDown);
        if (row.ButtonText("Presets", fixedWidth: "Presets".GetWidthCached() + UIUtility.ButtonPadding))
            listing.ClearCache();
    }
}