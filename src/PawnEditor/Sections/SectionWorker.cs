using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class SectionWorker
{
    public SectionDef def;
    public TabWorker tab;

    protected SectionWorker(SectionDef def)
    {
        this.def = def;
    }

    protected abstract void DoSectionContents(ref Rect inRect, Pawn pawn);



    private void DoSectionHeader(Rect inRect)
    {
        UIComponents.SectionSeperator(inRect, def.label);
    }

    public void DoSection(ref Rect inRect, Pawn pawn)
    {
        if (!def.sectionCategory.HasFlag(PawnUtility.GetPawnCategory(pawn))) return;
        if (!def.hideHeader) this.DoSectionHeader(inRect.TakeTopPart(30f));
        this.DoSectionContents(ref inRect, pawn);
    }

    public virtual void OnPawnChanged(Pawn pawn)
    {
    }
}