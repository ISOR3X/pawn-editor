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

        b.AddNode(mergedStyle, r =>
        {
            using (new TextBlock(mergedStyle.fontSize, anchor, color ?? Color.white))
            {
                Verse.Text.WordWrap = wrap ?? r.width < Verse.Text.CalcSize(text).x;
                var displayText = wrap == false ? text.Truncate(r.width) : text;
                Verse.Widgets.Label(r, displayText);
                Verse.Text.WordWrap = true;
            }

            if (onHover != null && Mouse.IsOver(r))
                onHover(r);
            // }, Measure);
        });

        /*
        return;

        (float w, float h) Measure(TaffyMeasureMode widthMode, float width, TaffyMeasureMode heightMode, float height)
        {
            using (new TextBlock(mergedStyle.fontSize ?? GameFont.Small))
            {
                switch (widthMode)
                {
                    case TaffyMeasureMode.Exact:
                        // Width fully constrained — wrap and measure height.
                        return (width, WrapHeight(text, width, wrap));

                    case TaffyMeasureMode.MinContent:
                        // Return the widest unbreakable word (CSS min-width:auto).
                        // Cached so Text.CalcSize is called at most once per (word, font) per session.
                        var minW = 0f;
                        foreach (var word in text.Split(' '))
                        {
                            var key = (word, mergedStyle.fontSize ?? GameFont.Small);
                            if (!TaffyBuilder.WordWidthCache.TryGetValue(key, out var w))
                                TaffyBuilder.WordWidthCache[key] = w = Verse.Text.CalcSize(word).x;
                            if (w > minW) minW = w;
                        }
                        return (minW, WrapHeight(text, minW, wrap));

                    case TaffyMeasureMode.FitContent:
                        // Definite available width — wrap at that width.
                        return (width, WrapHeight(text, width, wrap));

                    default:
                        // MaxContent / unconstrained — return natural (unwrapped) size.
                        var sz = Verse.Text.CalcSize(text);
                        return (sz.x, sz.y);
                }
            }

            static float WrapHeight(string t, float w, bool? wrapOverride)
                => wrapOverride == false ? Verse.Text.CalcSize(t).y : Verse.Text.CalcHeight(t, w);
        }
        */
    }
}