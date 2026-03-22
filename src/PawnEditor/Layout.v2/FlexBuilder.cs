using System;
using HotSwap;
using PawnEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Widgets = Verse.Widgets;

namespace FlexLayout
{
    // ──────────────────────────────────────────────────────────────────────────
    // FlexBuilder
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builder object passed to the layout lambda in <see cref="Flex.Row"/> and
    /// <see cref="UnityEngine.UIElements.Column"/>. Call <see cref="Item"/> to add leaf items,
    /// <see cref="Button"/> / <see cref="Label"/> for common widgets, and
    /// <see cref="Row"/> / <see cref="Column"/> for nested containers.
    /// </summary>
    [HotSwappable]
    public sealed class FlexBuilder
    {
        internal readonly LayoutContainer Container;

        internal FlexBuilder(LayoutContainer container)
        {
            Container = container;
        }

        // ── Leaf item ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a leaf item to the container.
        /// Items with no <paramref name="draw"/> callback act as invisible spacers.
        /// </summary>
        public void Item(
            ElementStyle style       = default,
            Action<Rect>? draw    = null)
        {
            Container.Add(MakeItem(style, nested: null, draw: draw));
        }

        // ── Widget helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Adds a <see cref="Verse.Widgets.ButtonText"/> item.
        /// Defaults to <see cref="Text.LineHeight"/> height if no height is set in
        /// <paramref name="style"/>.
        /// </summary>
        public void Button(
            string label,
            Action onClick,
            ElementStyle style       = default)
        {
            if (style.height == StyleLength.Auto()) style.height = UIUtility.ButtonHeight;
            Container.Add(MakeItem(style, nested: null,
                draw: rect => { if (Widgets.ButtonText(rect, label)) onClick(); }));
        }

        /// <summary>
        /// Adds a <see cref="Widgets.Label"/> item.
        /// </summary>
        public void Label(
            string text,
            ElementStyle style       = default,
            TextAnchor anchor     = TextAnchor.MiddleLeft)
        {
            Container.Add(MakeItem(style, nested: null,
                draw: rect =>
                {
                    var prev    = Text.Anchor;
                    Text.Anchor = anchor;
                    Widgets.Label(rect, text);
                    Text.Anchor = prev;
                }));
        }

        // ── Nested row ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a nested row container as an item in this container.
        /// <paramref name="style"/> controls how this row sits inside its parent.
        /// </summary>
        public void Row(
            ElementStyle style             = default,
            float gap                   = 0f,
            float columnGap             = 0f,
            float rowGap                = 0f,
            FlexWrap wrap               = FlexWrap.NoWrap,
            Action<FlexBuilder>? build  = null)
        {
            var nested = MakeContainer(FlexDirection.Row,
                columnGap > 0f ? columnGap : gap, rowGap, wrap);
            Container.Add(MakeItem(style, nested: nested, draw: null));
            build?.Invoke(new FlexBuilder(nested));
        }

        // ── Nested column ──────────────────────────────────────────────────────

        /// <summary>
        /// Adds a nested column container as an item in this container.
        /// </summary>
        public void Column(
            ElementStyle style             = default,
            float gap                   = 0f,
            float columnGap             = 0f,
            float rowGap                = 0f,
            FlexWrap wrap               = FlexWrap.NoWrap,
            Action<FlexBuilder>? build  = null)
        {
            var nested = MakeContainer(FlexDirection.Column,
                columnGap, rowGap > 0f ? rowGap : gap, wrap);
            Container.Add(MakeItem(style, nested: nested, draw: null));
            build?.Invoke(new FlexBuilder(nested));
        }

        // ── Scroll view ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a scrollable container as an item in this container.
        /// Use <paramref name="direction"/> to control whether content scrolls
        /// vertically (Column, default) or horizontally (Row).
        /// Content is measured at unconstrained size along the scroll axis so
        /// items are never asked to shrink.
        /// The scroll key is derived automatically from the call site — each
        /// call location in source gets a unique stable key with no boilerplate.
        /// The only edge case is two ScrollView calls on the same source line,
        /// which would share state.
        /// </summary>
        public void ScrollView(
            ElementStyle style             = default,
            float gap                   = 0f,
            FlexDirection direction     = FlexDirection.Column,
            Action<FlexBuilder>? build  = null,
            [System.Runtime.CompilerServices.CallerFilePath] string callerFile = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int callerLine  = 0)
        {
            string key    = $"{callerFile}:{callerLine}";
            var scrollPos = Flex.GetScrollState(key).ScrollPos;

            bool isVertical = direction == FlexDirection.Column
                           || direction == FlexDirection.ColumnReverse;

            // Default shrink to 0 for scroll views — they should never shrink
            // ScrollView defaults to shrink=0 — it handles overflow via scrolling, not shrinking.
            // Callers can override by passing a style with an explicit shrink value via ElementStyle.WithShrink().
            var resolvedStyle = style;

            Container.Add(MakeItem(resolvedStyle, nested: null,
                draw: outerRect =>
                {
                    float scrollbarWidth = GenUI.ScrollBarWidth;

                    LayoutContainer BuildInner(float innerW, float innerH)
                    {
                        var c = new LayoutContainer
                        {
                            Style = ElementStyle.Column()
                                .Direction(direction)
                                .ColumnGap(isVertical ? 0f : gap)
                                .RowGap(isVertical ? gap : 0f),
                        };
                        build?.Invoke(new FlexBuilder(c));
                        FlexSolver.Compute(c, new Rect(0f, 0f, innerW, innerH));
                        return c;
                    }

                    // Step 1: measure at full outer size on constrained axis.
                    float measureW = isVertical ? outerRect.width  : 100_000f;
                    float measureH = isVertical ? 100_000f         : outerRect.height;
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
                    bool overflows = isVertical
                        ? contentH > outerRect.height
                        : contentW > outerRect.width;

                    if (overflows)
                    {
                        float recomputeW = isVertical ? outerRect.width - scrollbarWidth : 100_000f;
                        float recomputeH = isVertical ? 100_000f : outerRect.height - scrollbarWidth;
                        innerContainer = BuildInner(recomputeW, recomputeH);

                        contentW = 0f; contentH = 0f;
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
                            overflows ? outerRect.width - scrollbarWidth : outerRect.width,
                            Mathf.Max(contentH, outerRect.height))
                        : new Rect(0f, 0f,
                            Mathf.Max(contentW, outerRect.width),
                            overflows ? outerRect.height - scrollbarWidth : outerRect.height);

                    // Step 4: open scroll view, draw, close.
                    Widgets.BeginScrollView(outerRect, ref scrollPos, viewRect);
                    FlexSolver.Draw(innerContainer);
                    Widgets.EndScrollView();

                    Flex.SetScrollState(key, new Flex.ScrollState { ScrollPos = scrollPos });
                }));
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static FlexItem MakeItem(
            ElementStyle style, LayoutContainer? nested, Action<Rect>? draw)
        {
            return new FlexItem
            {
                Style       = style,
                AsContainer = nested,
                OnDraw      = rect =>
                {
                    if (PawnEditorMod.Settings.drawDebug)
                        Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, 0.1f));
                    draw?.Invoke(rect);
                },
            };
        }

        private static LayoutContainer MakeContainer(
            FlexDirection direction, float columnGap, float rowGap, FlexWrap wrap)
        {
            return new LayoutContainer
            {
                Style = ElementStyle.Column()
                    .Direction(direction)
                    .ColumnGap(columnGap)
                    .RowGap(rowGap)
                    .Wrap(wrap),
            };
        }
    }
}