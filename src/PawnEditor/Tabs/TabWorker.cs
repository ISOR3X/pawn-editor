using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class TabWorker(TabDef def)
{
    public readonly List<FloatMenuOption> quickActions = [];
    public TabDef Def = def;
    protected Vector2 tabScrollPosition = Vector2.zero;
    private float _viewRectHeight = 5000f;

    public virtual void PreOpen()
    {
    }

    public virtual void PostClose()
    {
    }

    public virtual void DoTabContents(ref Rect inRect)
    {
        var r = inRect.ContractedBy(16f);
        Verse.Widgets.BeginGroup(r);
        var contentRect = r.AtZero();
        var additionalWidth = r.height < _viewRectHeight ? UIUtility.ScrollBarWidth + GenUI.GapTiny : 0;
        var viewRect = new Rect(0f, 0f, contentRect.width - additionalWidth, _viewRectHeight);
        Verse.Widgets.BeginScrollView(contentRect, ref tabScrollPosition, viewRect);
        _viewRectHeight = Taffy.MeasuredColumn(viewRect, col => DoInnerTabContents(col));
        Verse.Widgets.EndScrollView();
        Verse.Widgets.EndGroup();
    }

    protected abstract void DoInnerTabContents(TaffyBuilder col);

    public virtual void Notify_ContentChanged()
    {
        // Update quick actions
        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        quickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            quickActions.Add(attribute.ToFloatMenuOption(method));
    }
}
