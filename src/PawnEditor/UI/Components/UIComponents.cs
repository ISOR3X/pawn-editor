using System;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.UnityGUIBugsFixer;

namespace PawnEditor;

[StaticConstructorOnStartup]
[Reloadable]
public static partial class UIComponents
{
    public static void SectionSeparator(Rect inRect, string label)
    {
        var rect = inRect.TakeTopPart(30f);
        var color = GUI.color;
        GUI.color = Widgets.SeparatorLabelColor;
        using (new TextBlock(Text.Anchor = TextAnchor.UpperLeft))
        {
            Widgets.Label(rect, label.CapitalizeFirst());
        }

        rect.yMin += 20f;
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
        GUI.color = color;
    }

    public static void WidgetLabel(Rect inRect, string label)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(inRect, label.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        }
    }

    public static bool ButtonText_TruncateWithTooltip(Rect inRect, string label)
    {
        const float padding = 16f;
        var width = inRect.width;
        if (Text.CalcSize(label).x > width - padding) TooltipHandler.TipRegion(inRect, label);

        return Widgets.ButtonText(inRect, label.Truncate(width - padding));
    }

    public static void IntField(Rect inRect, ref int value, int min, int max, ref string? buffer,
        bool minMaxButtons = false)
    {
        var intBuff = -1;
        if (buffer == null) intBuff = value;

        if (minMaxButtons)
            if (Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeftDouble))
            {
                if (value >= min + 1)
                {
                    value = min;
                    buffer = null;
                }
                else
                {
                    Messages.Message(new Message("Reached limit of input", MessageTypeDefOf.RejectInput));
                }

                return;
            }

        if (Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeft))
        {
            if (value >= min + 1)
            {
                value--;
                buffer = null;
            }
            else
            {
                Messages.Message(new Message("Reached limit of input", MessageTypeDefOf.RejectInput));
            }

            return;
        }

        if (minMaxButtons)
            if (Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightDouble))
            {
                if (value <= max - 1)
                {
                    value = max;
                    buffer = null;
                }
                else
                {
                    Messages.Message(new Message("Reached limit of input", MessageTypeDefOf.RejectInput));
                }

                return;
            }

        if (Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRight))
        {
            if (value <= max - 1)
            {
                value++;
                buffer = null;
            }
            else
            {
                Messages.Message(new Message("Reached limit of input", MessageTypeDefOf.RejectInput));
            }

            return;
        }

        var fieldRect = inRect.ContractedBy(0f, 4f);
        Widgets.TextFieldNumeric(fieldRect, ref intBuff, ref buffer);

        if (GUI.GetNameOfFocusedControl() != "TextField" + fieldRect.y.ToString("F0") + fieldRect.x.ToString("F0"))
        {
            value = Mathf.Clamp(intBuff, min, max);
            buffer = null;
        }
    }

    public static string DelayedTextField(
        Rect inRect,
        string text,
        ref string? buffer,
        int maxLength,
        string? previousFocusedControlName,
        string? controlName = null)
    {
        return DelayedTextField(inRect, text, ref buffer, (rect, buffer) => Widgets.TextField(rect, buffer, maxLength),
            previousFocusedControlName, controlName);
    }

    public static int DelayedTextFieldNumeric(
        Rect inRect,
        int value,
        ref string? buffer,
        float min,
        float max,
        string? previousFocusedControlName,
        bool incrementButtons = false,
        string? controlName = null)
    {
        // Increment/ decrement value with buttons.
        if (incrementButtons)
        {
            if (Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowLeft))
            {
                value--;
                buffer = null;
            }

            if (Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRight))
            {
                value++;
                buffer = null;
            }
        }

        var output = DelayedTextField(inRect, value.ToString(), ref buffer, (rect, buff) =>
        {
            // float val = value;
            int.TryParse(buff, out var val);
            Widgets.TextFieldNumeric(rect, ref val, ref buff, min, max);
            return buff.ToString();
        }, previousFocusedControlName, controlName);

        int.TryParse(output, out var result);

        result = UIUtility.IncrementWithScroll(inRect, result);

        return Mathf.Clamp(result, (int)min, (int)max);
    }


    // REF: Widgets.DelayedTextField, but modified so that it also works with a single text field.
    private static string DelayedTextField(Rect inRect,
        string text,
        ref string? buffer,
        Func<Rect, string?, string> inputDrawer,
        string? previousFocusedControlName,
        string? controlName = null)
    {
        // controlName ??= $"TextField{(object)inRect.x},{(object)inRect.y}";
        controlName ??= $"TextField{(object)inRect.y}{(object)inRect.x}";
        var isPreviousFocused = previousFocusedControlName == controlName;
        var isFocused = GUI.GetNameOfFocusedControl() == controlName;
        var name = controlName + "_unfocused";

        GUI.SetNextControlName(name);
        GUI.Label(inRect, "");
        GUI.SetNextControlName(controlName);

        var keyPressed = false;
        if (isFocused && Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return ||
                                                                     Event.current.keyCode == KeyCode.KeypadEnter))
        {
            Event.current.Use();
            keyPressed = true;
        }

        var clickedOutside = Event.current.type == EventType.MouseDown && !inRect.Contains(Event.current.mousePosition);
        isFocused = !keyPressed && !clickedOutside && isFocused;

        if (isPreviousFocused)
        {
            buffer = inputDrawer.Invoke(inRect, buffer);
            if (isFocused) return text;
            GUI.FocusControl(name);
            return buffer;
        }

        buffer = inputDrawer.Invoke(inRect, text);
        return buffer;
    }

    public static Color DelayedHexField(Rect inRect,
        Color currentColor,
        ref string? buffer,
        string? previousFocusedControlName,
        string? controlName = null)
    {
        const int maxLength = 7; // #RRGGBB
        var colorAsText = ColorUtility.ToHtmlStringRGB(currentColor);
        var output = DelayedTextField(inRect, colorAsText, ref buffer, maxLength - 1, previousFocusedControlName,
            controlName);
        if (output != colorAsText)
        {
            // Add the hash since this is needed for parsing.
            if (!output.StartsWith("#")) output = output.Insert(0, "#");

            // Add leading zeros if needed.
            var length = output.Length;
            if (length < maxLength)
                for (var i = 0; i < maxLength - length; i++)
                    output = output.Insert(1, "0");

            if (ColorUtility.TryParseHtmlString(output, out var color))
                return color;
        }

        return currentColor;
    }

    public static void DrawGradient(Rect inRect, Gradient gradient)
    {
        // TODO: Keep an eye on performance of this function.
        var texture = new Texture2D((int)inRect.width, (int)inRect.height, TextureFormat.RGBA32, false);

        for (var i = 0; i < texture.width; i++)
        {
            var t = i / (texture.width - 1f);
            for (var j = 0; j < texture.height; j++) texture.SetPixel(i, j, gradient.Evaluate(t));
        }

        texture.Apply();
        GUI.DrawTexture(inRect, texture);
    }

    public static void GradientSlider(Rect inRect, Widgets.ColorComponents colorComponent, ref Color color,
        ref string? lastFocusedSlider)
    {
        var originalRect = inRect;
        var hashCode = inRect.GetHashCode().ToString();
        inRect = inRect.ContractedBy(6f); // Contract to arrow is inside of the rect.
        var gradient = UIUtility.GradientFromColorComponent(colorComponent, color);
        var range = inRect.xMax - inRect.xMin;
        var xPosition = inRect.xMin + range * color.GetComponent(colorComponent);

        if (Event.current.button == 0 && Input.GetKey(KeyCode.Mouse0))
        {
            if (Widgets.ClickedInsideRect(originalRect) || (MouseDrag() && lastFocusedSlider == hashCode))
            {
                lastFocusedSlider = hashCode;
                if (Event.current.type == EventType.MouseDrag)
                    Event.current.Use();
                var mousePosition = Mathf.Clamp(Event.current.mousePosition.x, inRect.xMin, inRect.xMax);
                var fraction = (mousePosition - inRect.xMin) / range;
                var value = Mathf.Lerp(0, 1, fraction);
                color = color.SetComponent(colorComponent, value);
                xPosition = mousePosition;
            }
        }
        else
        {
            lastFocusedSlider = null;
        }

        DrawGradient(inRect, gradient);

        var position = new Rect(xPosition - 6f, inRect.yMax - 6f, 12f, 12f);
        GUI.DrawTextureWithTexCoords(position, Widgets.SelectionArrow, new Rect(0f, 0, 1f, 1f), true);
    }

    public static void GradientSlider_LabeledWithField(Rect inRect, Widgets.ColorComponents colorComponent,
        ref Color color, ref string? buffer, ref string? lastFocusedSlider,
        string? previousFocusedControlName)
    {
        var nameWidth = "X".GetWidthCached() + UIUtility.LabelPadding;
        var valueWidth = "XXX".GetWidthCached() + UIUtility.LabelPadding;
        Widgets.Label(inRect.TakeLeftPart(nameWidth), colorComponent.ToString()[0].ToString());
        var value = Mathf.RoundToInt(color.GetComponent(colorComponent) * 255);
        var output = DelayedTextFieldNumeric(inRect.TakeRightPart(valueWidth), value, ref buffer, 0, 255,
            previousFocusedControlName);

        if (!Mathf.Approximately(output, value))
        {
            var test = (float)output / 255;
            color = color.SetComponent(colorComponent, test);
        }

        GradientSlider(inRect, colorComponent, ref color, ref lastFocusedSlider);
    }
}