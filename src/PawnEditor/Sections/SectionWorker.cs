using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker(SectionDef def)
{
    public SectionDef Def = def;

    public virtual bool ShowSection(Pawn p)
    {
        return Def.sectionCategory.HasFlag(PawnUtility.GetPawnCategory(p));
    }

    protected abstract void DoSectionContents(TaffyBuilder col, Pawn pawn);

    /// <summary>Adds this section's content items into <paramref name="col"/>. Called by <c>TaffyLayoutNode.BuildInto</c>.</summary>
    public void BuildSection(TaffyBuilder col, Pawn pawn)
    {
        col.ContextKey = pawn.thingIDNumber.ToString();
        DoSectionContents(col, pawn);
    }

    public float DoSection(Pawn pawn, Rect inRect)
    {
        if (!ShowSection(pawn)) return 0f;
        return Taffy.MeasuredColumn(inRect, col =>
        {
            col.ContextKey = pawn.thingIDNumber.ToString();
            DoSectionContents(col, pawn);
        });
    }


    // TODO: Call this
    public virtual void OnThingChanged(Pawn pawn)
    {
    }
}