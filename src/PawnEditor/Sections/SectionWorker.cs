using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;
[HotSwappable]
public abstract class SectionWorker(SectionDef def)
{
    public SectionDef Def = def;
    public TabWorker? Tab;
    private float _cachedHeight = -1f; // -1 means not yet measured

    protected abstract void DoSectionContents(ref Rect rect, Pawn pawn);

    public float MeasureHeight(Pawn pawn, float width)
    {
        if (!Def.sectionCategory.HasFlag(PawnUtility.GetPawnCategory(pawn))) return 0f;
        if (_cachedHeight >= 0f) return _cachedHeight;

        const float maxHeight = 99999f;

        var rect = new Rect(0, 0, width, maxHeight);
        DoSectionContents(ref rect, pawn);
        _cachedHeight = maxHeight - rect.height;
        return _cachedHeight;
    }

    public void DoSection(ref Rect inRect, Pawn pawn)
    {
        if (!Def.sectionCategory.HasFlag(PawnUtility.GetPawnCategory(pawn))) return;
        DoSectionContents(ref inRect, pawn);
    }

    public void InvalidateHeight()
    {
        _cachedHeight = -1f;
    }

    public virtual void OnThingChanged(Pawn pawn) { }
}