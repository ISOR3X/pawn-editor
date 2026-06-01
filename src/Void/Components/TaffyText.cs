using Taffy;
using UnityEngine;
using Verse;

namespace Void.Components;

public static partial class TaffyExtensions
{
    /// <summary>
    ///     Adds a leaf node that measures its own size using RimWorld's <see cref="Text.CalcSize" />
    ///     and <see cref="Text.CalcHeight" />.
    ///     <para>
    ///         When a fixed <paramref name="width" /> is set the node wraps at that width and the height
    ///         is computed via <see cref="Text.CalcHeight" />. Otherwise, the natural (unwrapped) size from
    ///         <see cref="Text.CalcSize" /> is returned, capped at the available width if the axis is definite.
    ///     </para>
    ///     The default draw callback renders the text as a label.
    /// </summary>
    public static void Text(this TaffyBuilder b, string text,
        TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null, bool? wrap = null, Action<Rect>? onHover = null,
        StyleOverride? style = null)
    {
        var mergedStyle = style ?? new StyleOverride();

        var node = b.tree.NewLeafWithContext(mergedStyle.Resolve(),
            (Func<Size<float?>, Size<AvailableSpace>, Size<float>>)Measure);

        b.children.Add(node);
        b.callbacks.Add((node, r =>
        {
            using (new TextBlock(mergedStyle.fontSize, anchor, color ?? Color.white))
            {
                var font = mergedStyle.fontSize ?? GameFont.Small;
                Verse.Text.WordWrap = wrap ?? r.width < GetNaturalTextSize(text, font).x;
                var displayText = wrap == false ? text.Truncate(r.width) : text;
                Verse.Widgets.Label(r, displayText);
                Verse.Text.WordWrap = true; // WordWrap is true by default
            }

            if (onHover != null)
                if (Mouse.IsOver(r))
                    onHover(r);
        }));
        return;

        // Store a per-node measure closure as the node's context object.
        // The tree-level dispatch in Execute will cast it and call it.
        Size<float> Measure(Size<float?> known, Size<AvailableSpace> available)
        {
            var font = mergedStyle.fontSize ?? GameFont.Small;
            using (new TextBlock(font))
            {
                if (known.Width.HasValue)
                    // Width fully constrained by parent algorithm - wrap and measure height.
                    return new Size<float>(known.Width.Value, MeasureHeight(text, font, known.Width.Value, wrap));

                if (available.Width.IsMinContent)
                {
                    // Min-content query: return the widest unbreakable word.
                    // This mirrors CSS min-width:auto - text can shrink and wrap, but never
                    // below the width of its longest word (which for single-word labels equals
                    // the full text width, preventing unwanted shrinkage).
                    // Results are cached in _wordWidthCache so Text.CalcSize is called at most
                    // once per (word, font) pair across all frames.
                    var minW = 0f;
                    foreach (var word in text.Split(' '))
                    {
                        var key = (word, font);
                        if (!TaffyBuilder.WordWidthCache.TryGetValue(key, out var w))
                            TaffyBuilder.WordWidthCache[key] = w = Verse.Text.CalcSize(word).x;
                        if (w > minW) minW = w;
                    }


                    return new Size<float>(minW, MeasureHeight(text, font, minW, wrap));
                }

                // Definite available width - wrap at that width.
                if (available.Width.IntoOption() is { } aw) return new Size<float>(aw, MeasureHeight(text, font, aw, wrap));

                // MaxContent / unconstrained - return natural (unwrapped) size.
                var sz = GetNaturalTextSize(text, font);
                return new Size<float>(sz.x, sz.y);
            }

            // Returns height at natural width if wrap is false.
            static float MeasureHeight(string text, GameFont font, float width, bool? wrap)
            {
                if (wrap == false) return GetNaturalTextSize(text, font).y;

                var cacheKey = (text, font, Mathf.CeilToInt(width));
                if (TaffyBuilder.TextHeightCache.TryGetValue(cacheKey, out var cachedHeight)) return cachedHeight;

                var height = Verse.Text.CalcHeight(text, width);
                TaffyBuilder.TextHeightCache[cacheKey] = height;
                return height;
            }
        }

        static Vector2 GetNaturalTextSize(string text, GameFont font)
        {
            var cacheKey = (text, font);
            if (TaffyBuilder.TextSizeCache.TryGetValue(cacheKey, out var cachedSize)) return cachedSize;

            using (new TextBlock(font))
            {
                var size = Verse.Text.CalcSize(text);
                TaffyBuilder.TextSizeCache[cacheKey] = size;
                return size;
            }
        }
    }
}