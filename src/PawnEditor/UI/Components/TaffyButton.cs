using HotSwap;
using PawnEditor.Extensions;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class TaffyExtensions
{
    /// <returns>Button padding, button height, icon height, icon + label gap, font size</returns>
    private static (float, float, float, float, GameFont) ResolveButtonSize(UIUtility.ComponentSize size)
    {
        return size switch
        {
            UIUtility.ComponentSize.Small => (12f, 20f, 12f, 4f, GameFont.Tiny),
            UIUtility.ComponentSize.Default => (GenUI.GapLabel, UIUtility.ButtonHeight, 18f, 6f,
                GameFont.Small),
            UIUtility.ComponentSize.Large => (52f, Verse.Widgets.BackButtonHeight, 18f, 6f,
                GameFont.Small),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>
    ///     Adds a button with auto-computed width.
    ///     Both <paramref name="label" /> and <paramref name="icon" /> are optional.
    /// </summary>
    public static void Button(this TaffyBuilder b, string? label = null, Texture2D? icon = null,
        Color? iconColor = null, Action<Rect>? onClick = null, Action<Rect>? onHover = null,
        bool drawGraphic = true,
        bool block = false, UIUtility.ComponentSize size = UIUtility.ComponentSize.Default, StyleOverride? style = null)
    {
        var (padding, height, iconSize, iconGap, fontSize) = ResolveButtonSize(size);

        // Override so there's no padding if we only have an icon.
        var paddingInline = label == null && icon != null ? iconGap : padding;

        // Measure label width at build time (cached across frames).
        var labelW = 0f;
        if (label != null)
            using (new TextBlock(fontSize))
            {
                var key = (label, font: fontSize);
                if (!TaffyBuilder.WordWidthCache.TryGetValue(key, out labelW))
                    TaffyBuilder.WordWidthCache[key] = labelW = Verse.Text.CalcSize(label).x;
            }

        var unpaddedW = labelW + (label != null && icon != null ? iconGap : 0f) +
                        (icon != null ? iconSize : 0f);
        var totalW = unpaddedW + paddingInline * 2f;

        // By default, the button is fixed size. Setting it to block makes it width: 100%.
        // This is inspired by the API for https://ui.nuxt.com/docs/components/button
        var resolvedStyle = (style ?? new StyleOverride()).Merge(block
            ? new StyleOverride
            {
                width = Dimension.Percent(1f),
                height = height,
                minWidth = unpaddedW
            }
            : new StyleOverride
            {
                width = totalW,
                height = height
            }).Resolve();

        // Capture for closure.
        var capturedLabel = label;
        var capturedIcon = icon;
        var capturedColor = iconColor;
        var capturedLabelW = labelW;

        b.AddLeaf(resolvedStyle, r =>
        {
            var clicked = Verse.Widgets.ButtonInvisible(r);
            if (drawGraphic) Verse.Widgets.DrawButtonGraphic(r);
            else Verse.Widgets.DrawHighlightIfMouseover(r);

            if (capturedIcon != null || capturedLabel != null)
            {
                var contentW = (capturedIcon != null ? iconSize : 0f)
                               + (capturedIcon != null && capturedLabel != null ? iconGap : 0f)
                               + capturedLabelW;
                var groupX = r.xMin + (r.width - contentW) / 2f;

                if (capturedIcon != null)
                    using (new GUIColor(capturedColor ?? Color.white))
                    {
                        GUI.DrawTexture(
                            r.CenteredVertically(iconSize) with { xMin = groupX, width = iconSize },
                            capturedIcon);
                    }

                if (capturedLabel != null)
                {
                    var labelX = groupX + (capturedIcon != null ? iconSize + iconGap : 0f);
                    using (new TextBlock(fontSize, TextAnchor.MiddleLeft, false))
                    {
                        Verse.Widgets.Label(r with { xMin = labelX, width = capturedLabelW },
                            capturedLabel.Truncate(capturedLabelW));
                    }
                }
            }


            if (onHover != null)
                if (Mouse.IsOver(r))
                    onHover(r);

            if (clicked) onClick?.Invoke(r);
        });
    }
}