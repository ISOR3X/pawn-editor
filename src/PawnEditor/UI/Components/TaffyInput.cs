using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    private const float DefaultInputWidth = 120f;

    // Locked GUIStyle for InputNumber: all state backgrounds/colors match normal so that
    // spurious focus transfers (e.g. button clicks inside the field rect) produce no visual
    // change. Used when the field is not genuinely focused.
    private static (GUIStyle? style, GameFont font) _sLockedTextField;

    private static GUIStyle GetLockedTextFieldStyle()
    {
        var font = Verse.Text.Font;
        if (_sLockedTextField.style == null || _sLockedTextField.font != font)
        {
            var s = new GUIStyle(Verse.Text.CurTextFieldStyle);
            s.focused.background = s.normal.background;
            s.focused.textColor = s.normal.textColor;
            s.hover.background = s.normal.background;
            s.hover.textColor = s.normal.textColor;
            s.active.background = s.normal.background;
            s.active.textColor = s.normal.textColor;
            _sLockedTextField = (s, font);
        }

        return _sLockedTextField.style!;
    }

    // Clears keyboard focus. Can be called from any draw callback.
    private static void Unfocus() => GUIUtility.keyboardControl = 0;

    // Persistent per-widget state: keyed by "{contextKey}:{file}:{line}".
    // Stores (Source, Typed): Source is the caller's value last frame; Typed is what the user
    // has typed. The cache is only applied when Source matches the current caller value,
    // so external changes (e.g. Generate button) are never overwritten by stale cache.
    private static readonly Dictionary<string, (string Source, string Typed)> SInputState = new();

    /// <summary>
    /// Adds a text-input leaf. Works like <c>CharacterCardUtility.DoNameInputRect</c>:
    /// call with a local copy of the value, then check for changes afterward.
    /// <code>
    /// var first = triple.First;
    /// row.Input(ref first, maxLength: 12);
    /// if (first != triple.First) pawn.Name = new NameTriple(first, ...);
    /// </code>
    /// The update reflects the value typed on the PREVIOUS frame (standard IMGUI pattern).
    /// Requires <see cref="SectionWorker.BuildSection"/> to have set a <c>ContextKey</c> on
    /// the builder so that state is isolated per pawn.
    /// </summary>
    public static void Input(this TaffyBuilder b, ref string text, int? maxLength = null,
        Regex? pattern = null, Color? color = null, Action<Rect>? onHover = null, StyleOverride? style = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";
        var controlName = $"Input_{key}";

        if (SInputState.TryGetValue(key, out var stored) && stored.Source == text)
            text = stored.Typed;

        var resolvedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = DefaultInputWidth,
            height = UIUtility.ButtonHeight
        }).Resolve();

        var displayValue = text;
        b.AddLeaf(resolvedStyle, r =>
        {
            if (onHover != null && Mouse.IsOver(r)) onHover(r);

            var isFocused = GUI.GetNameOfFocusedControl() == controlName;
            var clickedOutside = Event.current.type == EventType.MouseDown && !r.Contains(Event.current.mousePosition);

            string input;
            using (new GUIColor(color ?? Color.white))
            {
                GUI.SetNextControlName(controlName);
                input = Verse.Widgets.TextField(r, displayValue);
            }

            if (isFocused && clickedOutside) Unfocus();

            if (maxLength.HasValue && input.Length > maxLength.Value) return;
            if (pattern != null && !pattern.IsMatch(input)) return;
            SInputState[key] = (displayValue, input);
        });
    }

    // Persistent per-widget state for numeric inputs: keyed by "{contextKey}:{id}" or "{contextKey}:{file}:{line}".
    // Stores (buffer, value, externalValue): externalValue is the caller's value last frame.
    // The stored value is only applied when externalValue matches the current caller value,
    // so external changes (e.g. dragging a color rect) are never overwritten by stale input state.
    private static readonly Dictionary<string, (string buffer, int value, int externalValue)> SNumericState = [];

    /// <summary>
    /// Adds a numeric text-input leaf that only accepts digit characters while typing,
    /// and applies min/max clamping only when the field loses focus.
    /// Pass an explicit <paramref name="id"/> when the call site is a shared helper method
    /// (multiple callers would otherwise share the same file:line key).
    /// </summary>
    public static void InputNumber(this TaffyBuilder b, ref int value, int min = 0, int max = 9999,
        StyleOverride? style = null, string? id = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = id != null ? $"{b.ContextKey}:{id}" : $"{b.ContextKey}:{file}:{line}";
        var controlName = $"InputNumber_{key}";

        var incomingValue = value;
        var hasState = SNumericState.TryGetValue(key, out var stored);
        var restoreState = hasState && stored.externalValue == incomingValue;
        // Only restore the user's typed value if the external source hasn't changed since last
        // frame. If it changed (e.g. ColorRect dragged), use the new value and reset the buffer.
        if (restoreState) value = stored.value;

        var resolvedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = Dimension.Length(DefaultInputWidth),
            height = UIUtility.ButtonHeight
        }).Resolve();

        var capturedValue = value;
        var capturedBuffer = restoreState ? stored.buffer : value.ToString();

        b.AddLeaf(resolvedStyle, r =>
        {
            var r2 = r.RightPartPixels(r.height / 2f);
            r2.x -= GenUI.GapTiny;
            var upRect = r2.TopHalf();
            var downRect = r2.BottomHalf();

            // Pre-compute focus/click state before any controls process events so the blur
            // path can fire on the same frame as the click-outside.
            var isFocused = GUI.GetNameOfFocusedControl() == controlName;
            var clickedOutside = Event.current.type == EventType.MouseDown && !r.Contains(Event.current.mousePosition);
            var effectivelyFocused = isFocused && !clickedOutside;

            var scrollVal = UIUtility.IncrementWithScroll(r, capturedValue, 5);
            var scrollFired = scrollVal != capturedValue;
            if (scrollFired) Commit(scrollVal);

            var buttonFired = false;
            if (Widgets.ButtonImageWithHold(upRect, TexPawnEditor.Up, key + ":up"))
            {
                Commit(capturedValue + 1);
                buttonFired = true;
            }
            else if (Widgets.ButtonImageWithHold(downRect, TexPawnEditor.Down, key + ":down"))
            {
                Commit(capturedValue - 1);
                buttonFired = true;
            }

            GUI.SetNextControlName(controlName);
            // When genuinely focused use the original style so its focused.background (white
            // border) renders naturally. When not focused use the locked style so spurious
            // focus transfers from button clicks produce no visual change.
            var fieldStyle = effectivelyFocused ? Verse.Text.CurTextFieldStyle : GetLockedTextFieldStyle();
            var raw = GUI.TextField(r, capturedBuffer, fieldStyle);

            // Skip state update when a button just committed, otherwise the old buffer
            // would overwrite the new value on the same frame.
            if (buttonFired || scrollFired) return;

            if (isFocused && !effectivelyFocused) Unfocus();

            var buf = FilterNumeric(raw, min < 0);
            if (effectivelyFocused)
            {
                // While focused: preserve the raw buffer so the user can type freely.
                // Keep the last committed value for callers reading this frame.
                SNumericState[key] = (buf, capturedValue, incomingValue);
            }
            else
            {
                // On blur: parse and clamp, or revert to the last committed value if empty/invalid.
                var v = int.TryParse(buf, out var p) ? Mathf.Clamp(p, min, max) : capturedValue;
                SNumericState[key] = (v.ToString(), v, incomingValue);
            }
        });
        return;

        void Commit(int newVal)
        {
            var clamped = Mathf.Clamp(newVal, min, max);
            SNumericState[key] = (clamped.ToString(), clamped, incomingValue);
        }

        static string FilterNumeric(string input, bool allowNegative)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new System.Text.StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (char.IsDigit(ch) || allowNegative && ch == '-' && i == 0) sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}
