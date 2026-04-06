using Taffy;
using UnityEngine;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    /// <summary>
    /// Adds a button with auto-computed width.
    /// </summary>
    public static void Icon(this TaffyBuilder b, Texture2D icon,
        Color? iconColor = null, Style? style = null)
    {
        style ??= new Style();

        style = style.WithDefaults(new Style
        {
            size = new Size<Dimension>(Dimension.Length(ButtonIconSize), Dimension.Length(ButtonIconSize)),
            alignSelf = AlignItems.Center
        });

        // Capture for closure.
        var capturedIcon = icon;
        var capturedColor = iconColor;

        b.AddLeaf(style, r =>
        {
            using (new GUIColor(capturedColor ?? Color.white))
                GUI.DrawTexture(r, capturedIcon);
        });
    }
}