using UnityEngine;
using Verse;
using Void;

namespace PawnEditor;

public abstract class TabWorker(TabDef def)
{
    public readonly List<FloatMenuOption> quickActions = [];
    private Vector2 _tabScrollPosition = Vector2.zero;
    private float _viewRectHeight = 5000f;
    public TabDef Def = def;

    /// <summary>The context type this worker requires, or null for context-free workers.</summary>
    public virtual Type? RequiredContextType => null;

    protected virtual StyleOverride? LayoutStyle => null;

    public virtual void DoTabContents(ref Rect inRect, IContext? context)
    {
        var r = inRect.ContractedBy(16f);
        Verse.Widgets.BeginGroup(r);
        var contentRect = r.AtZero();
        var additionalWidth = r.height < _viewRectHeight ? UIUtility.ScrollBarWidth + GenUI.GapTiny : 0;
        var viewRect = new Rect(0f, 0f, contentRect.width - additionalWidth, _viewRectHeight);
        Verse.Widgets.BeginScrollView(contentRect, ref _tabScrollPosition, viewRect);
        _viewRectHeight = Void.Taffy.DivMeasured(viewRect, col => DoInnerTabContents(col, context), Def.layout.Style);
        Verse.Widgets.EndScrollView();
        Verse.Widgets.EndGroup();
    }

    protected abstract void DoInnerTabContents(TaffyBuilder col, IContext? context);

    public virtual void Notify_ContentChanged()
    {
        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        quickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            quickActions.Add(attribute.ToFloatMenuOption(method));
    }
}

/// <summary>
///     Typed TabWorker that receives a strongly-typed context. Only renders when the context
///     matches <typeparamref name="TContext" />; silently no-ops otherwise.
/// </summary>
public abstract class TabWorker<TContext>(TabDef def) : TabWorker(def)
    where TContext : class, IContext
{
    public override Type RequiredContextType => typeof(TContext);

    protected sealed override void DoInnerTabContents(TaffyBuilder col, IContext? context)
    {
        if (context is TContext typed)
            DoInnerTabContents(col, typed);
    }

    protected abstract void DoInnerTabContents(TaffyBuilder col, TContext context);
}