using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class SectionWorker(SectionDef def)
{
    public SectionDef Def = def;
    public TabWorker? Tab;

    protected abstract void DoSectionContents(ref Rect inRect, Pawn pawn);

    private void DoSectionHeader(Rect inRect)
    {
        UIComponents.SectionSeparator(inRect, Def.label);
    }

    public void DoSection(ref Rect inRect, Pawn pawn)
    {
        if (!Def.sectionCategory.HasFlag(PawnUtility.GetPawnCategory(pawn))) return;
        if (!Def.hideHeader) DoSectionHeader(inRect.TakeTopPart(30f));
        DoSectionContents(ref inRect, pawn);
    }

    public virtual void OnPawnChanged(Pawn pawn)
    {
    }
}