#if DEBUG
using LudeonTK;
using UnityEngine;
using Verse;
using static VoidComponents;

namespace Void.Dev;

/// <summary>
///     Vanilla-API twin of <see cref="Window_Playground" /> for benchmarking. Draws the same tab bar and
///     button playground with plain <see cref="Verse.Widgets" /> calls and manual <see cref="Rect" /> math,
///     so profiler numbers can be compared against the <see cref="Taffy.UITree" /> version. Only the
///     Buttons tab has content; the other tabs exist so the tab bar draws the same number of buttons.
/// </summary>
public class Window_PlaygroundVanilla : Window
{
    private const float Gap = 8f;
    private const float RowGap = 10f;

    private static readonly Tab[] Tabs = (Tab[])Enum.GetValues(typeof(Tab));
    private int _clicks;
    private bool _showExtra;

    private Tab _tab = Tab.Buttons;

    public Window_PlaygroundVanilla()
    {
        resizeable = true;
        draggable = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
    }

    public override Vector2 InitialSize => new(1280f, 860f);

    [DebugAction("Void", "Open vanilla playground", allowedGameStates = AllowedGameStates.Invalid)]
    private static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_PlaygroundVanilla>())
            Find.WindowStack.TryRemove(typeof(Window_PlaygroundVanilla));
        else Find.WindowStack.Add(new Window_PlaygroundVanilla());
    }

    public override void DoWindowContents(Rect inRect)
    {
        var y = inRect.y;
        y = TabBar(inRect, y) + RowGap;

        switch (_tab)
        {
            case Tab.Buttons: ButtonPlayground(inRect, y); break;
            default:
                Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f),
                    $"{_tab}: not implemented in the vanilla playground.");
                break;
        }
    }

    /// <summary>
    ///     Row of tab buttons, a flexible spacer, and a right-aligned overlay toggle.
    /// </summary>
    private float TabBar(Rect inRect, float y)
    {
        var x = inRect.x;
        var height = ButtonMetrics(ComponentSize.Default).height;

        foreach (var tab in Tabs)
            if (DrawButton(ref x, y, tab.ToString(), null, ComponentSize.Default,
                    tab == _tab ? ButtonVariant.Solid : ButtonVariant.Ghost, false))
                _tab = tab;

        // Right-aligned: measure first, then place at the far edge.
        var overlayWidth = MeasureButton("Overlay", null, ComponentSize.Small);
        var smallHeight = ButtonMetrics(ComponentSize.Small).height;
        var ox = inRect.xMax - overlayWidth;
        if (DrawButton(ref ox, y + (height - smallHeight) / 2f, "Overlay", null, ComponentSize.Small,
                ButtonVariant.Solid, false))
            VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug;

        return y + height;
    }

    private void ButtonPlayground(Rect inRect, float y)
    {
        using (new TextBlock(GameFont.Medium))
        {
            var title = $"Button playground (clicks: {_clicks})";
            var h = Text.CalcHeight(title, inRect.width);
            Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, h), title);
            y += h + RowGap;
        }

        // One row per size.
        var x = inRect.x;
        if (DrawButton(ref x, y, "Small", null, ComponentSize.Small, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Small + icon", TexUI.ArrowRight, ComponentSize.Small, ButtonVariant.Solid,
                false)) _clicks++;
        if (DrawButton(ref x, y, null, TexUI.ArrowRight, ComponentSize.Small, ButtonVariant.Solid, false)) _clicks++;
        y += ButtonMetrics(ComponentSize.Small).height + RowGap;

        x = inRect.x;
        if (DrawButton(ref x, y, "Default", null, ComponentSize.Default, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Default + icon", TexUI.ArrowRight, ComponentSize.Default, ButtonVariant.Solid,
                false)) _clicks++;
        if (DrawButton(ref x, y, null, TexUI.ArrowRight, ComponentSize.Default, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Ghost", null, ComponentSize.Default, ButtonVariant.Ghost, false)) _clicks++;
        DrawButton(ref x, y, "Disabled", null, ComponentSize.Default, ButtonVariant.Solid, true);
        y += ButtonMetrics(ComponentSize.Default).height + RowGap;

        x = inRect.x;
        if (DrawButton(ref x, y, "Large", null, ComponentSize.Large, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Large + icon", TexUI.ArrowRight, ComponentSize.Large, ButtonVariant.Solid,
                false)) _clicks++;
        y += ButtonMetrics(ComponentSize.Large).height + RowGap;

        // Block button: full parent width.
        var blockHeight = ButtonMetrics(ComponentSize.Default).height;
        if (DrawButtonRect(new Rect(inRect.x, y, inRect.width, blockHeight), _showExtra ? "Hide extra" : "Show extra",
                null, ComponentSize.Default, ButtonVariant.Solid, false))
            _showExtra = !_showExtra;
        y += blockHeight + RowGap;

        if (!_showExtra) return;

        // Toggled block: padded column with a wrapping label and a truncated long button.
        const float padding = 8f;
        const float innerGap = 6f;
        var inner = new Rect(inRect.x + padding, y + padding, inRect.width - padding * 2f, 0f);

        const string text =
            "This block is added and removed by the button above. A very long label follows to check truncation:";
        var textHeight = Text.CalcHeight(text, inner.width);
        Verse.Widgets.Label(new Rect(inner.x, inner.y, inner.width, textHeight), text);

        var bx = inner.x;
        DrawButton(ref bx, inner.y + textHeight + innerGap,
            "This label is far too long for the space it has been given and should truncate",
            TexUI.ArrowLeft, ComponentSize.Default, ButtonVariant.Solid, false, 220f);
    }

    /// <returns>Horizontal padding, button height, icon size, icon + label gap, font.</returns>
    private static (float padding, float height, float iconSize, float iconGap, GameFont font) ButtonMetrics(
        ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => (12f, 20f, 12f, 4f, GameFont.Tiny),
            ComponentSize.Default => (GenUI.GapLabel, UIUtility.ButtonHeight, 18f, 6f, GameFont.Small),
            ComponentSize.Large => (52f, Verse.Widgets.BackButtonHeight, 18f, 6f, GameFont.Small),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>
    ///     Intrinsic width of a button: padding on both sides, the icon, the gap, and the measured label.
    ///     Icon-only buttons are square.
    /// </summary>
    private static float MeasureButton(string? label, Texture2D? icon, ComponentSize size)
    {
        var (padding, height, iconSize, iconGap, font) = ButtonMetrics(size);
        if (label == null && icon != null) return height;

        var width = padding * 2f;
        if (icon != null) width += iconSize + iconGap;
        if (label != null)
            using (new TextBlock(font))
            {
                width += Text.CalcSize(label).x;
            }

        return width;
    }

    /// <summary>
    ///     Draws a button at the cursor and advances it by the button width plus the row gap.
    /// </summary>
    private static bool DrawButton(ref float x, float y, string? label, Texture2D? icon, ComponentSize size,
        ButtonVariant variant, bool disabled, float? maxWidth = null)
    {
        var width = MeasureButton(label, icon, size);
        if (maxWidth is { } max) width = Mathf.Min(width, max);
        var rect = new Rect(x, y, width, ButtonMetrics(size).height);
        x += width + Gap;
        return DrawButtonRect(rect, label, icon, size, variant, disabled);
    }

    /// <summary>
    ///     Same drawing as the VoidComponents Button: invisible hit box, atlas or hover highlight,
    ///     centered icon + label, dark overlay when disabled.
    /// </summary>
    private static bool DrawButtonRect(Rect rect, string? label, Texture2D? icon, ComponentSize size,
        ButtonVariant variant, bool disabled)
    {
        var (padding, _, iconSize, iconGap, font) = ButtonMetrics(size);
        var iconOnly = label == null && icon != null;
        if (iconOnly) padding = 0f;

        var clicked = Verse.Widgets.ButtonInvisible(rect);

        if (variant == ButtonVariant.Solid)
        {
            var atlas = Verse.Widgets.ButtonBGAtlas;
            if (Mouse.IsOver(rect) && !disabled)
            {
                atlas = Verse.Widgets.ButtonBGAtlasMouseover;
                if (Input.GetMouseButton(0)) atlas = Verse.Widgets.ButtonBGAtlasClick;
            }

            Verse.Widgets.DrawAtlas(rect, atlas);
        }
        else
        {
            Verse.Widgets.DrawHighlightIfMouseover(rect);
        }

        // Content: icon then label, centered as a group, label truncated to whatever is left.
        var content = rect.ContractedBy(padding, 0f);
        using (new TextBlock(font))
        {
            var contentWidth = 0f;
            var labelWidth = 0f;
            if (icon != null) contentWidth += iconSize;
            if (icon != null && label != null) contentWidth += iconGap;
            if (label != null)
            {
                labelWidth = Mathf.Max(0f, Mathf.Min(Text.CalcSize(label).x, content.width - contentWidth));
                contentWidth += labelWidth;
            }

            var cx = content.x + Mathf.Max(0f, (content.width - contentWidth) / 2f);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(cx, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize), icon);
                cx += iconSize + (label != null ? iconGap : 0f);
            }

            if (label != null)
                using (new TextBlock(null, TextAnchor.MiddleLeft, false))
                {
                    Verse.Widgets.Label(new Rect(cx, rect.y, labelWidth, rect.height), label.Truncate(labelWidth));
                }
        }

        if (disabled) Verse.Widgets.DrawBoxSolid(rect, Color.black with { a = 0.25f });

        return clicked && !disabled;
    }

    private enum Tab
    {
        Buttons,
        Text,
        Icons,
        Collapsible,
        List,
        Layout
    }
}
#endif