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

    // Hold-repeat state: direction (+1/-1/0), time the hold started, time of the last repeat tick.
    private static readonly Dictionary<string, (int direction, float heldSince, float lastRepeat)> SHoldState = [];

    private const float HoldInitialDelay = 0.4f;
    private const float HoldRepeatInterval = 0.07f;

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

            // Click: ButtonImage fires on MouseUp.
            if (Verse.Widgets.ButtonImage(upRect, TexPawnEditor.Up))
            {
                Commit(capturedValue + 1);
                return;
            }

            if (Verse.Widgets.ButtonImage(downRect, TexPawnEditor.Down))
            {
                Commit(capturedValue - 1);
                return;
            }

            // Hold-repeat: track which button (if any) is being held and fire on a timer.
            SHoldState.TryGetValue(key, out var hold);
            var mouseHeld = UnityEngine.Input.GetMouseButton(0);
            if (mouseHeld && Mouse.IsOver(upRect))
            {
                if (hold.direction != +1) hold = (+1, Time.realtimeSinceStartup, Time.realtimeSinceStartup);
            }
            else if (mouseHeld && Mouse.IsOver(downRect))
            {
                if (hold.direction != -1) hold = (-1, Time.realtimeSinceStartup, Time.realtimeSinceStartup);
            }
            else hold = default;

            SHoldState[key] = hold;

            if (hold.direction != 0 &&
                Time.realtimeSinceStartup - hold.heldSince >= HoldInitialDelay &&
                Time.realtimeSinceStartup - hold.lastRepeat >= HoldRepeatInterval)
            {
                SHoldState[key] = hold with { lastRepeat = Time.realtimeSinceStartup };
                Commit(capturedValue + hold.direction);
                return;
            }

            // Text field.
            GUI.SetNextControlName(controlName);
            var raw = Verse.Widgets.TextField(r, capturedBuffer);

            // Strip non-digit characters; allow a leading '-' only when min is negative.
            var buf = FilterNumeric(raw, min < 0);

            var isFocused = GUI.GetNameOfFocusedControl() == controlName;
            if (isFocused)
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