using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;
using TexUI = Void.TexUI;
using Widgets = Void.Widgets;

public static partial class VoidComponents
{
    public enum InputVariant
    {
        Solid = 0,
        Ghost = 1
    }

    private const float DefaultInputWidth = 128f;

    /// <summary>
    ///     Locked GUIStyle for InputNumber: all state backgrounds/colors match normal so that
    ///     spurious focus transfers (e.g. button clicks inside the field rect) produce no visual
    ///     change. Used when the field is not genuinely focused.
    /// </summary>
    private static readonly Dictionary<GameFont, GUIStyle> LockedTextFields = [];

    /// <summary>
    ///     Ghost GUIStyle: no background or border in any state.
    /// </summary>
    private static readonly Dictionary<GameFont, GUIStyle> GhostTextFields = [];


    private static readonly StyleCache<ComponentSize> InputStyles = new(size => new Style
    {
        width = Dimension.Px(DefaultInputWidth),
        height = Dimension.Px(size == ComponentSize.Small ? 20f : UIUtility.ButtonHeight),
        flexShrink = 0f,
        fontSize = InputMetrics(size)
    });

    /// <summary>Clears keyboard focus. Can be called from any draw callback.</summary>
    private static void Unfocus()
    {
        GUIUtility.keyboardControl = 0;
    }

    private static (bool isFocused, bool effectivelyFocused) GetFocusState(string controlName, Rect r)
    {
        var isFocused = GUI.GetNameOfFocusedControl() == controlName;
        var clickedOutside = Event.current.type == EventType.MouseDown && !r.Contains(Event.current.mousePosition);
        return (isFocused, isFocused && !clickedOutside);
    }

    private static string FilterNumeric(string input, bool allowNegative)
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

    private static GUIStyle ResolveTextFieldStyle(InputVariant variant, bool focused)
    {
        var font = Verse.Text.Font;

        if (variant == InputVariant.Ghost)
        {
            if (GhostTextFields.TryGetValue(font, out var ghost)) return ghost;
            ghost = new GUIStyle(Verse.Text.CurTextFieldStyle);
            ghost.normal.background = null;
            ghost.focused.background = null;
            ghost.hover.background = null;
            ghost.active.background = null;
            ghost.border = new RectOffset(0, 0, 0, 0);
            return GhostTextFields[font] = ghost;
        }

        if (focused) return Verse.Text.CurTextFieldStyle;

        if (LockedTextFields.TryGetValue(font, out var locked)) return locked;
        locked = new GUIStyle(Verse.Text.CurTextFieldStyle);
        locked.focused = new GUIStyleState
            { background = locked.normal.background, textColor = locked.normal.textColor };
        locked.hover = new GUIStyleState { background = locked.normal.background, textColor = locked.normal.textColor };
        locked.active = new GUIStyleState
            { background = locked.normal.background, textColor = locked.normal.textColor };
        return LockedTextFields[font] = locked;
    }

