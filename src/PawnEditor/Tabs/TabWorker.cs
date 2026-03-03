using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class TabWorker
{
    public readonly List<FloatMenuOption> QuickActions = [];
    public TabDef Def;
    protected Vector2 TabScrollPosition = Vector2.zero;

    public TabWorker(TabDef def)
    {
        Def = def;
    }

    public virtual void PreOpen()
    {
    }

    public virtual void PostClose()
    {
    }

    public virtual void DoTabContents(ref Rect inRect)
    {
        inRect = inRect.ContractedBy(16f);
        DoInnerTabContents(ref inRect);
    }

    protected abstract void DoInnerTabContents(ref Rect inRect);

    public virtual void Notify_ContentChanged()
    {
        // Update quick actions
        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        QuickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            QuickActions.Add(attribute.ToFloatMenuOption(method));
    }
}