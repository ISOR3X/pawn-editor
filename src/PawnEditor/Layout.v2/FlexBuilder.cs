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
///     <see cref="Flex.Column" />. Call <see cref="Item" /> to add leaf items,
///     <see cref="Button" /> / <see cref="Label" /> for common widgets, and
///     <see cref="Row" /> / <see cref="Column" /> for nested containers.
/// </summary>
[HotSwappable]
public sealed class FlexBuilder
{
    private readonly LayoutContainer _container;

    /// <summary>
    /// When true, MakeItem sets OnDraw = null — items are registered for sizing
    /// only, no widget code executes. Used for the fitContent measurement pass.
    /// </summary>
    private readonly bool _measureOnly;

    /// <summary>
    /// The rect available to this container, set at construction time.
    /// Used by fitContent to measure children before layout runs.
    /// </summary>
    private readonly Rect _availableRect;

    internal FlexBuilder(LayoutContainer container, bool measureOnly = false, Rect availableRect = default)
    {
        _container = container;
        _measureOnly = measureOnly;
        _availableRect = availableRect;
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
    ///     Defaults to <see cref="UIUtility.ButtonHeight" /> if no height is set.
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
                    Widgets.Label(rect, text);
            }));
    }

    // ── Nested row ─────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a nested row container as an item in this container.
    ///     <paramref name="style" /> controls how this row sits inside its parent.
    ///     When <paramref name="fitContent" /> is true, the row measures its own
    ///     content height via a silent pre-pass and uses it as the item height,
    ///     so the parent column can correctly stack rows without a frame lag.
    /// </summary>
    public void Row(
        ElementStyle? style = null,
        float gap = 0f,
        float columnGap = 0f,
        float rowGap = 0f,
        FlexWrap wrap = FlexWrap.NoWrap,
        bool fitContent = false,
        Action<FlexBuilder>? build = null,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        var s = style ?? ElementStyle.Default();
        var cGap = columnGap > 0 ? columnGap : gap;
        var rGap = rowGap > 0 ? rowGap : gap;
        var nested = MakeContainer(FlexDirection.Row, cGap, rGap, wrap);
        var nestedRect = new Rect(0f, 0f, _availableRect.width, 100_000f);
        build?.Invoke(new FlexBuilder(nested, availableRect: nestedRect));

        if (fitContent)
        {
            var key = $"{callerFile}:{callerLine}";
            var parentWidth = _availableRect.width;

            if (parentWidth > 0f)
            {
                var measure = MakeContainer(FlexDirection.Row, cGap, rGap, wrap);
                build?.Invoke(new FlexBuilder(measure, measureOnly: true));
                FlexSolver.Compute(measure, new Rect(0f, 0f, parentWidth, 100_000f));

                float contentHeight = 0f;
                foreach (var child in measure.Children)
                    contentHeight = Mathf.Max(contentHeight, child.ComputedRect.yMax);

                if (contentHeight > 0f)
                {
                    s = s.With(height: StyleSize.Px(contentHeight));
                    Flex.SetFitContentSize(key, contentHeight);
                }
            }
            else
            {
                var cached = Flex.GetFitContentSize(key);
                if (cached > 0f) s = s.With(height: StyleSize.Px(cached));
            }
        }

        _container.Add(MakeItem(s, nested, null));
    }

    // ── Nested column ──────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a nested column container as an item in this container.
    ///     When <paramref name="fitContent" /> is true, measures content width
    ///     via a silent pre-pass.
    /// </summary>
    public void Column(
        ElementStyle? style = null,
        float gap = 0f,
        float columnGap = 0f,
        float rowGap = 0f,
        FlexWrap wrap = FlexWrap.NoWrap,
        bool fitContent = false,
        Action<FlexBuilder>? build = null,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        var s = style ?? ElementStyle.Default();
        var nested = MakeContainer(FlexDirection.Column,
            columnGap, rowGap > 0f ? rowGap : gap, wrap);
        var nestedRect = new Rect(0f, 0f, 100_000f, _availableRect.height);
        build?.Invoke(new FlexBuilder(nested, availableRect: nestedRect));

        if (fitContent)
        {
            var key = $"{callerFile}:{callerLine}";
            var parentHeight = _availableRect.height;

            if (parentHeight > 0f)
            {
                var measure = MakeContainer(FlexDirection.Column,
                    columnGap, rowGap > 0f ? rowGap : gap, wrap);
                build?.Invoke(new FlexBuilder(measure, measureOnly: true));
                FlexSolver.Compute(measure, new Rect(0f, 0f, 100_000f, parentHeight));

                float contentWidth = 0f;
                foreach (var child in measure.Children)
                    contentWidth = Mathf.Max(contentWidth, child.ComputedRect.xMax);

                if (contentWidth > 0f)
                {
                    s = s.With(width: StyleSize.Px(contentWidth));
                    Flex.SetFitContentSize(key, contentWidth);
                }
            }
            else
            {
                var cached = Flex.GetFitContentSize(key);
                if (cached > 0f) s = s.With(width: StyleSize.Px(cached));
            }
        }

        _container.Add(MakeItem(s, nested, null));
    }

    // ── Scroll view ────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a scrollable container as an item in this container.
    ///     Use <see cref="ElementStyle.flexDirection"/> to control whether content
    ///     scrolls vertically (Column, default) or horizontally (Row).
    /// </summary>
    public void ScrollView(
        ElementStyle? style = null,
        Action<FlexBuilder>? build = null,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        var key = $"{callerFile}:{callerLine}";
        var scrollPos = Flex.GetScrollState(key).scrollPos;
        var s = style ?? ElementStyle.Default();
        s.display = Display.Flex;

        var isVertical = s.flexDirection is FlexDirection.Column or FlexDirection.ColumnReverse;

        _container.Add(MakeItem(s, null,
            outerRect =>
            {
                var measureW = isVertical ? outerRect.width : 100_000f;
                var measureH = isVertical ? 100_000f : outerRect.height;
                var innerContainer = BuildInner(measureW, measureH);

                float contentW = 0f, contentH = 0f;
                foreach (var child in innerContainer.Children)
                {
                    contentW = Mathf.Max(contentW, child.ComputedRect.xMax);
                    contentH = Mathf.Max(contentH, child.ComputedRect.yMax);
                }

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

                var viewRect = isVertical
                    ? new Rect(0f, 0f,
                        overflows ? outerRect.width - GenUI.ScrollBarWidth : outerRect.width,
                        Mathf.Max(contentH, outerRect.height))
                    : new Rect(0f, 0f,
                        Mathf.Max(contentW, outerRect.width),
                        overflows ? outerRect.height - GenUI.ScrollBarWidth : outerRect.height);

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

    private FlexItem MakeItem(
        ElementStyle style, LayoutContainer? nested, Action<Rect>? draw)
    {
        // In measureOnly mode, suppress all draw callbacks so no widget code executes.
        Action<Rect>? onDraw = _measureOnly ? null : rect =>
        {
            if (PawnEditorMod.Settings.drawDebug)
                Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, 0.1f));
            draw?.Invoke(rect);
        };

        return new FlexItem
        {
            Style = style,
            AsContainer = nested,
            OnDraw = onDraw
        };
    }

    private static LayoutContainer MakeContainer(
        FlexDirection direction, float columnGap, float rowGap, FlexWrap wrap)
    {
        return new LayoutContainer
        {
            Style = new ElementStyle(display: Display.Flex, flexDirection: direction,
                columnGap: columnGap, rowGap: rowGap, flexWrap: wrap)
        };
    }
}