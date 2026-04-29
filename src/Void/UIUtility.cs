
using RimWorld;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;

namespace Void;

public static class UIUtility
{
    public enum ComponentSize
    {
        Small = -1,
        Default = 0,
        Large = 1
    }

    public const float ScrollBarWidth = 16f;
    public const float ButtonHeight = 30f;

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

    public static void DefIconPreview(Rect inRect, Def def, Color? color = null, float scale = 1.1f)
    {
        color ??= Color.white;

        if (!Mouse.IsOver(inRect)) return;

        var r = new Rect(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y, 100f,
            100f + Text.LineHeight);
        Find.WindowStack.ImmediateWindow(12918217, r, WindowLayer.Super, () =>
        {
            var rect2 = r.AtZero();
            rect2.height -= Text.LineHeight;
            Verse.Widgets.DrawHighlight(rect2);
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
}