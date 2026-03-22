using System;
using System.Runtime.CompilerServices;
using HotSwap;
using PawnEditor;
using UnityEngine;
using Verse;
using Widgets = Verse.Widgets;

// ReSharper disable InvalidXmlDocComment

namespace FlexLayout;
// ──────────────────────────────────────────────────────────────────────────
// FlexBuilder
// ──────────────────────────────────────────────────────────────────────────

/// <summary>
///     Builder object passed to the layout lambda in <see cref="Flex.Row" /> and
///     <see cref="UnityEngine.UIElements.Column" />. Call <see cref="Item" /> to add leaf items,
///     <see cref="Button" /> / <see cref="Label" /> for common widgets, and
///     <see cref="Row" /> / <see cref="Column" /> for nested containers.
/// </summary>
[HotSwappable]
public sealed class FlexBuilder
{
    private readonly LayoutContainer _container;

    internal FlexBuilder(LayoutContainer container)
    {
        _container = container;
    }

    // ── Leaf item ──────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a leaf item to the container.
    ///     Items with no <paramref name="draw" /> callback act as invisible spacers.
    /// </summary>
    public void Item(
        ElementStyle style = default,
        Action<Rect>? draw = null)
    {
        _container.Add(MakeItem(style, null, draw));
    }

    // ── Widget helpers ─────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a <see cref="Verse.Widgets.ButtonText" /> item.
    ///     Defaults to <see cref="Text.LineHeight" /> height if no height is set in
    ///     <paramref name="style" />.
    /// </summary>
    public void Button(
        string label,
        Action onClick,
        ElementStyle? style = null)
    {
        var s = style ?? ElementStyle.Default();
        if (s.height == StyleSize.Auto()) s.height = UIUtility.ButtonHeight;
        if (s.width == StyleSize.Auto()) s.width = Text.CalcSize(label).x + UIUtility.LabelPadding * 2;
        _container.Add(MakeItem(s, null,
            rect =>
            {
                if (Widgets.ButtonText(rect, label)) onClick();
            }));
    }

    /// <summary>
    ///     Adds a <see cref="Widgets.Label" /> item.
    /// </summary>
    public void Label(
        string text,
        ElementStyle style = default,
        TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        if (style.height == StyleSize.Auto()) style.height = Text.LineHeight;
        if (style.width == StyleSize.Auto()) style.width = Text.CalcSize(text).x + UIUtility.LabelPadding * 2;
        _container.Add(MakeItem(style, null,
            rect =>
            {
                using (new TextBlock(anchor))
                {
                    Widgets.Label(rect, text);
                }
            }));
    }

    // ── Nested row ─────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a nested row container as an item in this container.
    ///     <paramref name="style" /> controls how this row sits inside its parent.
    /// </summary>
    public void Row(
        ElementStyle? style = null,
        float gap = 0f,
        float columnGap = 0f,
        float rowGap = 0f,
        FlexWrap wrap = FlexWrap.NoWrap,
        Action<FlexBuilder>? build = null)
    {
        var s = style ?? ElementStyle.Default();
        var cGap = columnGap > 0 ? columnGap : gap;
        var rGap = rowGap > 0 ? rowGap : gap;
        var nested = MakeContainer(FlexDirection.Row, cGap, rGap, wrap);
        _container.Add(MakeItem(s, nested, null));
        build?.Invoke(new FlexBuilder(nested));
    }

    // ── Nested column ──────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a nested column container as an item in this container.
    /// </summary>
    public void Column(
        ElementStyle style = default,
        float gap = 0f,
        float columnGap = 0f,
        float rowGap = 0f,
        FlexWrap wrap = FlexWrap.NoWrap,
        Action<FlexBuilder>? build = null)
    {
        var nested = MakeContainer(FlexDirection.Column,
            columnGap, rowGap > 0f ? rowGap : gap, wrap);
        _container.Add(MakeItem(style, nested, null));
        build?.Invoke(new FlexBuilder(nested));
    }

