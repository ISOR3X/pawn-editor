using UnityEngine;
using Verse;

namespace Void;

[StaticConstructorOnStartup]
public static class Widgets
{
    private const float HoldInitialDelay = 0.4f;

    private const float HoldRepeatInterval = 0.07f;

    // Hold-repeat state: direction (+1/-1/0), time the hold started, time of the last repeat tick.
    private static readonly Dictionary<string, (int direction, float heldSince, float lastRepeat)> SHoldState = [];

    public static void WidgetLabel(Rect inRect, string label)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Verse.Widgets.Label(inRect, label.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        }
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
    ///     Like <see cref="Verse.Widgets.ButtonImage" /> but also fires on hold-repeat.
    ///     <paramref name="key" /> must be a globally unique stable string per button.
    /// </summary>
    public static bool ButtonImageWithHold(Rect rect, Texture2D tex, string key)
    {
        if (Verse.Widgets.ButtonImage(rect, tex))
        {
            SHoldState.Remove(key);
            return true;
        }

        SHoldState.TryGetValue(key, out var hold);
        var mouseHeld = Input.GetMouseButton(0);
        if (mouseHeld && Mouse.IsOver(rect))
        {
            if (hold.direction == 0) hold = (1, Time.realtimeSinceStartup, Time.realtimeSinceStartup);
        }
        else
        {
            hold = default;
        }

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