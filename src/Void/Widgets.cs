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

    /// <summary>
    ///     Like <see cref="Verse.Widgets.ButtonImage" /> but also fires on hold-repeat.
    ///     <paramref name="key" /> must be a globally unique stable string per button.
    /// </summary>
    public static bool ButtonImageWithHold(Rect rect, Texture2D tex, string key, bool disabled = false)
    {
        var mouseOverColor = disabled ? Color.white : GenUI.MouseoverColor;
        if (Verse.Widgets.ButtonImage(rect, tex, Color.white, mouseOverColor))
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