    private static GameFont InputMetrics(ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => GameFont.Tiny,
            ComponentSize.Default => GameFont.Small,
            ComponentSize.Large => GameFont.Medium,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>
    ///     Text buffer for numeric inputs.This allows us to type "5000" for it to be later be clamped to 50 on blur.
    /// </summary>
    private sealed class InputNumberState
    {
        public string Buffer = "";
    }

    extension(UIBranch branch)
    {
        /// <summary>
        ///     Adds a text-input leaf. The value passed in is the source of truth every frame;
        ///     <paramref name="onChange" /> fires from the draw callback with the newly typed value,
        ///     so there is no internal buffer that can drift from it.
        ///     <code>
        /// row.Input(subject.Name, v => subject.Name = v, maxLength: 24);
        /// </code>
        ///     Rejecting an edit is an <c>if</c> in <paramref name="onChange" />: the field keeps its
        ///     previous value, because that is what gets rendered next frame.
        ///     Keyboard focus is keyed off the resolved component key, so two inputs only keep
        ///     independent focus when their call sites differ or they are given explicit ids.
        /// </summary>
        public TaffyNode Input(string value, Action<string> onChange, int? maxLength = null,
            Regex? pattern = null, Action<Rect>? onHover = null, Action<Rect>? draw = null,
            bool disabled = false, ComponentSize size = ComponentSize.Default,
            InputVariant variant = InputVariant.Solid,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var controlName = $"VoidInput_{key}";

            var baseStyle = InputStyles.Get(size);
            var mergedStyle = style == null ? baseStyle : style.Merge(baseStyle);
            var color = mergedStyle.color ?? Color.white;

            return branch.Div(draw: r =>
            {
                draw?.Invoke(r);
                if (onHover != null && Mouse.IsOver(r)) onHover(r);

                // Resolved before the field draws so the locked style applies on the click frame.
                var (isFocused, effectivelyFocused) = GetFocusState(controlName, r);

                var next = value;
                using (new TextBlock(InputMetrics(size)))
                using (new GUIColor(disabled ? color with { a = color.a * 0.5f } : color))
                {
                    var inputStyle = ResolveTextFieldStyle(variant, effectivelyFocused);
                    if (disabled)
                    {
                        GUI.Label(r, value, inputStyle);
                    }
                    else
                    {
                        GUI.SetNextControlName(controlName);
                        next = GUI.TextField(r, value, inputStyle);
                    }
                }

                if (disabled) return;
                if (isFocused && !effectivelyFocused) Unfocus();

                if (next == value) return;
                if (maxLength.HasValue && next.Length > maxLength.Value) return;
                if (pattern != null && !pattern.IsMatch(next)) return;
                onChange(next);
            }, style: mergedStyle, id: key);
        }

        /// <summary>
        ///     Adds a numeric input with incrementing buttons and scroll-to-increment.
        ///     Only numbers are accepted while typing and <paramref name="min" />/<paramref name="max" /> clamping is applied on
        ///     blur, so an intermediate value can be typed through.
        ///     <paramref name="onChange" /> fires only when the committed value actually changes.
        /// </summary>
        public TaffyNode InputNumber(int value, Action<int> onChange, int min = 0, int max = 9999,
            Action<Rect>? onHover = null, Action<Rect>? draw = null,
            bool disabled = false, ComponentSize size = ComponentSize.Default,
            InputVariant variant = InputVariant.Solid,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var controlName = $"VoidInputNumber_{key}";
            var state = branch.UpsertState(key, () => new InputNumberState());

            // If the value is not equal to the buffer, that means it has changed externally.
            // Reset the buffer in this case.
            if (value.ToString() != state.Buffer) state.Buffer = value.ToString();

            // // A write from anywhere but this widget (a slider dragged, a Generate button) has to win
            // // over the buffer, so reset whenever the incoming value moved on its own.
            // if (!state.Initialized || state.External != value)
            // {
            //     state.Initialized = true;
            //     state.External = value;
            //     state.Value = value;
            //     state.Buffer = value.ToString();
            // }

            var baseStyle = InputStyles.Get(size);
            var mergedStyle = style == null ? baseStyle : style.Merge(baseStyle);
            var color = mergedStyle.color ?? Color.white;

            var node = branch.Div(draw: r =>
            {
                draw?.Invoke(r);
                if (onHover != null && Mouse.IsOver(r)) onHover(r);

                var spinner = r.RightPartPixels(r.height / 2f);
                spinner.x -= GenUI.GapTiny;

                var (isFocused, effectivelyFocused) = GetFocusState(controlName, r);

                var scrolled = UIUtility.IncrementWithScroll(r, value, 5);
                var scrollFired = scrolled != value;
                if (scrollFired) Commit(scrolled);

                var buttonFired = false;
                if (Widgets.ButtonImageWithHold(spinner.TopHalf(), TexUI.ArrowUp, controlName + ":up", disabled))
                {
                    Commit(value + 1);
                    buttonFired = true;
                }
                else if (Widgets.ButtonImageWithHold(spinner.BottomHalf(), TexUI.ArrowDown,
                             controlName + ":down", disabled))
                {
                    Commit(value - 1);
                    buttonFired = true;
                }

                // var raw = capturedBuffer;
                using (new TextBlock(InputMetrics(size)))
                using (new GUIColor(disabled ? color with { a = color.a * 0.5f } : color))
                {
                    // When genuinely focused use the original style, so its focused.background (the
                    // white border) renders naturally. Otherwise the locked style keeps a stray
                    // focus transfer from flashing.
                    var inputStyle = ResolveTextFieldStyle(variant, effectivelyFocused);
                    if (disabled)
                    {
                        GUI.Label(r, state.Buffer, inputStyle);
                    }
                    else
                    {
                        GUI.SetNextControlName(controlName);
                        state.Buffer = GUI.TextField(r, state.Buffer, inputStyle);
                    }
                }

                // A button or the scroll wheel already committed this frame; letting the stale
                // buffer through below would immediately overwrite it.
                if (buttonFired || scrollFired || disabled) return;
                if (isFocused && !effectivelyFocused) Unfocus();

                var buf = FilterNumeric(state.Buffer, min < 0);
                if (effectivelyFocused)
                {
                    // While focused, keep the raw buffer so it can be typed into freely.
                    state.Buffer = buf;
                    return;
                }

                // On blur: parse and clamp, or fall back to the last committed value.
                Commit(int.TryParse(buf, out var parsed) ? parsed : value);
            }, style: mergedStyle, id: key);

            return node;

            void Commit(int newValue)
            {
                if (disabled) return;
                var clamped = Mathf.Clamp(newValue, min, max);
                state.Buffer = clamped.ToString();
                if (clamped == value) return;
                value = clamped;
                // Keep External in step so next frame does not read this as an outside write. If the
                // caller ignores onChange, its value will differ and the widget reverts, which is
                // the correct outcome for a rejected commit.
                // state.External = clamped;
                onChange(clamped);
            }
        }
    }
}
