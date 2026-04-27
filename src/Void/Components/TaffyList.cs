using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;

namespace Void.Components;

public static partial class TaffyExtensions
{
    private static readonly Dictionary<string, Vector2> SScrollPositions = new();

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
    /// </summary>
    public static void List<T>(
        this TaffyBuilder b,
        IReadOnlyList<T> items,
        Action<Rect, T> drawItem,
        float itemHeight = UIUtility.ButtonHeight,
        StyleOverride? style = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";

        var gapY = style?.gap?.Height.Value ?? 0f;
        var clampedCount = Mathf.Clamp(items.Count, 1, 6);
        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = Dimension.Percent(1f),
            height = clampedCount * itemHeight + Math.Max(0, clampedCount - 1) * gapY
        });

        var capturedItems = items;
        b.Item(r =>
        {
            SScrollPositions.TryGetValue(key, out var sp);

            var step = itemHeight + gapY;
            var totalHeight = capturedItems.Count > 0
                ? capturedItems.Count * itemHeight + (capturedItems.Count - 1) * gapY
                : itemHeight;
            var scrollbarW = totalHeight > r.height ? UIUtility.ScrollBarWidth + 4f : 0f;
            var viewRect = new Rect(0f, 0f, r.width - scrollbarW, totalHeight);

            Verse.Widgets.BeginScrollView(r, ref sp, viewRect);

            if (capturedItems.Count > 0)
            {
                var visibleTop = sp.y;
                var visibleBottom = sp.y + r.height;
                var first = Math.Max(0, (int)(visibleTop / step));
                var last = Math.Min(capturedItems.Count - 1, (int)(visibleBottom / step));

                for (var i = first; i <= last; i++)
                    drawItem(new Rect(0f, i * step, viewRect.width, itemHeight), capturedItems[i]);
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
            SScrollPositions[key] = sp;
        }, mergedStyle);
    }

    public static void HorizontalList<T>(
        this TaffyBuilder b,
        IReadOnlyList<T> items,
        Action<Rect, T> drawItem,
        float itemWidth = UIUtility.ButtonHeight,
        StyleOverride? style = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";

        var gapX = style?.gap?.Width.Value ?? 0f;
        var clampedCount = Mathf.Clamp(items.Count, 1, 6);
        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            height = Dimension.Percent(1f),
            width = clampedCount * itemWidth + Math.Max(0, clampedCount - 1) * gapX
        });

        var capturedItems = items;
        b.Item(r =>
        {
            SScrollPositions.TryGetValue(key, out var sp);

            var step = itemWidth + gapX;
            var totalWidth = capturedItems.Count > 0
                ? capturedItems.Count * itemWidth + (capturedItems.Count - 1) * gapX
                : itemWidth;
            var scrollbarH = totalWidth > r.width ? UIUtility.ScrollBarWidth + 4f : 0f;
            var viewRect = new Rect(0f, 0f, totalWidth, r.height - scrollbarH);

            Verse.Widgets.BeginScrollView(r, ref sp, viewRect);

            if (capturedItems.Count > 0)
            {
                var visibleStart = sp.x;
                var visibleEnd = sp.x + r.width;
                var first = Math.Max(0, (int)(visibleStart / step));
                var last = Math.Min(capturedItems.Count - 1, (int)(visibleEnd / step));

                for (var i = first; i <= last; i++)
                    drawItem(new Rect(i * step, 0f, itemWidth, viewRect.height), capturedItems[i]);
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
            SScrollPositions[key] = sp;
        }, mergedStyle);
    }
}