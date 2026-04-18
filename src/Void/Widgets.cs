using RimWorld;
using UnityEngine;
using Verse;
using Void.Extensions;
using static Verse.UnityGUIBugsFixer;

namespace Void;

[StaticConstructorOnStartup]
public static partial class Widgets
{
    
    private const float HoldInitialDelay = 0.4f;
    private const float HoldRepeatInterval = 0.07f;
    // Hold-repeat state: direction (+1/-1/0), time the hold started, time of the last repeat tick.
    private static readonly Dictionary<string, (int direction, float heldSince, float lastRepeat)> SHoldState = [];

    public static void SectionSeparator(Rect inRect, string label)
    {
        var rect = inRect.TakeTopPart(30f);
        using (new TextBlock(Text.Anchor = TextAnchor.UpperLeft))
        using (new GUIColor(Verse.Widgets.SeparatorLabelColor))
        {
            Verse.Widgets.Label(rect, label.CapitalizeFirst());
        }

        rect.yMin += 20f;
        using (new GUIColor(Verse.Widgets.SeparatorLineColor))
        {
            Verse.Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
        }
    }

    public static void WidgetLabel(Rect inRect, string label)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Verse.Widgets.Label(inRect, label.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        }
    }

    public static bool ButtonText_TruncateWithTooltip(Rect inRect, string label, float padding = 16f)
    {
        var width = inRect.width;
        if (Text.CalcSize(label).x > width - padding) TooltipHandler.TipRegion(inRect, label);

        return Verse.Widgets.ButtonText(inRect, label.Truncate(width - padding));
    }

    public static void IntField(Rect inRect, ref int value, int min, int max, ref string? buffer,
        bool minMaxButtons = false)
    {
        var intBuff = -1;
        if (buffer == null) intBuff = value;

        if (minMaxButtons)
            if (Verse.Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexUI.ArrowLeftDouble))
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

        if (Verse.Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexUI.ArrowLeft))
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
            if (Verse.Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexUI.ArrowRightDouble))
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

        if (Verse.Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexUI.ArrowRight))
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
        Verse.Widgets.TextFieldNumeric(fieldRect, ref intBuff, ref buffer);

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
        return DelayedTextField(inRect, text, ref buffer,
            (rect, buffer) => Verse.Widgets.TextField(rect, buffer, maxLength),
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
            if (Verse.Widgets.ButtonImage(inRect.TakeLeftPart(25).ContractedBy(0, 5), TexUI.ArrowLeft))
            {
                value--;
                buffer = null;
            }

            if (Verse.Widgets.ButtonImage(inRect.TakeRightPart(25).ContractedBy(0, 5), TexUI.ArrowRight))
            {
                value++;
                buffer = null;
            }
        }

        var output = DelayedTextField(inRect, value.ToString(), ref buffer, (rect, buff) =>
        {
            // float val = value;
            int.TryParse(buff, out var val);
            Verse.Widgets.TextFieldNumeric(rect, ref val, ref buff, min, max);
            return buff.ToString();
        }, previousFocusedControlName, controlName);

        int.TryParse(output, out var result);

        result = UIUtility.IncrementWithScroll(inRect, result, 5);

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

    /// <summary>
    /// Like <see cref="Verse.Widgets.ButtonImage"/> but also fires on hold-repeat.
    /// <paramref name="key"/> must be a globally unique stable string per button.
    /// </summary>
    public static bool ButtonImageWithHold(Rect rect, Texture2D tex, string key)
    {
        if (Verse.Widgets.ButtonImage(rect, tex))
        {
            SHoldState.Remove(key);
            return true;
        }

        SHoldState.TryGetValue(key, out var hold);
        var mouseHeld = UnityEngine.Input.GetMouseButton(0);
        if (mouseHeld && Mouse.IsOver(rect))
        {
            if (hold.direction == 0) hold = (1, Time.realtimeSinceStartup, Time.realtimeSinceStartup);
        }
        else hold = default;

        SHoldState[key] = hold;

        if (hold.direction != 0 &&
            Time.realtimeSinceStartup - hold.heldSince >= HoldInitialDelay &&
            Time.realtimeSinceStartup - hold.lastRepeat >= HoldRepeatInterval)
        {
            SHoldState[key] = hold with { lastRepeat = Time.realtimeSinceStartup };
            return true;
        }

        return false;
    }
}