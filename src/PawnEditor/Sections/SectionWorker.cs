using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker(SectionDef def)
{
    public SectionDef Def = def;

    public virtual bool ShowSection(Pawn p) => Def.sectionCategory.HasFlag(PawnUtility.GetPawnCategory(p));

    protected abstract void DoSectionContents(Listing_Standard listing, Pawn pawn);

    public float DoSection(Pawn pawn, Rect inRect)
    {
        if (!ShowSection(pawn)) return 0f;
        var listing = new Listing_Standard { maxOneColumn = true };
        listing.Begin(inRect);
        DoSectionContents(listing, pawn);
        listing.End();
        return listing.CurHeight;
    }


    public virtual void OnThingChanged(Pawn pawn)
    {
    }
}