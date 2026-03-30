using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class TaffyExtensions
{
    private const float ButtonIconSize = GenUI.SmallIconSize - 4f;
    private const float ButtonIconGap = GenUI.GapTiny + 2f;
    private const float ButtonPadding = UIUtility.ButtonPadding;

    /// <summary>
    /// Adds a button with auto-computed width.
    /// Both <paramref name="label"/> and <paramref name="icon"/> are optional.
    /// </summary>
    public static void Button(this TaffyBuilder b, string? label = null, Texture2D? icon = null,
        Color? iconColor = null, Action<Rect>? onClick = null, Action<Rect>? onHover = null,
        float paddingInline = ButtonPadding, Style? style = null, bool drawGraphic = true)
    {
        style ??= new Style();
        // Override so there's no padding if we only have an icon.
        paddingInline = label == null && icon != null ? ButtonIconGap : paddingInline;

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
            if (drawGraphic) Verse.Widgets.DrawButtonGraphic(r);
            else Verse.Widgets.DrawHighlightIfMouseover(r);

            if (capturedIcon != null || capturedLabel != null)
            {
                var contentW = (capturedIcon != null ? ButtonIconSize : 0f)
                               + (capturedIcon != null && capturedLabel != null ? ButtonIconGap : 0f)
                               + capturedLabelW;
                var groupX = r.xMin + (r.width - contentW) / 2f;

                if (capturedIcon != null)
                {
                    using (new GUIColor(capturedColor ?? Color.white))
                        GUI.DrawTexture(
                            r.CenteredVertically(ButtonIconSize) with { xMin = groupX, width = ButtonIconSize },
                            capturedIcon);
                }

                if (capturedLabel != null)
                {
                    var labelX = groupX + (capturedIcon != null ? ButtonIconSize + ButtonIconGap : 0f);
                    using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, false))
                        Verse.Widgets.Label(r with { xMin = labelX, width = capturedLabelW },
                            capturedLabel.Truncate(capturedLabelW));
                }
            }


            if (onHover != null)
            {
                if (Mouse.IsOver(r)) onHover(r);
            }

            if (clicked) onClick?.Invoke(r);
        });
    }
}