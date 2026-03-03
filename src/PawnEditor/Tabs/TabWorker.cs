using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class TabWorker
{
    public TabDef def;
    protected Vector2 TabScrollPosition = Vector2.zero;
    public List<FloatMenuOption> quickActions = new List<FloatMenuOption>();
    
    public TabWorker(TabDef def)
    {
        this.def = def;
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
        QuickActionUtility.actions.TryGetValue(def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        quickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
        {
            quickActions.Add(attribute.ToFloatMenuOption(method));
        }
    }
}