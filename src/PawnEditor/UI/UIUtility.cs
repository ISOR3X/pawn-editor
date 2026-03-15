using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class UIUtility
{
    public const float ScrollBarWidth = 16f;
    public const float ScrollBarWidth_WithMargin = ScrollBarWidth + 4f;
    public const float ButtonHeight = 30f;
    public const float ButtonPadding = 40f;
    public const float LabelPadding = 10f;
    public const float LabelOffset = 24f; // How far a label should be from its widget
    public static readonly Vector2 BottomButtonSize = new(150f, 38f);
    
    public static void SplitHorizontallyEqual(this Rect rect, out Rect top, out Rect bottom, float padding = 0)
    {
        var half = rect.height / 2;
        top = rect.TopPartPixels(half - padding);
        bottom = rect.BottomPartPixels(half - padding);
    }

    public static string TruncateWithTooltip(this string label, Rect inRect, float padding = 16f)
    {
        var lineRect = inRect.TopPartPixels(Text.LineHeight);
        lineRect.y -= Text.LineHeight;
        if (Text.CalcSize(label).x > inRect.width - padding)
        {
            TooltipHandler.TipRegion(lineRect, label);
            return label.Truncate(inRect.width - padding);
        }

        return label;
    }

    public static Gradient GradientFromColorComponent(Verse.Widgets.ColorComponents component, Color color)
    {
        var gradient = new Gradient();

        if (component == Verse.Widgets.ColorComponents.Hue)
        {
            // Create color keys for the gradient
            var colorKeys = new GradientColorKey[7];
            for (var i = 0; i < colorKeys.Length; i++)
            {
                var value = i / 6f;
                if (i == 6) value -= 0.001f; // Prevent wraparound
                colorKeys[i] = new GradientColorKey(color.SetComponent(component, value), i / 6f);
            }

            // Create alpha keys for the gradient
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

            // Set the color and alpha keys
            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        var colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(color.SetComponent(component, 0), 0f);
        colors[0] = new GradientColorKey(color.SetComponent(component, 1), 1f);

        gradient.SetKeys(colors, [new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1)]);
        return gradient;
    }

    public static float GetComponent(this Color color, Verse.Widgets.ColorComponents component)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        switch (component)
        {
            case Verse.Widgets.ColorComponents.Red:
                return color.r;
            case Verse.Widgets.ColorComponents.Green:
                return color.g;
            case Verse.Widgets.ColorComponents.Blue:
                return color.b;
            case Verse.Widgets.ColorComponents.Hue:
                return h;
            case Verse.Widgets.ColorComponents.Sat:
                return s;
            case Verse.Widgets.ColorComponents.Value:
                return v;
            default:
                throw new ArgumentOutOfRangeException(nameof(component), component,
                    "Invalid color component, only RGB/HSV are supported.");
        }
    }

    public static Color SetComponent(this Color color, Verse.Widgets.ColorComponents component, float value)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        switch (component)
        {
            case Verse.Widgets.ColorComponents.Red:
                color.r = value;
                break;
            case Verse.Widgets.ColorComponents.Green:
                color.g = value;
                break;
            case Verse.Widgets.ColorComponents.Blue:
                color.b = value;
                break;
            case Verse.Widgets.ColorComponents.Hue:
                h = value;
                color = Color.HSVToRGB(h, s, v);
                break;
            case Verse.Widgets.ColorComponents.Sat:
                s = value;
                color = Color.HSVToRGB(h, s, v);
                break;
            case Verse.Widgets.ColorComponents.Value:
                v = value;
                color = Color.HSVToRGB(h, s, v);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(component), component,
                    "Invalid color component, only RGB/HSV are supported.");
        }

        return color;
    }

    public static void DefIconPreview(Rect inRect, Def? def, Color? color = null, float scale = 1.1f)
    {
        color ??= Color.white;

        if (!Mouse.IsOver(inRect)) return;

        var r = new Rect(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y, 100f, 100f + Text.LineHeight);
        Find.WindowStack.ImmediateWindow(12918217, r, WindowLayer.Super, () =>
        {
            var rect2 = r.AtZero();
            rect2.height -= Text.LineHeight;
            Verse.Widgets.DrawHighlight(rect2);
            if (def == null)
                return;
            Text.Anchor = TextAnchor.UpperCenter;
            Verse.Widgets.LabelFit(new Rect(0.0f, rect2.yMax, rect2.width, Text.LineHeight), def.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            using (new GUIColor(color.Value))
            {
                Verse.Widgets.DefIcon(rect2, def, scale: scale);
            }
        });
    }

    public static int IncrementWithScroll(Rect inRect, int value, int shiftIncrement = -1)
    {
        // Increment/ decrement value with mouse scroll. Uses a scrollview to prevent scrolling of other scrollviews due to mouse scroll.
        var v = Vector2.zero;
        Verse.Widgets.BeginScrollView(inRect, ref v, inRect);
        if (Mouse.IsOver(inRect))
        {
            var tooltip = "Scroll to change value";
            if (shiftIncrement != -1)
                tooltip += $", hold shift to increment by {shiftIncrement}";
            TooltipHandler.TipRegion(inRect, tooltip);

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 1) && !Utility.HasDoneOnce)
            {
                if (Event.current.shift && shiftIncrement != -1)
                    value += shiftIncrement;
                else
                    value++;
                Utility.HasDoneOnce = true;
            }
            else if (Mathf.Approximately(scroll, -1) && !Utility.HasDoneOnce)
            {
                if (Event.current.shift && shiftIncrement != -1)
                    value -= shiftIncrement;
                else
                    value--;
                Utility.HasDoneOnce = true;
            }
            else if (scroll == 0)
            {
                Utility.HasDoneOnce = false;
            }
        }

        Verse.Widgets.EndScrollView();

        return value;
    }


    public static void ColorPickerLabeled(this Listing_Standard listing, string label, float height, ref Color color,
        Dictionary<string, Color>? specialColors, List<Color> colors, Action<Color> onApply, out float newHeight,
        string? tooltip = null)
    {
        listing.Label(label);
        var rect = listing.GetRect(height);

        if (!tooltip.NullOrEmpty())
            TooltipHandler.TipRegion(rect, (TipSignal)tooltip);

        var availableColors = colors.Append(new Color(0, 0, 0, 0f)).ToList();

        if (rect.width <= 0)
        {
            newHeight = height;
            return;
        }

        var oldColor = color;
        Verse.Widgets.ColorSelector(rect, ref color, availableColors, out newHeight,
            extraOnGUI: (currentColor, r) =>
            {
                if (currentColor.a != 0) return;
                if (Verse.Widgets.ButtonImage(r.ExpandedBy(2f), Designator_Eyedropper.EyeDropperTex))
                    Find.WindowStack.Add(new Dialog_ColorPicker(onApply, oldColor, colors, specialColors));
            });
    }

    public static void DrawElementStackSection<T>(
        Rect inRect,
        List<T> elements, GenUI.StackElementDrawer<T> drawer, GenUI.StackElementWidthGetter<T> widthGetter,
        float rowHeight = 22f,
        string? emptyLabel = null,
        bool allowOrderOptimization = false)
    {
        GUI.DrawTexture(inRect, InspectPaneFiller.HealthTex);
        var innerRect = inRect.ContractedBy(4f);

        if (elements.NullOrEmpty())
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Verse.Widgets.Label(innerRect,
                    (emptyLabel ?? "None".Translate()).Colorize(ColoredText.SubtleGrayColor));
            }

            return;
        }

        GenUI.DrawElementStack(
            innerRect,
            rowHeight, elements, drawer, widthGetter,
            allowOrderOptimization: allowOrderOptimization);
    }

    public static float DrawElementStackSectionHeight<T>(
        List<T> elements, GenUI.StackElementWidthGetter<T> widthGetter,
        float width,
        float rowHeight = 22f)
    {
        if (elements.NullOrEmpty()) return rowHeight;
        var stackRect = GenUI.DrawElementStack(
            new Rect(0, 0, width, 99999f),
            rowHeight, elements, null, widthGetter);
        return
            stackRect.height + 8f; // 8f is the extra padding added by ContractedBy(4f) in DrawElementStackSection<T>.
    }

    public static Rect RectLabeled(Rect rect, string label, float? labelWidth = null)
    {
        var w = labelWidth ?? label.GetWidthCached() + LabelOffset;
        rect.SplitVertically(w, out var left, out var right);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Verse.Widgets.Label(left, label);
        }

        return right;
    }

    public static bool ButtonTextLabeled(Rect rect, string label, string buttonLabel)
    {
        var right = RectLabeled(rect, label);
        return Verse.Widgets.ButtonText(right, buttonLabel);
    }

    public static bool ButtonTextLabeled_WithIcon(Rect rect, string label, string buttonLabel, Texture2D icon,
        Color? color = null)
    {
        var right = RectLabeled(rect, label);

        return ButtonText_WithIcon(right, buttonLabel, icon, color);
    }

    public static bool ButtonText_WithIcon(Rect rect, string label, Texture2D icon, Color? color)
    {
        const float iconSize = 20f;
        const float gap = 4f;

        var l = label.Truncate(rect.width - (iconSize + gap + LabelPadding * 2));
        var width = iconSize + gap + l.GetWidthCached();
        var remaining = rect.width - width;

        var clicked = Verse.Widgets.ButtonInvisible(rect);
        Verse.Widgets.DrawButtonGraphic(rect);

        var contentRect = rect.ContractedBy(remaining / 2, 0f);

        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Text.WordWrap = false;
            Verse.Widgets.Label(contentRect.TakeLeftPart(l.GetWidthCached()), l);
            Text.WordWrap = true;
        }

        contentRect.xMin += gap;

        var iconRect = new Rect(contentRect.x, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);

        using (new GUIColor(color ?? Color.white))
        {
            GUI.DrawTexture(iconRect, icon);
        }

        return clicked;
    }
}