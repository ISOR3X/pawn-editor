using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

// ReSharper disable PossibleLossOfFraction

namespace PawnEditor;

[HotSwappable]
public static class UIUtility
{
    public const float SearchBarHeight = 30f;
    public const float RegularButtonHeight = 30f;
    public static readonly Vector2 BottomButtonSize = new(150f, 38f);

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

    public static void CheckboxLabeledCentered(Rect rect, string label, ref bool checkOn, bool disabled = false, float size = 24, Texture2D texChecked = null,
        Texture2D texUnchecked = null)
    {
        if (!disabled && Widgets.ButtonInvisible(rect))
        {
            checkOn = !checkOn;
            if (checkOn)
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            else
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
        }

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(rect.TakeLeftPart(Text.CalcSize(label).x), label);

        Widgets.CheckboxDraw(rect.x + rect.width / 2 - 12f, rect.y + rect.height / 2 - 12f, checkOn, disabled, size, texChecked, texUnchecked);
    }

    public static Rect[] Split1D(this Rect rect, int count, bool vertical, float padding = 0)
    {
        var result = new Rect[count];
        var size = ((vertical ? rect.height : rect.width) - padding * (count - 1)) / count;
        for (var i = 0; i < count; i++)
            result[i] = vertical
                ? new(rect.x, rect.y + (size + padding) * i, rect.width, size)
                : new Rect(rect.x + (size + padding) * i, rect.y, size, rect.height);

        return result;
    }

    public static float ColumnWidth(float padding, params string[] labels)
    {
        return labels.Max(str => Text.CalcSize(str).x) + padding;
    }

    public static void ListSeparator(this Listing listing, string label)
    {
        ListSeparator(new Rect(listing.curX, listing.curY, listing.ColumnWidth, 30f), label);
        listing.Gap(30f);
    }

    public static void ListSeparator(Rect inRect, string label)
    {
        var rect = inRect.TakeTopPart(30f);
        var color = GUI.color;
        GUI.color = Widgets.SeparatorLabelColor;
        using (new TextBlock(Text.Anchor = TextAnchor.UpperLeft))
            Widgets.Label(rect, label);
        rect.yMin += 20f;
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
        GUI.color = color;
    }

    public static bool ButtonTextImage(Rect inRect, [CanBeNull] Def def)
    {
        // Dictionary<string, TaggedString> truncateCache = new();
        var flag = Widgets.ButtonText(inRect, "");

        if (def != null)
        {
            if (Mouse.IsOver(inRect)) TooltipHandler.TipRegion(inRect, def.LabelCap);

            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                const float iconSize = 24f;
                var labelSize = Text.CalcSize(def.LabelCap).x;
                var middleRect = inRect;
                middleRect.width = labelSize + (iconSize + 4f);
                middleRect.x += (inRect.width - middleRect.width) / 2;
                var labelRect = middleRect.TakeLeftPart(labelSize);
                var iconRect = middleRect.TakeRightPart(iconSize);
                iconRect.height = iconSize;
                iconRect.yMin += 4f;
                Widgets.Label(labelRect, def.LabelCap);
                // Widgets.Label(labelRect, thingDef.LabelCap.Truncate(labelRect.width, truncateCache));

                if (def is StyleCategoryDef style)
                {
                    Widgets.DrawTextureFitted(iconRect, style.Icon, 1f);
                    return flag;
                }

                Widgets.DefIcon(iconRect, def);
                return flag;
            }
        }

        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(inRect, "None");

        return flag;
    }

    public static bool ButtonTextImage(Listing_Standard listing, [CanBeNull] Def def)
    {
        var rect = listing.GetRect(30f);
        var flag = false;
        if (!listing.BoundingRectCached.HasValue || rect.Overlaps(listing.BoundingRectCached.Value)) flag = ButtonTextImage(rect.TakeTopPart(30f), def);

        listing.Gap(listing.verticalSpacing);
        return flag;
    }

    public static void IntField(Rect inRect, ref int value, int min, int max, ref string buffer, bool minMaxButtons = false)
    {
        if (minMaxButtons)
        {
            if (Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeftHalfDouble))
            {
                if (value >= min + 1)
                {
                    value = min;
                    buffer = null;
                }
                else
                    Messages.Message(new("Reached limit of input", MessageTypeDefOf.RejectInput));
            }
        }

        if (Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeftHalf))
        {
            if (value >= min + 1)
            {
                value--;
                buffer = null;
            }
            else
                Messages.Message(new("Reached limit of input", MessageTypeDefOf.RejectInput));
        }

        if (minMaxButtons)
        {
            if (Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightHalfDouble))
            {
                if (value <= max - 1)
                {
                    value = max;
                    buffer = null;
                }
                else
                    Messages.Message(new("Reached limit of input", MessageTypeDefOf.RejectInput));
            }
        }

        if (Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightHalf))
        {
            if (value <= max - 1)
            {
                value++;
                buffer = null;
            }
            else
                Messages.Message(new("Reached limit of input", MessageTypeDefOf.RejectInput));
        }


        Widgets.TextFieldNumeric(inRect.ContractedBy(0f, 4f), ref value, ref buffer, min, max);
    }

    public static Rect CellRect(int cell, Rect inRect)
    {
        var cellWidth = inRect.width / 2f - 16f;
        var cellHeight = 30f;
        var x = cell % 2 == 0 ? 0f : cellWidth + 32f;
        var y = cell / 2 * (cellHeight + 8f);
        return new(inRect.x + x, inRect.y + y, cellWidth, cellHeight);
    }

    public static Color FadedColor(this Color color, float alpha) => new(color.r, color.g, color.b, alpha);

    public static void LabelWithIcon(Rect rect, string label, Texture labelIcon, float labelIconScale = 1f)
    {
        var outerRect = new Rect(rect.x, rect.y, labelIcon.width, rect.height);
        rect.xMin += labelIcon.width;
        Widgets.DrawTextureFitted(outerRect, labelIcon, labelIconScale);
        Widgets.Label(rect, label);
    }

    public static bool DefaultButtonText(ref Rect inRect, string label, float xMargin = 48f)
    {
        float width = Text.CalcSize(label).x;
        var rect = inRect.TakeLeftPart(width + xMargin);
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleCenter, false))
        {
            return Widgets.ButtonText(rect, label);
        }
    }
}