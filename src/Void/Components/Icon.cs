using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

public static partial class VoidComponents
{
    private static float ResolveIconSize(ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => 8f,
            ComponentSize.Default => 18f,
            ComponentSize.Large => GenUI.SmallIconSize,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    extension(UIBranch branch)
    {
        public void Icon(Texture2D icon,
            Color? iconColor = null, ComponentSize size = ComponentSize.Default,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = id ?? $"{file}_{line}";
            var iconSize = ResolveIconSize(size);

            var mergedStyle = (style ?? new Style()).Merge(new Style
            {
                width = Dimension.Px(iconSize),
                height = Dimension.Px(iconSize),
                alignSelf = TaffyAlignItems.Center
            });

            // Capture for closure.
            var capturedIcon = icon;
            var capturedColor = iconColor;

            branch.Div(draw: r =>
            {
                using (new GUIColor(capturedColor ?? Color.white))
                {
                    GUI.DrawTexture(r, capturedIcon);
                }
            }, style: mergedStyle, id: key);
        }
    }
}
