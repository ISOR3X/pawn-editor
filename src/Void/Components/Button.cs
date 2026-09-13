using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

public static partial class VoidComponents
{
    public enum ButtonVariant
    {
        Solid = 0,
        Ghost = 1
    }

    /// <returns>Button padding, button height, icon height, icon + label gap, font size</returns>
    private static (float, float, float, float, GameFont) ResolveButtonSize(ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => (12f, 20f, 12f, 4f, GameFont.Tiny),
            ComponentSize.Default => (GenUI.GapLabel, UIUtility.ButtonHeight, 18f, 6f,
                GameFont.Small),
            ComponentSize.Large => (52f, Verse.Widgets.BackButtonHeight, 18f, 6f,
                GameFont.Small),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>
    /// Copy of <see cref="Verse.Widgets.DrawButtonGraphic" />, but with a disabled flag to disable interaction states.
    /// </summary>
    private static void DrawButtonGraphic(Rect rect, bool disabled)
    {
        var atlas = Verse.Widgets.ButtonBGAtlas;

        if (Mouse.IsOver(rect) && !disabled)
        {
            atlas = Verse.Widgets.ButtonBGAtlasMouseover;
            if (Input.GetMouseButton(0)) atlas = Verse.Widgets.ButtonBGAtlasClick;
        }

        Verse.Widgets.DrawAtlas(rect, atlas);
    }

    extension(UIBranch branch)
    {
        public TaffyNode Button(string? label = null, Texture2D? icon = null,
            Color? iconColor = null, Action<Rect>? onClick = null, Action<Rect>? onHover = null,
            bool block = false, bool disabled = false, ComponentSize size = ComponentSize.Default,
            ButtonVariant variant = ButtonVariant.Solid,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = id ?? $"{file}_{line}";

            var (padding, height, iconSize, iconGap, fontSize) = ResolveButtonSize(size);

            // Override so there's no padding if we only have an icon.
            var iconOnly = label == null && icon != null;
            var paddingInline = iconOnly ? 0f : padding;

            var mergedStyle = (style ?? new Style()).Merge(new Style
            {
                height = Dimension.Px(height),
                display = TaffyDisplay.Flex,
                alignItems = TaffyAlignItems.Center,
                justifyContent = TaffyAlignContent.Center,
                gap = new TaffyAxes(Dimension.Px(iconGap), Dimension.Px(0f)),
                padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(paddingInline), Dimension.Px(0f), Dimension.Px(paddingInline))
            });

            // By default, the button has a fixed size. Setting it to block makes it width: 100%.
            // This is inspired by the API for https://ui.nuxt.com/docs/components/button
            if (block) mergedStyle.width = Dimension.Percent(1f);
            if (iconOnly) mergedStyle.width = Dimension.Px(height);

            return branch.Div(draw: r =>
            {
                var clicked = Verse.Widgets.ButtonInvisible(r);

                if (variant == ButtonVariant.Solid) DrawButtonGraphic(r, disabled);
                else Verse.Widgets.DrawHighlightIfMouseover(r);

                if (onHover != null)
                    if (Mouse.IsOver(r))
                        onHover(r);

                if (clicked) onClick?.Invoke(r);

                if (disabled) Verse.Widgets.DrawBoxSolid(r, Color.black with { a = 0.25f });

            }, builder: b =>
            {
                if (icon != null)
                    b.Div(style: new Style { width = Dimension.Px(iconSize), height = Dimension.Px(iconSize), flexShrink = 0f },
                        draw: r => GUI.DrawTexture(r, icon));
                if (label != null)
                    b.Text(label,
                        style: new Style
                        {
                            flexShrink = 1f,
                            minWidth = Dimension.Px(0),
                            wordWrap = false,
                            fontSize = fontSize,
                        });
            }, style: mergedStyle, id: key);
        }
    }
}
