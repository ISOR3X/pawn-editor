using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace Void.Native;

public static partial class Components
{
    /// <summary>
    ///     Text buffer for numeric inputs. This allows us to type "5000" for it to be later clamped to 50 on blur.
    /// </summary>
    private sealed class InputNumberState
    {
        public string Buffer = "";

        /// <summary>
        ///     The value the buffer was last synced to. Only when the caller's value differs from this did it change
        ///     from outside; a buffer that differs from the value is just being typed in, until it is committed.
        /// </summary>
        public int Seen;
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

    extension(UIBranch branch)
    {
        /// <summary>
        ///     Adds a numeric input with incrementing buttons and scroll-to-increment.
        ///     Only numbers are accepted while typing and <paramref name="min" />/<paramref name="max" /> clamping is
        ///     applied on blur, so an intermediate value can be typed through.
        ///     <paramref name="onChange" /> fires only when the committed value actually changes.
        /// </summary>
        public UINode InputNumber(int value, Action<int> onChange, int min = 0, int max = 9999,
            Action<Rect>? onHover = null, Action<Rect>? draw = null,
            bool disabled = false, ComponentSize size = ComponentSize.Default,
            InputVariant variant = InputVariant.Solid,
            Style? style = null, string? id = null, [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);
            var controlName = $"VoidInputNumber_{key}";
            var state = branch.UpsertState(key, () => new InputNumberState { Seen = value, Buffer = value.ToString() });

            // Follow the value when it changed from outside. Comparing it to the buffer instead would wipe whatever is
            // being typed at the next build, since a typed number is only committed on blur.
            if (state.Seen != value)
            {
                state.Seen = value;
                state.Buffer = value.ToString();
            }

            var baseStyle = InputStyles.Get(size);
            var mergedStyle = style == null ? baseStyle : style.Merge(baseStyle);
            var color = mergedStyle.color ?? Color.white;

            return branch.Div(draw: r =>
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

                using (new TextBlock(InputMetrics(size)))
                using (new GUIColor(disabled ? color with { a = color.a * 0.5f } : color))
                {
                    // When genuinely focused use the original style, so its focused.background (the
                    // white border) renders naturally. Otherwise the locked style keeps a stray
                    // focus transfer from flashing.
                    var inputStyle = ResolveTextFieldStyle(variant, effectivelyFocused);
                    if (disabled)
                    {
                        TakeControlId();
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

            void Commit(int newValue)
            {
                if (disabled) return;
                var clamped = Mathf.Clamp(newValue, min, max);
                state.Buffer = clamped.ToString();
                if (clamped == value) return;
                value = clamped;
                state.Seen = clamped;
                // If the caller ignores onChange, its value will differ from Seen next frame and the widget reverts,
                // which is the correct outcome for a rejected commit.
                onChange(clamped);
            }
        }
    }
}