    // ── Scroll view ────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a scrollable container as an item in this container.
    ///     Use <paramref name="direction" /> to control whether content scrolls
    ///     vertically (Column, default) or horizontally (Row).
    ///     Content is measured at unconstrained size along the scroll axis so
    ///     items are never asked to shrink.
    ///     The scroll key is derived automatically from the call site — each
    ///     call location in source gets a unique stable key with no boilerplate.
    ///     The only edge case is two ScrollView calls on the same source line,
    ///     which would share state.
    /// </summary>
    public void ScrollView(
        ElementStyle? style = null,
        Action<FlexBuilder>? build = null,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        var key = $"{callerFile}:{callerLine}";
        var scrollPos = Flex.GetScrollState(key).scrollPos;
        var s = style ?? new ElementStyle();
        s.display = Display.Flex;

        var isVertical = s.flexDirection is FlexDirection.Column or FlexDirection.ColumnReverse;

        // Default shrink to 0 for scroll views — they should never shrink
        // ScrollView defaults to shrink=0 — it handles overflow via scrolling, not shrinking.
        // Callers can override by passing a style with an explicit shrink value via ElementStyle.WithShrink().

        _container.Add(MakeItem(s, null,
            outerRect =>
            {
                // Step 1: measure at full outer size on constrained axis.
                var measureW = isVertical ? outerRect.width : 100_000f;
                var measureH = isVertical ? 100_000f : outerRect.height;
                var innerContainer = BuildInner(measureW, measureH);

                float contentW = 0f, contentH = 0f;
                foreach (var child in innerContainer.Children)
                {
                    contentW = Mathf.Max(contentW, child.ComputedRect.xMax);
                    contentH = Mathf.Max(contentH, child.ComputedRect.yMax);
                }

                // Step 2: if content overflows the scroll axis, a scrollbar will
                // appear and eat into the constrained axis. Recompute at the
                // reduced size so items don't trigger a cross-axis scrollbar.
                var overflows = isVertical
                    ? contentH > outerRect.height
                    : contentW > outerRect.width;

                if (overflows)
                {
                    var recomputeW = isVertical ? outerRect.width - GenUI.ScrollBarWidth : 100_000f;
                    var recomputeH = isVertical ? 100_000f : outerRect.height - GenUI.ScrollBarWidth;
                    innerContainer = BuildInner(recomputeW, recomputeH);

                    contentW = 0f;
                    contentH = 0f;
                    foreach (var child in innerContainer.Children)
                    {
                        contentW = Mathf.Max(contentW, child.ComputedRect.xMax);
                        contentH = Mathf.Max(contentH, child.ComputedRect.yMax);
                    }
                }

                // Step 3: build viewRect — non-scroll axis is exactly the outer
                // size (scrollbar already accounted for), scroll axis is content
                // size clamped to at least outer size to avoid a spurious scrollbar.
                var viewRect = isVertical
                    ? new Rect(0f, 0f,
                        overflows ? outerRect.width - GenUI.ScrollBarWidth : outerRect.width,
                        Mathf.Max(contentH, outerRect.height))
                    : new Rect(0f, 0f,
                        Mathf.Max(contentW, outerRect.width),
                        overflows ? outerRect.height - GenUI.ScrollBarWidth : outerRect.height);

                // Step 4: open scroll view, draw, close.
                Widgets.BeginScrollView(outerRect, ref scrollPos, viewRect);
                FlexSolver.Draw(innerContainer);
                Widgets.EndScrollView();

                Flex.SetScrollState(key, new Flex.ScrollState { scrollPos = scrollPos });
                return;

                LayoutContainer BuildInner(float innerW, float innerH)
                {
                    var c = new LayoutContainer { Style = s };
                    build?.Invoke(new FlexBuilder(c));
                    FlexSolver.Compute(c, new Rect(0f, 0f, innerW, innerH));
                    return c;
                }
            }));
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static FlexItem MakeItem(
        ElementStyle style, LayoutContainer? nested, Action<Rect>? draw)
    {
        return new FlexItem
        {
            Style = style,
            AsContainer = nested,
            OnDraw = rect =>
            {
                if (PawnEditorMod.Settings.drawDebug)
                    Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, 0.1f));
                draw?.Invoke(rect);
            }
        };
    }

    private static LayoutContainer MakeContainer(
        FlexDirection direction, float columnGap, float rowGap, FlexWrap wrap)
    {
        return new LayoutContainer
        {
            Style =
                new ElementStyle(display: Display.Flex, flexDirection: direction, columnGap: columnGap,
                    rowGap: rowGap, flexWrap: wrap)
        };
    }
}