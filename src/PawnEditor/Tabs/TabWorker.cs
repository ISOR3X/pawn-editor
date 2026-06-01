using UnityEngine;
using Verse;
using Void;

namespace PawnEditor;

public abstract class TabWorker(TabDef def)
{
    private static readonly StyleOverride DefaultRootStyle = new()
    {
        gap = Void.Taffy.Gap(GenUI.GapSmall, GenUI.GapSmall)
    };

    private const float LayoutEpsilon = 0.01f;

    public readonly List<FloatMenuOption> quickActions = [];
    private bool _measuredHeightDirty = true;
    private bool _measuredHeightUsesScrollbar;
    private float _lastMeasuredWidth = -1f;
    private Vector2 _tabScrollPosition = Vector2.zero;
    private float _viewRectHeight = 5000f;
    public TabDef Def = def;

    /// <summary>The context type this worker requires, or null for context-free workers.</summary>
    public virtual Type? RequiredContextType => null;

    public virtual void DoTabContents(ref Rect inRect, IContext? context)
    {
        var r = inRect.ContractedBy(16f);
        Verse.Widgets.BeginGroup(r);

        var contentRect = r.AtZero();
        var rootStyle = Def.layout?.Props.Style.Merge(DefaultRootStyle) ?? DefaultRootStyle;
        EnsureMeasuredHeight(contentRect, context, rootStyle);
        var additionalWidth = _measuredHeightUsesScrollbar ? UIUtility.ScrollBarWidth + GenUI.GapTiny : 0;
        var viewRect = new Rect(0f, 0f, contentRect.width - additionalWidth, _viewRectHeight);

        Verse.Widgets.BeginScrollView(contentRect, ref _tabScrollPosition, viewRect);
        Void.Taffy.Div(Def.defNameHash + (context?.HashCode ?? 0), viewRect, col => DoInnerTabContents(col, context),
            rootStyle);

        Verse.Widgets.EndScrollView();
        Verse.Widgets.EndGroup();
    }

    private void EnsureMeasuredHeight(Rect contentRect, IContext? context, StyleOverride rootStyle)
    {
        var measuredWidth = contentRect.width - (_measuredHeightUsesScrollbar ? UIUtility.ScrollBarWidth + GenUI.GapTiny : 0f);
        if (!_measuredHeightDirty && Mathf.Abs(measuredWidth - _lastMeasuredWidth) < LayoutEpsilon) return;

        var needsScrollbar = _measuredHeightUsesScrollbar;
        var nextMeasuredWidth = measuredWidth;
        float measuredHeight;

        do
        {
            measuredHeight = Void.Taffy.MeasureHeight(
                new Rect(0f, 0f, nextMeasuredWidth, contentRect.height),
                col => DoInnerTabContents(col, context),
                rootStyle);

            var resolvedNeedsScrollbar = contentRect.height < measuredHeight;
            if (resolvedNeedsScrollbar == needsScrollbar)
            {
                _measuredHeightUsesScrollbar = resolvedNeedsScrollbar;
                _viewRectHeight = measuredHeight;
                _lastMeasuredWidth = nextMeasuredWidth;
                _measuredHeightDirty = false;
                return;
            }

            needsScrollbar = resolvedNeedsScrollbar;
            nextMeasuredWidth = contentRect.width - (needsScrollbar ? UIUtility.ScrollBarWidth + GenUI.GapTiny : 0f);
        } while (true);
    }

    protected abstract void DoInnerTabContents(TaffyBuilder col, IContext? context);

    public virtual void Notify_ContentChanged()
    {
        _measuredHeightDirty = true;
        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        quickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            quickActions.Add(attribute.ToFloatMenuOption(method));
    }
}

/// <summary>
///     Typed TabWorker that receives a strongly typed context. Only renders when the context
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