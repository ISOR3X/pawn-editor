using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Taffy;
using UnityEngine;
using Verse;

namespace Void.Components;

public static partial class TaffyExtensions
{
    private const float DefaultInputWidth = 120f;

    // Locked GUIStyle for InputNumber: all state backgrounds/colors match normal so that
    // spurious focus transfers (e.g. button clicks inside the field rect) produce no visual
    // change. Used when the field is not genuinely focused.
    private static (GUIStyle? style, GameFont font) _sLockedTextField;

    // Ghost GUIStyle: no background or border in any state. Rebuilt when the active font changes.
    private static (GUIStyle? style, GameFont font) _sGhostTextField;

    // Persistent per-widget state: keyed by "{contextKey}:{file}:{line}".
    // Stores (Source, Typed): Source is the caller's value last frame; Typed is what the user
    // has typed. The cache is only applied when Source matches the current caller value,
    // so external changes (e.g. Generate button) are never overwritten by stale cache.
    private static readonly Dictionary<string, (string Source, string Typed)> SInputState = new();

    // Persistent per-widget state for numeric inputs: keyed by "{contextKey}:{id}" or "{contextKey}:{file}:{line}".
    // Stores (buffer, value, externalValue): externalValue is the caller's value last frame.
    // The stored value is only applied when externalValue matches the current caller value,
    // so external changes (e.g. dragging a color rect) are never overwritten by stale input state.
    private static readonly Dictionary<string, (string buffer, int value, int externalValue)> SNumericState = [];

    private static readonly Dictionary<string, (float value, float externalValue)> SRangeState = [];

    public enum InputVariant
    {
        Solid = 0,
        Ghost = 1,
    }

    private static GUIStyle ResolveTextFieldStyle(InputVariant variant, bool focused, bool disabled)
    {
        if (variant == InputVariant.Ghost)
        {
            {
                var font = Verse.Text.Font;
                if (_sGhostTextField.style == null || _sGhostTextField.font != font)
                {
                    var s = new GUIStyle(Verse.Text.CurTextFieldStyle);
                    s.normal.background = null;
                    s.focused.background = null;
                    s.hover.background = null;
                    s.active.background = null;
                    s.border = new RectOffset(0, 0, 0, 0);
                    _sGhostTextField = (s, font);
                }

                return _sGhostTextField.style!;
            }
        }

        if (focused) return Verse.Text.CurTextFieldStyle;

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
    }

    // Clears keyboard focus. Can be called from any draw callback.
    private static void Unfocus() => GUIUtility.keyboardControl = 0;

    private static string MakeKey(TaffyBuilder b, string? id, string? file, int line)
        => id != null ? $"{b.ContextKey}:{id}" : $"{b.ContextKey}:{file}:{line}";

    private static (bool isFocused, bool effectivelyFocused) GetFocusState(string controlName, Rect r)
    {
        var isFocused = GUI.GetNameOfFocusedControl() == controlName;
        var clickedOutside = Event.current.type == EventType.MouseDown && !r.Contains(Event.current.mousePosition);
        return (isFocused, isFocused && !clickedOutside);
    }

