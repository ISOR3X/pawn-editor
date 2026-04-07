using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    private static readonly Dictionary<string, Vector2> SScrollPositions = new();

    /// <summary>
    /// Adds a fixed-height scrollable list with virtualized rendering.
    /// All items must share the same <paramref name="itemHeight"/>.
    /// <code>
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
        Style? style = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";

        style ??= new Style();
        style = style.WithDefaults(new Style
        {
            size = new Size<Dimension>(Dimension.Percent(1f), Dimension.Length(200f))
        });

        var capturedItems = items;
        b.AddLeaf(style, r =>
        {
            SScrollPositions.TryGetValue(key, out var sp);

            var totalHeight = Math.Max(capturedItems.Count, 1) * itemHeight;
            var viewRect = new Rect(0f, 0f, r.width - UIUtility.ScrollBarWidth, totalHeight);

            Verse.Widgets.BeginScrollView(r, ref sp, viewRect);

            if (capturedItems.Count > 0)
            {
                var visibleTop = sp.y;
                var visibleBottom = sp.y + r.height;
                var first = Math.Max(0, (int)(visibleTop / itemHeight));
                var last = Math.Min(capturedItems.Count - 1, (int)(visibleBottom / itemHeight));

                for (var i = first; i <= last; i++)
                    drawItem(new Rect(0f, i * itemHeight, viewRect.width, itemHeight), capturedItems[i]);
            }
            else
            {
                using (new GUIColor(ColoredText.SubtleGrayColor))
                using (new TextBlock(TextAnchor.MiddleLeft))
                    Verse.Widgets.Label(viewRect, "No results available.");
            }

            Verse.Widgets.EndScrollView();
            SScrollPositions[key] = sp;
        });
    }
}