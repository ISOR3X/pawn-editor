using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

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
    /// Adds a button with auto-computed width.
    /// </summary>
    public static void Icon(this TaffyBuilder b, Texture2D icon,
        Color? iconColor = null, UIUtility.ComponentSize size = UIUtility.ComponentSize.Default,
        StyleOverride? style = null)
    {
        var iconSize = ResolveIconSize(size);

        var resolvedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = iconSize,
            height = iconSize,
            alignSelf = AlignItems.Center
        }).Resolve();

        // Capture for closure.
        var capturedIcon = icon;
        var capturedColor = iconColor;

        b.AddLeaf(resolvedStyle, r =>
        {
            using (new GUIColor(capturedColor ?? Color.white))
                GUI.DrawTexture(r, capturedIcon);
        });
    }
}