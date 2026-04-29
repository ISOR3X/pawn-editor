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
                Verse.Text.WordWrap = wrap ?? r.width < Verse.Text.CalcSize(text).x;
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
            using (new TextBlock(mergedStyle.fontSize ?? GameFont.Small))
            {
                if (known.Width.HasValue)
                    // Width fully constrained by parent algorithm - wrap and measure height.
                    return new Size<float>(known.Width.Value, MinWidth(text, known.Width.Value, wrap));

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
                        var key = (word, mergedStyle.fontSize ?? GameFont.Small);
                        if (!TaffyBuilder.WordWidthCache.TryGetValue(key, out var w))
                            TaffyBuilder.WordWidthCache[key] = w = Verse.Text.CalcSize(word).x;
                        if (w > minW) minW = w;
                    }


                    return new Size<float>(minW, MinWidth(text, minW, wrap));
                }

                // Definite available width - wrap at that width.
                if (available.Width.IntoOption() is { } aw) return new Size<float>(aw, MinWidth(text, aw, wrap));

                // MaxContent / unconstrained - return natural (unwrapped) size.
                var sz = Verse.Text.CalcSize(text);
                return new Size<float>(sz.x, sz.y);
            }

            // Returns height at natural width if wrap is false.
            static float MinWidth(string text, float width, bool? wrap)
            {
                return wrap == false ? Verse.Text.CalcSize(text).y : Verse.Text.CalcHeight(text, width);
            }
        }
    }
}