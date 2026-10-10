using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace Void.Native;

public static partial class Components
{
    private static readonly Style EmptyListStyle = new()
    {
        color = ColoredText.SubtleGrayColor,
        textAnchor = TextAnchor.MiddleLeft,
        wordWrap = false
    };

    private static readonly StyleCache<(float itemHeight, float topMargin)> ListRowStyles = new(k => new Style
    {
        width = Dimension.Percent(1f),
        height = Dimension.Px(k.itemHeight),
        flexShrink = 0f,
        margin = new TaffyEdges(Dimension.Px(k.topMargin), Dimension.Px(0f), Dimension.Px(0f), Dimension.Px(0f))
    });

    private static readonly StyleCache<(float contentHeight, float gap)> ListContentStyles = new(k => new Style
    {
        width = Dimension.Percent(1f),
        height = Dimension.Px(k.contentHeight),
        flexShrink = 0f,
        flexDirection = TaffyFlexDirection.Column,
        gap = new TaffyAxes(Dimension.Px(k.gap))
    });

    private static readonly StyleCache<float> ListViewportStyles = new(height => new Style
    {
        width = Dimension.Percent(1f),
        height = Dimension.Px(height),
        flexShrink = 0f,
        overflowX = TaffyOverflow.Hidden,
        overflowY = TaffyOverflow.Scroll
    });

    extension(UIBranch branch)
    {
        /// <summary>
        ///     A scrollable list that only builds the rows in view (plus <paramref name="overscan" /> on either side).
        ///     All rows share <paramref name="itemHeight" />. Rows outside the view are removed, so they should not keep
        ///     state of their own.
        ///     <para>
        ///         <paramref name="rowDraw" /> gives a row something to paint behind its content, for rows that want a
        ///         background of their own (zebra, hover, selection). It gets the item and its index, and the scroll state of
        ///         the list as of the last draw, which is <c>null</c> until the list has been drawn once or when it does not
        ///         scroll.
        ///     </para>
        /// </summary>
        public UINode List<T>(
            IReadOnlyList<T> items,
            Action<UIBranch, T> drawItem,
            float itemHeight = UIUtility.ButtonHeight,
            int? maxItemsVisibleAtOnce = null,
            Func<T, string>? itemKey = null,
            int overscan = 2,
            Func<T, int, Action<Rect>?>? rowDraw = null,
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);

            var count = items.Count;
            var gapY = style?.gap?.Height.value ?? 0f;
            var step = itemHeight + gapY;

            var contentHeight = count > 0 ? count * itemHeight + (count - 1) * gapY : itemHeight;
            var visibleRows = Mathf.Clamp(count, 1, maxItemsVisibleAtOnce ?? Math.Max(count, 1));
            var viewportHeight = visibleRows * itemHeight + Math.Max(0, visibleRows - 1) * gapY;

            return branch.Div(viewport =>
            {
                // The scroll position as of the previous frame's draw. On the very first build there is none yet,
                // and the declared height is the best guess of what is visible.
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

                // The first row built is pushed down by the rows above it that are not built.
                var rowStyle = ListRowStyles.Get((itemHeight, 0f));
                var firstRowStyle = ListRowStyles.Get((itemHeight, first * step));

                // The content keeps the full height of all rows, so the scroll view is sized as if they were all there.
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
                        content.Div(row => drawItem(row, item), draw: rowDraw?.Invoke(item, i),
                            style: i == first ? firstRowStyle : rowStyle, id: itemKey?.Invoke(item) ?? $"i{i}");
                    }
                }, style: ListContentStyles.Get((contentHeight, gapY)));
            }, style: (style ?? new Style()).Merge(ListViewportStyles.Get(viewportHeight)), id: key);
        }
    }
}
