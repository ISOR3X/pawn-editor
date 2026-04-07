using Taffy;
using UnityEngine;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    private static float ResolveIconSize(UIUtility.ComponentSize size)
    {
        return size switch
        {
            UIUtility.ComponentSize.Small => 12f,
            UIUtility.ComponentSize.Default => ButtonIconSize,
            UIUtility.ComponentSize.Large => 24f,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }
    
    /// <summary>
    /// Adds a button with auto-computed width.
    /// </summary>
    public static void Icon(this TaffyBuilder b, Texture2D icon,
        Color? iconColor = null, Style? style = null, UIUtility.ComponentSize size = UIUtility.ComponentSize.Default)
    {
        style ??= new Style();
        var iconSize = ResolveIconSize(size);

        style = style.WithDefaults(new Style
        {
            size = new Size<Dimension>(Dimension.Length(iconSize), Dimension.Length(iconSize)),
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