using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

public static partial class VoidComponents
{
    private sealed class ListState
    {
        public Vector2 ScrollPos;
    }

    extension(UIBranch branch)
    {
        /// <summary>
        /// Adds a fixed-height scrollable list with virtualized rendering.
        /// All items must share the same <paramref name="itemHeight" />.
        /// <code>
        /// col.List(categories, (rect, cat) =>
        /// {
        ///     var selected = _set.Contains(cat);
        ///     Widgets.CheckboxLabeled(rect, cat, ref selected);
        ///     if (selected != _set.Contains(cat)) { ... }
        /// });
        /// </code>
        /// TODO: Refactor to use components inside of childs as well.
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
            var key = id ?? $"{file}_{line}";

            var state = branch.State(key, () => new ListState { ScrollPos = new Vector2() });

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

                Verse.Widgets.BeginScrollView(r, ref state.ScrollPos, viewRect);

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
                        Verse.Widgets.Label(viewRect, "No results available.");
                    }
                }

                Verse.Widgets.EndScrollView();
            }, style: mergedStyle);

        }
    }
}
