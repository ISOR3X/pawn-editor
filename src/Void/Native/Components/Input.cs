using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace Void.Native;

public static partial class Components
{
    public enum InputVariant
    {
        Solid = 0,
        Ghost = 1
    }

    private const float DefaultInputWidth = 128f;

    /// <summary>
    ///     Locked GUIStyle: all state backgrounds/colors match normal so that spurious focus transfers (e.g. button
    ///     clicks inside the field rect) produce no visual change. Used when the field is not genuinely focused.
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

    /// <summary>
    ///     Makes a disabled field use up a control id like an enabled one does. IMGUI hands them out in call order and
    ///     keyboard focus is one of them, so when a field is enabled and starts taking an id, every field after it would
    ///     get the next one, and the focus would move to the field before the one that had it.
    /// </summary>
    private static void TakeControlId()
    {
        GUIUtility.GetControlID(FocusType.Passive);
    }

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
        public UINode Input(string value, Action<string> onChange, int? maxLength = null,
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
                        GUIUtility.GetControlID(FocusType.Passive);
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
    }
}
