using System.Drawing;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;

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

    public enum ComponentSize
    {
        Small = -1,
        Default = 0,
        Large = 1
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

    public static int IncrementWithScroll(Rect inRect, int value, int? shiftIncrement = null)
    {
        if (!Mouse.IsOver(inRect)) return value;

        if (Event.current.type == EventType.ScrollWheel)
        {
            var delta = Event.current.delta.y != 0
                ? Event.current.delta.y
                : Event.current.delta.x; // Windows detects shift + scroll as horizontal scroll.
            if (delta < 0)
                value += Event.current.shift && shiftIncrement != null ? shiftIncrement.Value : 1;
            else if (delta > 0)
                value -= Event.current.shift && shiftIncrement != null ? shiftIncrement.Value : 1;
            Event.current.Use();
        }

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
                {
                    // Find.WindowStack.Add(new Dialog_ColorPicker(onApply, oldColor, colors, specialColors));
                }
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

    public static bool ButtonTextLabeled(Rect rect, string label, string buttonLabel, float? labelWidth = null)
    {
        var right = RectLabeled(rect, label, labelWidth);
        return Verse.Widgets.ButtonText(right, buttonLabel);
    }

    public static bool ButtonTextLabeled_WithIcon(Rect rect, string label, string buttonLabel, Texture2D icon,
        Color? color = null)
    {
        var right = RectLabeled(rect, label);

        return ButtonText_WithIcon(right, buttonLabel, icon, color);
    }

    public static bool ButtonText_WithIcon(Rect rect, string label, Texture2D icon, Color? color = null)
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

    public static void Rotated(this Texture2D texture, Rect rect, float angleDegrees)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angleDegrees, rect.center);
        GUI.DrawTexture(rect, texture);
        GUI.matrix = old;
    }
}