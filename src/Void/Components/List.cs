using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;
using Widgets = Verse.Widgets;

public static partial class VoidComponents
{
    private sealed class ListState
    {
        public Vector2 ScrollPos;
    }

    private static readonly Style EmptyListStyle = new()
    {
        color = ColoredText.SubtleGrayColor,
        textAnchor = TextAnchor.MiddleLeft,
        wordWrap = false
    };

    private static readonly StyleCache<(float itemHeight, float topMargin)> RowStyles = new(k =>
    {
        return new Style
        {
            width = Dimension.Percent(1f),
            height = Dimension.Px(k.itemHeight),
            flexShrink = 0f,
            margin = new TaffyEdges(Dimension.Px(k.topMargin), Dimension.Px(0f),
                    Dimension.Px(0f), Dimension.Px(0f))
        };
    });

    private static readonly StyleCache<(float contentHeight, float gap)> ContainerStyles = new(k =>
    {
        return new Style
        {
            width = Dimension.Percent(1f),
            height = Dimension.Px(k.contentHeight),
            flexShrink = 0f,
            flexDirection = TaffyFlexDirection.Column,
            gap = new TaffyAxes(Dimension.Px(k.gap))
        };
    });

    private static readonly StyleCache<float> RootStyles = new(k =>
    {
        return new Style
        {
            width = Dimension.Percent(1f),
            height = Dimension.Px(k),
            flexShrink = 0f,
            overflowX = TaffyOverflow.Hidden,
            overflowY = TaffyOverflow.Scroll
        };
    });

    extension(UIBranch branch)
    {
        /// <summary>
        ///     Adds a fixed-height scrollable list with virtualized rendering.
        ///     All items must share the same <paramref name="itemHeight" />.
        ///     <code>
        /// col.List(categories, (rect, cat) =>
        /// {
        ///     var selected = _set.Contains(cat);
        ///     Widgets.CheckboxLabeled(rect, cat, ref selected);
        ///     if (selected != _set.Contains(cat)) { ... }
        /// });
        /// </code>
        ///     TODO: Refactor to use components inside of childs as well.
        /// </summary>
        public void List<T>(
            IReadOnlyList<T> items,
            Action<Rect, T> drawItem,
            float itemHeight = UIUtility.ButtonHeight,
            int? maxItemsVisibleAtOnce = null,
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0
        )
        {
            var key = UIBranch.ResolveKey(id, file, line);

            var state = branch.UpsertState(key, () => new ListState { ScrollPos = new Vector2() });

            var gapY = style?.gap?.Height.value ?? 0f;
            var clampedCount = Mathf.Clamp(items.Count, 1, maxItemsVisibleAtOnce ?? items.Count);
            var mergedStyle = (style ?? new Style()).Merge(new Style
            {
                width = Dimension.Percent(1f),
                height = Dimension.Px(clampedCount * itemHeight + Math.Max(0, clampedCount - 1) * gapY)
            });

            branch.Div(draw: r =>
            {
                var step = itemHeight + gapY;
                var itemCount = items.Count;
                var totalHeight = itemCount > 0
                    ? itemCount * itemHeight + (itemCount - 1) * gapY
                    : itemHeight;
                var scrollbarW = totalHeight > r.height ? UIUtility.ScrollBarWidth + 4f : 0f;
                var viewRect = new Rect(0f, 0f, r.width - scrollbarW, totalHeight);

                Widgets.BeginScrollView(r, ref state.ScrollPos, viewRect);

                if (itemCount > 0)
                {
                    var visibleTop = state.ScrollPos.y;
                    var visibleBottom = state.ScrollPos.y + r.height;
                    var first = Math.Max(0, (int)(visibleTop / step));
                    var last = Math.Min(itemCount - 1, (int)(visibleBottom / step));

                    for (var i = first; i <= last; i++)
                        drawItem(new Rect(0f, i * step, viewRect.width, itemHeight), items[i]);
                }
                else
                {
                    using (new GUIColor(ColoredText.SubtleGrayColor))
                    using (new TextBlock(TextAnchor.MiddleLeft))
                    {
                        Widgets.Label(viewRect, "No results available.");
                    }
                }

                Widgets.EndScrollView();
            }, style: mergedStyle);
        }

        /// <summary>
        ///     Virtualized scrollable list. Note that items outside the viewport (excluding overscan items) are destroyed and should therefore not carry state.
        ///     TODO: Use spacers instead of margins? See https://ui.nuxt.com/docs/components/table#with-column-sorting (inspect DOM)
        /// </summary>
        public void List<T>(
            IReadOnlyList<T> items,
            Action<UIBranch, T> drawItem,
            float itemHeight = UIUtility.ButtonHeight,
            int? maxItemsVisibleAtOnce = null,
            Func<T, string>? itemKey = null,
            int overscan = 2,
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0
        )
        {
            var key = UIBranch.ResolveKey(id, file, line);

            var count = items.Count;
            var gapY = style?.gap?.Height.value ?? 0f;
            var step = itemHeight + gapY;

            var contentHeight = count > 0 ? count * itemHeight + (count - 1) * gapY : itemHeight;
            var visibleRows = Mathf.Clamp(count, 1, maxItemsVisibleAtOnce ?? Math.Max(count, 1));
            var viewportHeight = visibleRows * itemHeight + Math.Max(0, visibleRows - 1) * gapY;

            branch.Div(viewport =>
            {
                // The scroll offset and the measured viewport are written by DrawNode during the
                // previous pass. RimWorld runs several event passes per frame and rebuilds on each,
                // so a wheel or drag handled in one pass is already visible to the repaint pass.
                // On the very first build there is no state yet, and the declared height is exact.
                var scroll = viewport.GetState<ScrollState>("scroll");
                var scrollY = scroll?.pos.y ?? 0f;
                var windowHeight = scroll is { VisibleRect.height: > 0f } ? scroll.VisibleRect.height : viewportHeight;

                var first = 0;
                var last = count - 1;
                if (count > 0)
                {
                    first = Mathf.Max(0, Mathf.FloorToInt(scrollY / step) - overscan);
                    last = Mathf.Min(count - 1, Mathf.CeilToInt((scrollY + windowHeight) / step) + overscan);
                }

                // Explicitly set the margin for each row to zero,
                // so when a first row style is demoted to n-th child style,
                // Its margin is properly updated.
                // This is done because Style.Push priorities previous style fields over null.
                var rowStyle = RowStyles.Get((itemHeight, 0f));
                var firstRowStyle = RowStyles.Get((itemHeight, first * step));

                viewport.Div(content =>
                    {
                        if (count == 0)
                        {
                            content.Text("No results available.", EmptyListStyle);
                            return;
                        }

                        for (var i = first; i <= last; i++)
                        {
                            var item = items[i];
                            content.Div(row => drawItem(row, item),
                                style: i == first ? firstRowStyle : rowStyle,
                                id: itemKey?.Invoke(item) ?? $"i{i}");
                        }
                    },
                    // Full content height is used so the scroll view is correctly sized.
                    style: ContainerStyles.Get((contentHeight, gapY)));
            }, style: (style ?? new Style()).Merge(RootStyles.Get(viewportHeight)), id: key);
        }
    }
}
