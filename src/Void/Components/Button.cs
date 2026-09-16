using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;
using Widgets = Verse.Widgets;

public static partial class VoidComponents
{
    public enum ButtonVariant
    {
        Solid = 0,
        Ghost = 1
    }


    // Cache base styles to that they are not created newly on every button call.
    // Note that these return mutable values and should therefore not be modified.
    private static readonly StyleCache<(ComponentSize size, bool block, bool iconOnly)> ButtonStyles = new(k =>
    {
        var m = ButtonMetrics.For(k.size);
        // No padding if we only have an icon; the button is square instead.
        var paddingInline = k.iconOnly ? 0f : m.Padding;
        var s = new Style
        {
            height = Dimension.Px(m.Height),
            display = TaffyDisplay.Flex,
            alignItems = TaffyAlignItems.Center,
            justifyContent = TaffyAlignContent.Center,
            gap = new TaffyAxes(Dimension.Px(m.IconGap), Dimension.Px(0f)),
            padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(paddingInline), Dimension.Px(0f),
                Dimension.Px(paddingInline))
        };
        // By default, the button has a fixed size. Setting it to block makes it width: 100%.
        // This is inspired by the API for https://ui.nuxt.com/docs/components/button
        if (k.block) s.width = Dimension.Percent(1f);
        if (k.iconOnly) s.width = Dimension.Px(m.Height);
        return s;
    });

    private static readonly StyleCache<ComponentSize> ButtonLabelStyles = new(size => new Style
    {
        flexShrink = 1f,
        minWidth = Dimension.Px(0),
        wordWrap = false,
        fontSize = ButtonMetrics.For(size).Font
    });

    /// <summary>
    ///     Copy of <see cref="Verse.Widgets.DrawButtonGraphic" />, but with a disabled flag to disable interaction states.
    /// </summary>
    private static void DrawButtonGraphic(Rect rect, bool disabled)
    {
        var atlas = Widgets.ButtonBGAtlas;

        if (Mouse.IsOver(rect) && !disabled)
        {
            atlas = Widgets.ButtonBGAtlasMouseover;
            if (UnityEngine.Input.GetMouseButton(0)) atlas = Widgets.ButtonBGAtlasClick;
        }

        Widgets.DrawAtlas(rect, atlas);
    }

    /// <summary>
    ///     Fixed sizing for one <see cref="ComponentSize" /> of button.
    ///     This ensures consistent styling.
    /// </summary>
    private readonly record struct ButtonMetrics(
        float Padding,
        float Height,
        float IconGap,
        GameFont Font)
    {
        public static ButtonMetrics For(ComponentSize size)
        {
            return size switch
            {
                ComponentSize.Small => new ButtonMetrics(12f, 20f, 4f, GameFont.Tiny),
                ComponentSize.Default => new ButtonMetrics(GenUI.GapLabel, UIUtility.ButtonHeight, 6f,
                    GameFont.Small),
                ComponentSize.Large => new ButtonMetrics(52f, Widgets.BackButtonHeight, 6f, GameFont.Small),
                _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
            };
        }
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
            var key = UIBranch.ResolveKey(id, file, line);
            var iconOnly = label == null && icon != null;

            var baseStyle = ButtonStyles.Get((size, block, iconOnly));
            var mergedStyle = style == null ? baseStyle : style.Merge(baseStyle);

            return branch.Div(draw: r =>
            {
                var clicked = Widgets.ButtonInvisible(r);

                if (variant == ButtonVariant.Solid) DrawButtonGraphic(r, disabled);
                else Widgets.DrawHighlightIfMouseover(r);

                if (onHover != null)
                    if (Mouse.IsOver(r))
                        onHover(r);

                if (clicked) onClick?.Invoke(r);

                if (disabled) Widgets.DrawBoxSolid(r, Color.black with { a = 0.25f });
            }, builder: b =>
            {
                if (icon != null)
                    b.Icon(icon, iconColor, size);
                if (label != null)
                    b.Text(label, ButtonLabelStyles.Get(size));
            }, style: mergedStyle, id: key);
        }
    }
}