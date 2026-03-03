using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class UIUtility
{
    public const float scrollBarWidth = 16f;
    public const float scrollBarWidth_WithMargin = scrollBarWidth + 4f;
    public static readonly Vector2 BottomButtonSize = new(150f, 38f);
    public const float ButtonHeight = 30f;
    public const float ButtonPadding = 40f;
    public const float LabelPadding = 10f;

    public static Rect TakeTopPart(ref this Rect rect, float pixels)
    {
        var ret = rect.TopPartPixels(pixels);
        rect.yMin += pixels;
        return ret;
    }

    public static Rect TakeBottomPart(ref this Rect rect, float pixels)
    {
        var ret = rect.BottomPartPixels(pixels);
        rect.yMax -= pixels;
        return ret;
    }

    public static Rect TakeRightPart(ref this Rect rect, float pixels)
    {
        var ret = rect.RightPartPixels(pixels);
        rect.xMax -= pixels;
        return ret;
    }

    public static Rect TakeLeftPart(ref this Rect rect, float pixels)
    {
        var ret = rect.LeftPartPixels(pixels);
        rect.xMin += pixels;
        return ret;
    }

    public static Rect CenteredVertically(this Rect rect, float height)
    {
        var remove = (rect.height - height) / 2;
        rect.yMax -= remove;
        rect.yMin += remove;
        return rect;
    }

    public static void Indent(ref this Rect rect, float width = 4f)
    {
        rect.xMin += width;
    }

    public static void Gap(ref this Rect rect, float width = 4f)
    {
        rect.yMin += width;
    }

    public static void SplitHorizontallyEqual(this Rect rect, out Rect top, out Rect bottom, float padding = 0)
    {
        var half = rect.height / 2;
        top = rect.TopPartPixels(half - padding);
        bottom = rect.BottomPartPixels(half - padding);
    }

    public static string TruncateWithTooltip(this string label, Rect inRect)
    {
        const float padding = 16f;
        var lineRect = inRect.TopPartPixels(Text.LineHeight);
        lineRect.y -= Text.LineHeight;
        if (Text.CalcSize(label).x > inRect.width - padding)
        {
            TooltipHandler.TipRegion(lineRect, label);
            return label.Truncate(inRect.width - padding);
        }

        return label;
    }

    public static Gradient GradientFromColorComponent(Widgets.ColorComponents component, Color color)
    {
        var gradient = new Gradient();

        if (component == Widgets.ColorComponents.Hue)
        {
            // Create color keys for the gradient
            GradientColorKey[] colorKeys = new GradientColorKey[7];
            for (int i = 0; i < colorKeys.Length; i++)
            {
                var value = i / 6f;
                if (i == 6) value -= 0.001f; // Prevent wraparound
                colorKeys[i] = new GradientColorKey(color.SetComponent(component, value), i / 6f);
            }

            // Create alpha keys for the gradient
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

            // Set the color and alpha keys
            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        var colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(color.SetComponent(component, 0), 0f);
        colors[0] = new GradientColorKey(color.SetComponent(component, 1), 1f);

        gradient.SetKeys(colors, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return gradient;
    }

    public static float GetComponent(this Color color, Widgets.ColorComponents component)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        switch (component)
        {
            case Widgets.ColorComponents.Red:
                return color.r;
            case Widgets.ColorComponents.Green:
                return color.g;
            case Widgets.ColorComponents.Blue:
                return color.b;
            case Widgets.ColorComponents.Hue:
                return h;
            case Widgets.ColorComponents.Sat:
                return s;
            case Widgets.ColorComponents.Value:
                return v;
            default:
                throw new ArgumentOutOfRangeException(nameof(component), component, "Invalid color component, only RGB/HSV are supported.");
        }
    }

    public static Color SetComponent(this Color color, Widgets.ColorComponents component, float value)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        switch (component)
        {
            case Widgets.ColorComponents.Red:
                color.r = value;
                break;
            case Widgets.ColorComponents.Green:
                color.g = value;
                break;
            case Widgets.ColorComponents.Blue:
                color.b = value;
                break;
            case Widgets.ColorComponents.Hue:
                h = value;
                color = Color.HSVToRGB(h, s, v);
                break;
            case Widgets.ColorComponents.Sat:
                s = value;
                color = Color.HSVToRGB(h, s, v);
                break;
            case Widgets.ColorComponents.Value:
                v = value;
                color = Color.HSVToRGB(h, s, v);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(component), component, "Invalid color component, only RGB/HSV are supported.");
        }

        return color;
    }

    public static void DefIconPreview(Rect inRect, Def def, Color? color = null, float scale = 1.1f)
    {
        color ??= Color.white;

        if (!Mouse.IsOver(inRect)) return;
        
        Rect r = new Rect(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y, 100f, 100f + Text.LineHeight);
        Find.WindowStack.ImmediateWindow(12918217, r, WindowLayer.Super, () =>
        {
            Rect rect2 = r.AtZero();
            rect2.height -= Text.LineHeight;
            Widgets.DrawHighlight(rect2);
            if (def == null)
                return;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.LabelFit(new Rect(0.0f, rect2.yMax, rect2.width, Text.LineHeight), def.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            using (new GUIColor(color.Value))
                Widgets.DefIcon(rect2, def, scale: scale);
        });
    }

    public static int IncrementWithScroll(Rect inRect, int value, int shiftIncrement = -1)
    {
        // Increment/ decrement value with mouse scroll. Uses a scrollview to prevent scrolling of other scrollviews due to mouse scroll.
        Vector2 v = Vector2.zero;
        Widgets.BeginScrollView(inRect,ref v, inRect);
        if (Mouse.IsOver(inRect))
        {
            string tooltip = $"Scroll to change value";
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
        Widgets.EndScrollView();

        return value;
    }
}