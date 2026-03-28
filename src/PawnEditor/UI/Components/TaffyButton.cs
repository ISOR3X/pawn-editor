using System;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    private const float ButtonIconSize = 24f;
    private const float ButtonIconGap = 4f;

    /// <summary>
    /// Adds a button with auto-computed width.
    /// Width = <see cref="UIUtility.LabelPadding"/> × 2 + label width + gap + icon width (24 px).
    /// Height is always <see cref="UIUtility.ButtonHeight"/>.
    /// Both <paramref name="label"/> and <paramref name="icon"/> are optional.
    /// </summary>
    public static void Button(this TaffyBuilder b, string? label = null, Texture2D? icon = null,
        Color? iconColor = null, Action? onClick = null)
    {
        // Measure label width at build time (cached across frames).
        var labelW = 0f;
        if (label != null)
        {
            using (new TextBlock(GameFont.Small))
            {
                var key = (label, GameFont.Small);
                if (!TaffyBuilder.WordWidthCache.TryGetValue(key, out labelW))
                    TaffyBuilder.WordWidthCache[key] = labelW = Verse.Text.CalcSize(label).x;
            }
        }

        var totalW = UIUtility.LabelPadding
                     + labelW
                     + (label != null && icon != null ? ButtonIconGap : 0f)
                     + (icon != null ? ButtonIconSize : 0f)
                     + UIUtility.LabelPadding;

        var style = new Style
        {
            size = new Size<Dimension>(
                Dimension.Length(totalW),
                Dimension.Length(UIUtility.ButtonHeight))
        };

        // Capture for closure.
        var capturedLabel = label;
        var capturedIcon = icon;
        var capturedColor = iconColor;
        var capturedLabelW = labelW;

        b.AddLeaf(style, r =>
        {
            var clicked = Verse.Widgets.ButtonInvisible(r);
            Verse.Widgets.DrawButtonGraphic(r);

            if (capturedLabel != null)
            {
                using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
                    Verse.Widgets.Label(
                        new Rect(r.x + UIUtility.LabelPadding, r.y, capturedLabelW, r.height),
                        capturedLabel);
            }

            if (capturedIcon != null)
            {
                var ix = capturedLabel != null
                    ? r.x + UIUtility.LabelPadding + capturedLabelW + ButtonIconGap
                    : r.x + (r.width - ButtonIconSize) / 2f;
                var iy = r.y + (r.height - ButtonIconSize) / 2f;
                using (new GUIColor(capturedColor ?? Color.white))
                    GUI.DrawTexture(new Rect(ix, iy, ButtonIconSize, ButtonIconSize), capturedIcon);
            }

            if (clicked) onClick?.Invoke();
        });
    }
}