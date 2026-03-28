using HotSwap;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class TaffyExtensions
{
    private const float ButtonIconSize = 24f;
    private const float ButtonIconGap = 4f;
    private const float ButtonPadding = UIUtility.ButtonPadding;

    /// <summary>
    /// Adds a button with auto-computed width.
    /// Both <paramref name="label"/> and <paramref name="icon"/> are optional.
    /// </summary>
    public static void Button(this TaffyBuilder b, string? label = null, Texture2D? icon = null,
        Color? iconColor = null, Action? onClick = null, Action<Rect>? onHover = null,
        float paddingInline = ButtonPadding, Style? style = null)
    {
        style ??= new Style();

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

        var unpaddedW = labelW + (label != null && icon != null ? ButtonIconGap : 0f) +
                        (icon != null ? ButtonIconSize : 0f);
        var totalW = unpaddedW + paddingInline * 2f;

        style = style.WithDefaults(new Style
        {
            size = new Size<Dimension>(Dimension.Length(totalW), Dimension.Length(UIUtility.ButtonHeight))
        });


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
                var xOffset = capturedIcon != null ? ButtonIconSize + ButtonIconGap : 0f;
                using (new TextBlock(GameFont.Small, TextAnchor.MiddleCenter, false))
                    Verse.Widgets.Label(r with { xMin = r.xMin - xOffset },
                        capturedLabel.Truncate(r.width - ButtonPadding));
            }

            if (capturedIcon != null)
            {
                var ix = capturedLabel != null
                    ? r.x + paddingInline + capturedLabelW + ButtonIconGap
                    : r.x + (r.width - ButtonIconSize) / 2f;
                var iy = r.y + (r.height - ButtonIconSize) / 2f;
                using (new GUIColor(capturedColor ?? Color.white))
                    GUI.DrawTexture(new Rect(ix, iy, ButtonIconSize, ButtonIconSize), capturedIcon);
            }

            if (onHover != null)
            {
                if (Mouse.IsOver(r)) onHover(r);
            }

            if (clicked) onClick?.Invoke();
        });
    }
}