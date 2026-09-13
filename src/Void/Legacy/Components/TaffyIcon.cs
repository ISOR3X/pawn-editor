using Taffy;
using UnityEngine;
using Verse;

namespace Void.Components;

public static partial class TaffyExtensions
{
    private static float ResolveIconSize(UIUtility.ComponentSize size)
    {
        return size switch
        {
            UIUtility.ComponentSize.Small => 8f,
            UIUtility.ComponentSize.Default => 18f,
            UIUtility.ComponentSize.Large => GenUI.SmallIconSize,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>
    ///     Adds a button with auto-computed width.
    /// </summary>
    public static void Icon(this TaffyBuilder b, Texture2D icon,
        Color? iconColor = null, UIUtility.ComponentSize size = UIUtility.ComponentSize.Default,
        StyleOverride? style = null)
    {
        var iconSize = ResolveIconSize(size);

        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = Dimension.Px(iconSize),
            height = Dimension.Px(iconSize),
            alignSelf = TaffyAlignItems.Center
        });

        // Capture for closure.
        var capturedIcon = icon;
        var capturedColor = iconColor;

        b.Item(r =>
        {
            using (new GUIColor(capturedColor ?? Color.white))
            {
                GUI.DrawTexture(r, capturedIcon);
            }
        }, mergedStyle);
    }
}