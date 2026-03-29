using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class TaffyExtensions
{
    private const float DefaultInputWidth = 120f;

    // Persistent per-widget state: keyed by "{contextKey}:{file}:{line}".
    // The draw callback writes here; the next frame's Input call reads it back into ref text.
    private static readonly Dictionary<string, string> SInputState = new();

    /// <summary>
    /// Adds a text-input leaf. Works like <c>CharacterCardUtility.DoNameInputRect</c>:
    /// call with a local copy of the value, then check for changes afterwards.
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
        Regex? pattern = null, Style? style = null, Color? color = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = $"{b.ContextKey}:{file}:{line}";

        // Read the last frame's typed value and write it back to the ref so the caller
        // can detect changes (and act on them) during the current build phase.
        if (SInputState.TryGetValue(key, out var stored))
            text = stored;

        style ??= new Style();
        style = style.WithDefaults(new Style
        {
            size = new Size<Dimension>(Dimension.Length(DefaultInputWidth), Dimension.Length(UIUtility.ButtonHeight))
        });

        var displayValue = text;
        b.AddLeaf(style, r =>
        {
            string input;
            using (new GUIColor(color ?? Color.white))
            {
                input = Verse.Widgets.TextField(r, displayValue);
            }

            if (maxLength.HasValue && input.Length > maxLength.Value) return;
            if (pattern != null && !pattern.IsMatch(input)) return;
            SInputState[key] = input;
        });
    }

    // Persistent per-widget state for numeric inputs: keyed by "{contextKey}:{id}" or "{contextKey}:{file}:{line}".
    private static readonly Dictionary<string, (string buffer, int value)> SNumericState = [];

    /// <summary>
    /// Adds a numeric text-input leaf that only accepts digit characters while typing,
    /// and applies min/max clamping only when the field loses focus.
    /// Pass an explicit <paramref name="id"/> when the call site is a shared helper method
    /// (multiple callers would otherwise share the same file:line key).
    /// </summary>
    public static void InputNumber(this TaffyBuilder b, ref int value, int min = 0, int max = 9999,
        Style? style = null, string? id = null,
        [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
    {
        var key = id != null ? $"{b.ContextKey}:{id}" : $"{b.ContextKey}:{file}:{line}";
        var controlName = $"InputNumber_{key}";

        var hasState = SNumericState.TryGetValue(key, out var stored);
        if (hasState)
            value = stored.value;

        style ??= new Style();
        style = style.WithDefaults(new Style
        {
            size = new Size<Dimension>(Dimension.Length(DefaultInputWidth), Dimension.Length(UIUtility.ButtonHeight))
        });

        var capturedValue = value;
        var capturedBuffer = hasState ? stored.buffer : value.ToString();

        b.AddLeaf(style, r =>
        {
            var r2 = r.TakeRightPart(r.height / 2f);
            var upRect = r2.TopHalf();
            var downRect = r2.BottomHalf();

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
            var raw = Verse.Widgets.TextField(r, capturedBuffer);

            // Skip state update when a button just committed, otherwise the old buffer
            // would overwrite the new value on the same frame.
            if (!buttonFired)
            {
                var buf = FilterNumeric(raw, min < 0);

                if (GUI.GetNameOfFocusedControl() == controlName)
                {
                    // While focused: preserve the raw buffer so the user can type freely.
                    // Keep the last committed value for callers reading this frame.
                    SNumericState[key] = (buf, capturedValue);
                }
                else
                {
                    // On blur: parse and clamp, or revert to the last committed value if empty/invalid.
                    var v = int.TryParse(buf, out var p) ? Mathf.Clamp(p, min, max) : capturedValue;
                    SNumericState[key] = (v.ToString(), v);
                }
            }
        });
        return;

        void Commit(int newVal)
        {
            var clamped = Mathf.Clamp(newVal, min, max);
            SNumericState[key] = (clamped.ToString(), clamped);
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