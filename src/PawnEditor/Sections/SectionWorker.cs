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

    /// <summary>Override to build section contents. Not called for sections that have a <c>&lt;layout&gt;</c> — use <see cref="OnLayout"/> instead.</summary>
    protected virtual void DoSectionContents(TaffyBuilder builder, Pawn pawn) { }

    /// <summary>
    /// Override this to configure named elements in the section's XML layout per-frame.
    /// Called by <see cref="BuildSection"/> when <see cref="SectionDef.layout"/> is set.
    /// </summary>
    public virtual void OnLayout(UILayout layout, Pawn pawn) { }

    /// <summary>
    /// Adds this section's content into <paramref name="builder"/>.
    /// <para><paramref name="tabStyle"/> — when provided, is merged onto the section's single root
    /// node (tab wins on conflict). For legacy sections it is applied as a wrapper div.</para>
    /// </summary>
    public void BuildSection(TaffyBuilder builder, Pawn pawn, StyleOverride? tabStyle = null)
    {
        builder.ContextKey = pawn.thingIDNumber.ToString();
        if (Def.layout != null)
        {
            var layout = new UILayout(Def.layout, pawn);
            OnLayout(layout, pawn);
            layout.Render(builder, tabStyle);
        }
        else if (tabStyle != null)
        {
            builder.Div(inner => DoSectionContents(inner, pawn), tabStyle);
        }
        else
        {
            DoSectionContents(builder, pawn);
        }
    }

    public float DoSection(Pawn pawn, Rect inRect)
    {
        if (!ShowSection(pawn)) return 0f;
        return Taffy.MeasuredColumn(inRect, col => BuildSection(col, pawn));
    }


    // TODO: Call this
    public virtual void OnThingChanged(Pawn pawn)
    {
    }
}