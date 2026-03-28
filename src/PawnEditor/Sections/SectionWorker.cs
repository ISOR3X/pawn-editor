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

    protected abstract void DoSectionContents(TaffyBuilder builder, Pawn pawn);

    /// <summary>Adds this section's content items into <paramref name="builder"/>. Called by <c>TaffyLayoutNode.BuildInto</c>.</summary>
    public void BuildSection(TaffyBuilder builder, Pawn pawn)
    {
        builder.ContextKey = pawn.thingIDNumber.ToString();
        DoSectionContents(builder, pawn);
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