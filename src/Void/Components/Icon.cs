using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

public static partial class VoidComponents
{
    private static float IconMetrics(ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => 8f,
            ComponentSize.Default => 18f,
            ComponentSize.Large => GenUI.SmallIconSize,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    #region CACHE
    private static readonly StyleCache<ComponentSize> IconStyles = new(size =>
    {
        var iconSize = IconMetrics(size);
        return new Style
        {
            width = Dimension.Px(iconSize),
            height = Dimension.Px(iconSize),
            alignSelf = TaffyAlignItems.Center,
            flexShrink = 0f
        };
    });
    #endregion

    extension(UIBranch branch)
    {
        public void Icon(Texture2D icon,
            Color? iconColor = null, ComponentSize size = ComponentSize.Default,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = id ?? $"{file}_{line}";

            var baseStyle = IconStyles.Get(size);
            var mergedStyle = style == null ? baseStyle : style.Merge(baseStyle);

            branch.Div(draw: r =>
            {
                using (new GUIColor(iconColor ?? Color.white))
                {
                    GUI.DrawTexture(r, icon);
                }
            }, style: mergedStyle, id: key);
        }
    }
}