    /// <summary>
    ///     Adds a text-input leaf. Works like <c>CharacterCardUtility.DoNameInputRect</c>:
    ///     call with a local copy of the value, then check for changes afterward.
    ///     <code>
    /// var first = triple.First;
    /// row.Input(ref first, maxLength: 12);
    /// if (first != triple.First) pawn.Name = new NameTriple(first, ...);
    /// </code>
    ///     The update reflects the value typed on the PREVIOUS frame (standard IMGUI pattern).
    ///     Requires <see cref="SectionWorker.BuildSection" /> to have set a <c>ContextKey</c> on
    ///     the builder so that state is isolated per pawn.
    /// </summary>
    public static void Input(this TaffyBuilder b, ref string text, int? maxLength = null,
        Regex? pattern = null, Color? color = null, Action<Rect>? onHover = null, Action<Rect>? draw = null,
        bool disabled = false,
        InputVariant variant = InputVariant.Solid, StyleOverride? style = null, string? id = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = MakeKey(b, id, file, line);
        var controlName = $"Input_{key}";

        if (SInputState.TryGetValue(key, out var stored) && stored.Source == text)
            text = stored.Typed;

        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = DefaultInputWidth,
            height = UIUtility.ButtonHeight
        });

        var displayValue = text;
        b.Item(r =>
        {
            draw?.Invoke(r);
            if (onHover != null && Mouse.IsOver(r)) onHover(r);

            var (isFocused, effectivelyFocused) = GetFocusState(controlName, r);

            string input = displayValue;
            GUI.SetNextControlName(controlName);
            using (new GUIColor(disabled ? new Color(1f, 1f, 1f, 0.5f) : color ?? Color.white))
            {
                var inputStyle = ResolveTextFieldStyle(variant, effectivelyFocused, disabled);
                if (disabled) GUI.Label(r, displayValue, inputStyle);
                else input = GUI.TextField(r, displayValue, inputStyle);
            }

            if (disabled) return;
            if (isFocused && !effectivelyFocused) Unfocus();

            if (maxLength.HasValue && input.Length > maxLength.Value) return;
            if (pattern != null && !pattern.IsMatch(input)) return;
            SInputState[key] = (displayValue, input);
        }, mergedStyle);
    }

    /// <summary>
    ///     Adds a numeric text-input leaf that only accepts digit characters while typing,
    ///     and applies min/max clamping only when the field loses focus.
    ///     Pass an explicit <paramref name="id" /> when the call site is a shared helper method
    ///     (multiple callers would otherwise share the same file:line key).
    /// </summary>
    public static void InputNumber(this TaffyBuilder b, ref int value, int min = 0, int max = 9999,
        Action<Rect>? draw = null, Action<Rect>? onHover = null, bool disabled = false,
        InputVariant variant = InputVariant.Solid,
        StyleOverride? style = null, string? id = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = MakeKey(b, id, file, line);
        var controlName = $"InputNumber_{key}";

        var incomingValue = value;
        var hasState = SNumericState.TryGetValue(key, out var stored);
        var restoreState = hasState && stored.externalValue == incomingValue;
        // Only restore the user's typed value if the external source hasn't changed since last
        // frame. If it changed (e.g. ColorRect dragged), use the new value and reset the buffer.
        if (restoreState) value = stored.value;

        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = Dimension.Length(DefaultInputWidth),
            height = UIUtility.ButtonHeight
        });

        var capturedValue = value;
        var capturedBuffer = restoreState ? stored.buffer : value.ToString();

        b.Item(r =>
        {
            draw?.Invoke(r);
            if (onHover != null && Mouse.IsOver(r)) onHover(r);

            var r2 = r.RightPartPixels(r.height / 2f);
            r2.x -= GenUI.GapTiny;
            var upRect = r2.TopHalf();
            var downRect = r2.BottomHalf();

            // Pre-compute focus/click state before any controls process events so the blur
            // path can fire on the same frame as the click-outside.
            var (isFocused, effectivelyFocused) = GetFocusState(controlName, r);

            var scrollVal = UIUtility.IncrementWithScroll(r, capturedValue, 5);
            var scrollFired = scrollVal != capturedValue;
            if (scrollFired) Commit(scrollVal);

            var buttonFired = false;
            if (Widgets.ButtonImageWithHold(upRect, TexUI.ArrowUp, key + ":up", disabled))
            {
                Commit(capturedValue + 1);
                buttonFired = true;
            }
            else if (Widgets.ButtonImageWithHold(downRect, TexUI.ArrowDown, key + ":down", disabled))
            {
                Commit(capturedValue - 1);
                buttonFired = true;
            }

            GUI.SetNextControlName(controlName);
            // When genuinely focused use the original style so its focused.background (white
            // border) renders naturally. When not focused use the locked style so spurious
            // focus transfers from button clicks produce no visual change.
            var raw = capturedBuffer;
            using (new GUIColor(disabled ? new Color(1f, 1f, 1f, 0.5f) : mergedStyle.color ?? Color.white))
            {
                var inputStyle = ResolveTextFieldStyle(variant, effectivelyFocused, disabled);
                if (disabled) GUI.Label(r, capturedBuffer, inputStyle);
                else raw = GUI.TextField(r, capturedBuffer, inputStyle);
            }

            // Skip state update when a button just committed, otherwise the old buffer
            // would overwrite the new value on the same frame.
            if (buttonFired || scrollFired || disabled) return;

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
        }, mergedStyle);
        return;

        void Commit(int newVal)
        {
            if (disabled) return;
            var clamped = Mathf.Clamp(newVal, min, max);
            SNumericState[key] = (clamped.ToString(), clamped, incomingValue);
        }

        static string FilterNumeric(string input, bool allowNegative)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (char.IsDigit(ch) || (allowNegative && ch == '-' && i == 0)) sb.Append(ch);
            }

            return sb.ToString();
        }
    }

    public static void InputRange(this TaffyBuilder b, ref float value, float min = 0f, float max = 9999f,
        float step = 1f,
        Action<Rect>? draw = null, Action<Rect>? onHover = null, bool disabled = false,
        InputVariant variant = InputVariant.Solid,
        StyleOverride? style = null, string? id = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = MakeKey(b, id, file, line);
        var incomingValue = value;
        var hasState = SRangeState.TryGetValue(key, out var stored);
        if (hasState && Mathf.Approximately(stored.externalValue, incomingValue))
            value = stored.value;

        var mergedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            width = DefaultInputWidth,
            // This height makes it fit in a button height (30f) container when stacked with GameFont.Tiny text.
            height = 12f
        });

        // Smaller step when shift is held.
        if (Event.current.shift)
        {
            step = Mathf.Max(step / 10, 0.01f);
        }

        var capturedValue = Mathf.Clamp(value, min, max);

        b.Item(r =>
        {
            draw?.Invoke(r);
            if (onHover != null && Mouse.IsOver(r)) onHover(r);

            var nextValue = Verse.Widgets.HorizontalSlider(r, capturedValue, min, max, true, roundTo: step);

            SRangeState[key] = (nextValue, incomingValue);
        }, mergedStyle);
    }
}