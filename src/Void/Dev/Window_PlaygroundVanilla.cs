#if DEBUG
using System.Text;
using System.Text.RegularExpressions;
using LudeonTK;
using UnityEngine;
using Verse;
using static VoidComponents;

namespace Void.Dev;

/// <summary>
///     Vanilla-API twin of <see cref="Window_Playground" /> for benchmarking. Draws the same tab bar and
///     the same Buttons and Input playgrounds with plain <see cref="Verse.Widgets" /> calls and manual
///     <see cref="Rect" /> math, so profiler numbers can be compared against the <see cref="Taffy.UITree" />
///     version. The remaining tabs exist so the tab bar draws the same number of buttons.
///     Opens on the right half of the screen; <see cref="Window_Playground" /> takes the left.
/// </summary>
public class Window_PlaygroundVanilla : Window
{
    private const float Gap = 8f;
    private const float RowGap = 10f;
    private const float DefaultInputWidth = 160f;

    private static readonly Tab[] Tabs = (Tab[])Enum.GetValues(typeof(Tab));

    // Whole-value regex, mirroring the Taffy playground.
    private static readonly Regex LettersOnly = new("^[a-zA-Z]*$");


    // Mirrors the caches in VoidComponents.Input so the two windows do the same GUIStyle work.
    private static readonly Dictionary<GameFont, GUIStyle> LockedTextFields = [];
    private static readonly Dictionary<GameFont, GUIStyle> GhostTextFields = [];

    // Input tab state, mirroring the fields on Window_Playground one for one.
    private readonly string[] _inputRow = ["one", "two", "three"];
    private readonly List<string> _listItems = Enumerable.Range(1, 200).Select(i => $"Item {i}").ToList();

    // Vanilla has no per-node state, so numeric buffers live in a dictionary keyed by control name.
    // This is the bookkeeping that branch.State does for the Taffy version.
    private readonly Dictionary<string, string> _numBuffers = [];

    private int _clicks;
    private Vector2 _filterScroll;
    private string _inputCapped = "max 8";
    private string _inputDigits = "2077";
    private string _inputExternal = "100";
    private string _inputFilter = "";
    private string _inputFull = "Full width";
    private string _inputGhost = "Ghost variant";
    private string _inputGrow = "grows with the row";
    private string _inputLarge = "Large";
    private string _inputPattern = "letters";
    private string _inputSelfKeyed = "retype me";
    private string _inputSmall = "Small";
    private string _inputStd = "Default";
    private int _numBasic = 42;
    private int _numClamped = 5;
    private int _numLarge = 3;
    private int _numSigned;
    private int _numSmall = 1;
    private int _numStd = 2;
    private bool _showExtra;

    private Tab _tab = Tab.Buttons;

    public Window_PlaygroundVanilla()
    {
        resizeable = true;
        draggable = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
    }

    public override Vector2 InitialSize => new(UI.screenWidth / 2f, UI.screenHeight);

    [DebugAction("Void", "Open vanilla playground", allowedGameStates = AllowedGameStates.Invalid)]
    private static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_PlaygroundVanilla>())
            Find.WindowStack.TryRemove(typeof(Window_PlaygroundVanilla));
        else Find.WindowStack.Add(new Window_PlaygroundVanilla());
    }

    /// <summary>
    ///     Right half of the screen, so this can sit beside <see cref="Window_Playground" /> for a
    ///     side-by-side comparison. Also re-applied on a resolution change by the base class.
    /// </summary>
    public override void SetInitialSizeAndPosition()
    {
        var half = UI.screenWidth / 2f;
        windowRect = new Rect(half, 0f, half, UI.screenHeight).Rounded();
    }

    public override void DoWindowContents(Rect inRect)
    {
        var y = inRect.y;
        y = TabBar(inRect, y) + RowGap;

        switch (_tab)
        {
            case Tab.Buttons: ButtonPlayground(inRect, y); break;
            case Tab.Input: InputPlayground(inRect, y); break;
            default:
                Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f),
                    $"{_tab}: not implemented in the vanilla playground.");
                break;
        }
    }

    /// <summary>
    ///     Row of tab buttons, a flexible spacer, and a right-aligned overlay toggle.
    /// </summary>
    private float TabBar(Rect inRect, float y)
    {
        var x = inRect.x;
        var height = ButtonMetrics(ComponentSize.Default).height;

        foreach (var tab in Tabs)
            if (DrawButton(ref x, y, tab.ToString(), null, ComponentSize.Default,
                    tab == _tab ? ButtonVariant.Solid : ButtonVariant.Ghost, false))
                _tab = tab;

        // Right-aligned: measure first, then place at the far edge.
        var overlayWidth = MeasureButton("Overlay", null, ComponentSize.Small);
        var smallHeight = ButtonMetrics(ComponentSize.Small).height;
        var ox = inRect.xMax - overlayWidth;
        if (DrawButton(ref ox, y + (height - smallHeight) / 2f, "Overlay", null, ComponentSize.Small,
                ButtonVariant.Solid, false))
            VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug;

        return y + height;
    }

    private void ButtonPlayground(Rect inRect, float y)
    {
        using (new TextBlock(GameFont.Medium))
        {
            var title = $"Button playground (clicks: {_clicks})";
            var h = Text.CalcHeight(title, inRect.width);
            Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, h), title);
            y += h + RowGap;
        }

        // One row per size.
        var x = inRect.x;
        if (DrawButton(ref x, y, "Small", null, ComponentSize.Small, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Small + icon", TexUI.ArrowRight, ComponentSize.Small, ButtonVariant.Solid,
                false)) _clicks++;
        if (DrawButton(ref x, y, null, TexUI.ArrowRight, ComponentSize.Small, ButtonVariant.Solid, false)) _clicks++;
        y += ButtonMetrics(ComponentSize.Small).height + RowGap;

        x = inRect.x;
        if (DrawButton(ref x, y, "Default", null, ComponentSize.Default, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Default + icon", TexUI.ArrowRight, ComponentSize.Default, ButtonVariant.Solid,
                false)) _clicks++;
        if (DrawButton(ref x, y, null, TexUI.ArrowRight, ComponentSize.Default, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Ghost", null, ComponentSize.Default, ButtonVariant.Ghost, false)) _clicks++;
        DrawButton(ref x, y, "Disabled", null, ComponentSize.Default, ButtonVariant.Solid, true);
        y += ButtonMetrics(ComponentSize.Default).height + RowGap;

        x = inRect.x;
        if (DrawButton(ref x, y, "Large", null, ComponentSize.Large, ButtonVariant.Solid, false)) _clicks++;
        if (DrawButton(ref x, y, "Large + icon", TexUI.ArrowRight, ComponentSize.Large, ButtonVariant.Solid,
                false)) _clicks++;
        y += ButtonMetrics(ComponentSize.Large).height + RowGap;

        // Block button: full parent width.
        var blockHeight = ButtonMetrics(ComponentSize.Default).height;
        if (DrawButtonRect(new Rect(inRect.x, y, inRect.width, blockHeight), _showExtra ? "Hide extra" : "Show extra",
                null, ComponentSize.Default, ButtonVariant.Solid, false))
            _showExtra = !_showExtra;
        y += blockHeight + RowGap;

        if (!_showExtra) return;

        // Toggled block: padded column with a wrapping label and a truncated long button.
        const float padding = 8f;
        const float innerGap = 6f;
        var inner = new Rect(inRect.x + padding, y + padding, inRect.width - padding * 2f, 0f);

        const string text =
            "This block is added and removed by the button above. A very long label follows to check truncation:";
        var textHeight = Text.CalcHeight(text, inner.width);
        Verse.Widgets.Label(new Rect(inner.x, inner.y, inner.width, textHeight), text);

        var bx = inner.x;
        DrawButton(ref bx, inner.y + textHeight + innerGap,
            "This label is far too long for the space it has been given and should truncate",
            TexUI.ArrowLeft, ComponentSize.Default, ButtonVariant.Solid, false, 220f);
    }


    /// <summary>
    ///     Same content as <c>Window_Playground.InputPlayground</c>, laid out by hand. Every row here
    ///     that the Taffy version expresses as a style (centering, growing, a fixed label column) has
    ///     to be computed: measure, position, advance.
    /// </summary>
    private void InputPlayground(Rect inRect, float y)
    {
        var rowH = InputHeight(ComponentSize.Large);

        y = SectionTitle(inRect, y, "Input playground", GameFont.Medium);

        // Sizes. Each size picks its own font and height, so the boxes differ in both.
        y = SectionTitle(inRect, y, "Sizes", GameFont.Tiny);
        var x = inRect.x;
        _inputSmall = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputSmall, "v-small",
            size: ComponentSize.Small);
        _inputStd = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputStd, "v-std");
        _inputLarge = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputLarge, "v-large",
            size: ComponentSize.Large);
        DrawInputAt(ref x, y, rowH, DefaultInputWidth, "disabled", "v-disabled", disabled: true);
        y += rowH + RowGap;

        // Sizing. Full width is trivial here; the grown row is not, because the label column and the
        // trailing button have to be measured first so the field can be given what is left over.
        y = SectionTitle(inRect, y, "Sizing: fixed default, full width, and flex-grown", GameFont.Tiny);
        _inputFull = DrawInput(new Rect(inRect.x, y, inRect.width, InputHeight(ComponentSize.Default)),
            _inputFull, "v-fullwidth");
        y += InputHeight(ComponentSize.Default) + RowGap;

        x = inRect.x;
        LabelAt(ref x, y, rowH, inRect.xMax, "Label", GameFont.Small, 60f);
        var clearWidth = MeasureButton("Clear", null, ComponentSize.Small);
        var growWidth = Mathf.Max(0f, inRect.xMax - x - clearWidth - Gap);
        _inputGrow = DrawInputAt(ref x, y, rowH, growWidth, _inputGrow, "v-grow");
        if (DrawButton(ref x, y + (rowH - ButtonMetrics(ComponentSize.Small).height) / 2f, "Clear", null,
                ComponentSize.Small, ButtonVariant.Solid, false)) _inputGrow = "";
        y += rowH + RowGap;

        // Three ways to refuse an edit, in the order the field applies them.
        y = SectionTitle(inRect, y, "Rejecting edits: maxLength, pattern, and caller-side validation",
            GameFont.Tiny);
        x = inRect.x;
        _inputCapped = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputCapped, "v-capped", 8);
        LabelAt(ref x, y, rowH, inRect.xMax, $"{_inputCapped.Length}/8", GameFont.Tiny, 40f);
        _inputPattern = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputPattern, "v-pattern",
            pattern: LettersOnly);
        LabelAt(ref x, y, rowH, inRect.xMax, "pattern: letters", GameFont.Tiny);
        var typedDigits = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputDigits, "v-digits", 6);
        if (typedDigits.All(char.IsDigit)) _inputDigits = typedDigits;
        LabelAt(ref x, y, rowH, inRect.xMax, "callback: digits", GameFont.Tiny);
        y += rowH + RowGap;

        // Variants, and click-outside to release focus.
        y = SectionTitle(inRect, y, "Variants, and click-outside to release focus", GameFont.Tiny);
        x = inRect.x;
        _inputStd = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputStd, "v-variant-solid");
        _inputGhost = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputGhost, "v-variant-ghost",
            variant: InputVariant.Ghost);
        LabelAt(ref x, y, rowH, inRect.xMax, "ghost", GameFont.Tiny);
        if (DrawButton(ref x, y + (rowH - ButtonMetrics(ComponentSize.Small).height) / 2f,
                "Click me (focus should release)", null, ComponentSize.Small, ButtonVariant.Solid, false)) _clicks++;
        y += rowH + RowGap;

        // External writes land immediately.
        y = SectionTitle(inRect, y, "External writes land immediately", GameFont.Tiny);
        x = inRect.x;
        _inputExternal = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputExternal, "v-external");
        var smallY = y + (rowH - ButtonMetrics(ComponentSize.Small).height) / 2f;
        if (DrawButton(ref x, smallY, "Set 1234", null, ComponentSize.Small, ButtonVariant.Solid, false))
            _inputExternal = "1234";
        if (DrawButton(ref x, smallY, "+1", null, ComponentSize.Small, ButtonVariant.Solid, false))
            _inputExternal = (int.TryParse(_inputExternal, out var n) ? n + 1 : 0).ToString();
        LabelAt(ref x, y, rowH, inRect.xMax, $"backing value: {_inputExternal}", GameFont.Tiny);
        y += rowH + RowGap;

        // Independent focus. The keys are hand-written here; the Taffy version derives them from the
        // call site and only needs explicit ids because the loop shares one.
        y = SectionTitle(inRect, y, "Independent focus: tab between these, edits must not bleed", GameFont.Tiny);
        x = inRect.x;
        for (var i = 0; i < _inputRow.Length; i++)
            _inputRow[i] = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputRow[i], $"v-row-{i}");
        LabelAt(ref x, y, rowH, inRect.xMax, string.Join(" | ", _inputRow), GameFont.Tiny);
        y += rowH + RowGap;

        // InputNumber.
        y = SectionTitle(inRect, y,
            "InputNumber: spinner buttons hold-repeat, scroll over a field to increment (shift = 5)", GameFont.Tiny);
        x = inRect.x;
        _numBasic = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numBasic, "v-num-basic");
        LabelAt(ref x, y, rowH, inRect.xMax, $"value: {_numBasic}", GameFont.Tiny, 80f);
        _numClamped = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numClamped, "v-num-clamped", 1, 10);
        LabelAt(ref x, y, rowH, inRect.xMax, "1-10, clamped on blur", GameFont.Tiny);
        _numSigned = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numSigned, "v-num-signed", -50, 50);
        LabelAt(ref x, y, rowH, inRect.xMax, "-50..50, leading minus allowed", GameFont.Tiny);
        DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, 7, "v-num-disabled", disabled: true);
        y += rowH + RowGap;

        // Sizes and the ghost variant, with two ghosts sharing one value.
        x = inRect.x;
        _numSmall = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numSmall, "v-num-small",
            size: ComponentSize.Small);
        _numStd = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numStd, "v-num-std");
        _numLarge = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numLarge, "v-num-large",
            size: ComponentSize.Large);
        _numBasic = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numBasic, "v-num-ghost-a",
            variant: InputVariant.Ghost);
        _numBasic = DrawInputNumberAt(ref x, y, rowH, DefaultInputWidth, _numBasic, "v-num-ghost-b",
            variant: InputVariant.Ghost);
        LabelAt(ref x, y, rowH, inRect.xMax, "two ghosts, one value", GameFont.Tiny);
        y += rowH + RowGap;

        // Placeholder for the Taffy tab's keying anti-pattern, which has no vanilla equivalent: there
        // are no nodes to free, and the control name here is a constant. Kept so both tabs draw the
        // same number of fields.
        y = SectionTitle(inRect, y, "Anti-pattern slot (no vanilla equivalent: control name is constant)",
            GameFont.Tiny);
        _inputSelfKeyed = DrawInput(new Rect(inRect.x, y, DefaultInputWidth, InputHeight(ComponentSize.Default)),
            _inputSelfKeyed, "v-self-keyed");
        y += InputHeight(ComponentSize.Default) + RowGap;

        // Filter a list, virtualized the same way the List component does it.
        y = SectionTitle(inRect, y, "Filter a list", GameFont.Tiny);
        var filtered = string.IsNullOrEmpty(_inputFilter)
            ? _listItems
            : _listItems.Where(i => i.IndexOf(_inputFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        x = inRect.x;
        _inputFilter = DrawInputAt(ref x, y, rowH, DefaultInputWidth, _inputFilter, "v-filter");
        LabelAt(ref x, y, rowH, inRect.xMax, $"{filtered.Count} / {_listItems.Count} items", GameFont.Tiny);
        if (DrawButton(ref x, y + (rowH - ButtonMetrics(ComponentSize.Small).height) / 2f, "Reset", null,
                ComponentSize.Small, ButtonVariant.Solid, false)) _inputFilter = "";
        y += rowH + RowGap;

        DrawVirtualList(new Rect(inRect.x, y, inRect.width, 6f * UIUtility.ButtonHeight), filtered);
    }

    /// <summary>
    ///     Fixed-height scroll view that only draws the visible rows, mirroring the List component.
    /// </summary>
    private void DrawVirtualList(Rect rect, IReadOnlyList<string> items)
    {
        const float itemHeight = UIUtility.ButtonHeight;
        var totalHeight = items.Count > 0 ? items.Count * itemHeight : itemHeight;
        var scrollbarW = totalHeight > rect.height ? UIUtility.ScrollBarWidth + 4f : 0f;
        var viewRect = new Rect(0f, 0f, rect.width - scrollbarW, totalHeight);

        Verse.Widgets.BeginScrollView(rect, ref _filterScroll, viewRect);

        if (items.Count > 0)
        {
            var first = Math.Max(0, (int)(_filterScroll.y / itemHeight));
            var last = Math.Min(items.Count - 1, (int)((_filterScroll.y + rect.height) / itemHeight));
            for (var i = first; i <= last; i++)
                Verse.Widgets.Label(new Rect(0f, i * itemHeight, viewRect.width, itemHeight), items[i]);
        }
        else
        {
            using (new GUIColor(ColoredText.SubtleGrayColor))
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Verse.Widgets.Label(viewRect, "No results available.");
            }
        }

        Verse.Widgets.EndScrollView();
    }

    /// <summary>Draws a heading and returns the y below it.</summary>
    private static float SectionTitle(Rect inRect, float y, string text, GameFont font)
    {
        using (new TextBlock(font))
        {
            var h = Text.CalcHeight(text, inRect.width);
            Verse.Widgets.Label(new Rect(inRect.x, y, inRect.width, h), text);
            return y + h + RowGap;
        }
    }

    /// <summary>
    ///     Draws a label vertically centered in the row and advances the cursor. Width is measured
    ///     when not given, which is the manual stand-in for letting the layout size the box.
    /// </summary>
    private static void LabelAt(ref float x, float y, float rowHeight, float maxX, string text, GameFont font,
        float? width = null)
    {
        using (new TextBlock(font, TextAnchor.MiddleLeft, false))
        {
            // Clamped to the content rect: the Taffy row shrinks its text nodes to fit, and without
            // this the hand-laid row just draws past the window edge.
            var w = Mathf.Max(0f, Mathf.Min(width ?? Text.CalcSize(text).x, maxX - x));
            Verse.Widgets.Label(new Rect(x, y, w, rowHeight), text.Truncate(w));
            x += w + Gap;
        }
    }

    private static float InputHeight(ComponentSize size)
    {
        return size == ComponentSize.Small ? 20f : UIUtility.ButtonHeight;
    }

    private static GameFont InputFont(ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => GameFont.Tiny,
            ComponentSize.Default => GameFont.Small,
            ComponentSize.Large => GameFont.Medium,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    private static string DrawInputAt(ref float x, float y, float rowHeight, float width, string value,
        string controlName, int? maxLength = null, Regex? pattern = null, bool disabled = false,
        ComponentSize size = ComponentSize.Default, InputVariant variant = InputVariant.Solid)
    {
        var h = InputHeight(size);
        var rect = new Rect(x, y + (rowHeight - h) / 2f, width, h);
        x += width + Gap;
        return DrawInput(rect, value, controlName, maxLength, pattern, disabled, size, variant);
    }

    /// <summary>
    ///     Same drawing and focus handling as the VoidComponents Input, minus the node and the style
    ///     merge: pre-compute focus so the locked style applies on the click frame, draw, then accept
    ///     or refuse the edit.
    /// </summary>
    private static string DrawInput(Rect rect, string value, string controlName, int? maxLength = null,
        Regex? pattern = null, bool disabled = false, ComponentSize size = ComponentSize.Default,
        InputVariant variant = InputVariant.Solid)
    {
        var (isFocused, effectivelyFocused) = GetFocusState(controlName, rect);

        var next = value;
        using (new TextBlock(InputFont(size)))
        using (new GUIColor(disabled ? new Color(1f, 1f, 1f, 0.5f) : Color.white))
        {
            var style = ResolveTextFieldStyle(variant, effectivelyFocused);
            if (disabled)
            {
                GUI.Label(rect, value, style);
            }
            else
            {
                GUI.SetNextControlName(controlName);
                next = GUI.TextField(rect, value, style);
            }
        }

        if (disabled) return value;
        if (isFocused && !effectivelyFocused) Unfocus();

        if (next == value) return value;
        if (maxLength.HasValue && next.Length > maxLength.Value) return value;
        if (pattern != null && !pattern.IsMatch(next)) return value;
        return next;
    }

    private int DrawInputNumberAt(ref float x, float y, float rowHeight, float width, int value,
        string controlName, int min = 0, int max = 9999, bool disabled = false,
        ComponentSize size = ComponentSize.Default, InputVariant variant = InputVariant.Solid)
    {
        var h = InputHeight(size);
        var rect = new Rect(x, y + (rowHeight - h) / 2f, width, h);
        x += width + Gap;
        return DrawInputNumber(rect, value, controlName, min, max, disabled, size, variant);
    }

    /// <summary>
    ///     Numeric field with spinner buttons and scroll-to-increment. The typed buffer lives in
    ///     <see cref="_numBuffers" /> because there is no node to hang it on.
    /// </summary>
    private int DrawInputNumber(Rect rect, int value, string controlName, int min, int max, bool disabled,
        ComponentSize size, InputVariant variant)
    {
        if (!_numBuffers.TryGetValue(controlName, out var buffer) || value.ToString() != buffer)
            buffer = value.ToString();

        var spinner = rect.RightPartPixels(rect.height / 2f);
        spinner.x -= GenUI.GapTiny;

        var (isFocused, effectivelyFocused) = GetFocusState(controlName, rect);

        var scrolled = UIUtility.IncrementWithScroll(rect, value, 5);
        var scrollFired = scrolled != value;
        if (scrollFired && !disabled)
        {
            value = Mathf.Clamp(scrolled, min, max);
            buffer = value.ToString();
        }

        var buttonFired = false;
        if (Widgets.ButtonImageWithHold(spinner.TopHalf(), TexUI.ArrowUp, controlName + ":up", disabled))
        {
            if (!disabled) value = Mathf.Clamp(value + 1, min, max);
            buffer = value.ToString();
            buttonFired = true;
        }
        else if (Widgets.ButtonImageWithHold(spinner.BottomHalf(), TexUI.ArrowDown, controlName + ":down", disabled))
        {
            if (!disabled) value = Mathf.Clamp(value - 1, min, max);
            buffer = value.ToString();
            buttonFired = true;
        }

        using (new TextBlock(InputFont(size)))
        using (new GUIColor(disabled ? new Color(1f, 1f, 1f, 0.5f) : Color.white))
        {
            var style = ResolveTextFieldStyle(variant, effectivelyFocused);
            if (disabled)
            {
                GUI.Label(rect, buffer, style);
            }
            else
            {
                GUI.SetNextControlName(controlName);
                buffer = GUI.TextField(rect, buffer, style);
            }
        }

        _numBuffers[controlName] = buffer;
        if (buttonFired || scrollFired || disabled) return value;
        if (isFocused && !effectivelyFocused) Unfocus();

        var filtered = FilterNumeric(buffer, min < 0);
        if (effectivelyFocused)
        {
            // While focused, keep the raw buffer so it can be typed into freely.
            _numBuffers[controlName] = filtered;
            return value;
        }

        // On blur: parse and clamp, or fall back to the value we came in with.
        var committed = Mathf.Clamp(int.TryParse(filtered, out var parsed) ? parsed : value, min, max);
        _numBuffers[controlName] = committed.ToString();
        return committed;
    }

    private static GUIStyle ResolveTextFieldStyle(InputVariant variant, bool focused)
    {
        var font = Text.Font;

        if (variant == InputVariant.Ghost)
        {
            if (GhostTextFields.TryGetValue(font, out var ghost)) return ghost;
            ghost = new GUIStyle(Text.CurTextFieldStyle);
            ghost.normal.background = null;
            ghost.focused.background = null;
            ghost.hover.background = null;
            ghost.active.background = null;
            ghost.border = new RectOffset(0, 0, 0, 0);
            return GhostTextFields[font] = ghost;
        }

        if (focused) return Text.CurTextFieldStyle;

        if (LockedTextFields.TryGetValue(font, out var locked)) return locked;
        locked = new GUIStyle(Text.CurTextFieldStyle);
        locked.focused = new GUIStyleState
            { background = locked.normal.background, textColor = locked.normal.textColor };
        locked.hover = new GUIStyleState { background = locked.normal.background, textColor = locked.normal.textColor };
        locked.active = new GUIStyleState
            { background = locked.normal.background, textColor = locked.normal.textColor };
        return LockedTextFields[font] = locked;
    }

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


    /// <returns>Horizontal padding, button height, icon size, icon + label gap, font.</returns>
    private static (float padding, float height, float iconSize, float iconGap, GameFont font) ButtonMetrics(
        ComponentSize size)
    {
        return size switch
        {
            ComponentSize.Small => (12f, 20f, 12f, 4f, GameFont.Tiny),
            ComponentSize.Default => (GenUI.GapLabel, UIUtility.ButtonHeight, 18f, 6f, GameFont.Small),
            ComponentSize.Large => (52f, Verse.Widgets.BackButtonHeight, 18f, 6f, GameFont.Small),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>
    ///     Intrinsic width of a button: padding on both sides, the icon, the gap, and the measured label.
    ///     Icon-only buttons are square.
    /// </summary>
    private static float MeasureButton(string? label, Texture2D? icon, ComponentSize size)
    {
        var (padding, height, iconSize, iconGap, font) = ButtonMetrics(size);
        if (label == null && icon != null) return height;

        var width = padding * 2f;
        if (icon != null) width += iconSize + iconGap;
        if (label != null)
            using (new TextBlock(font))
            {
                width += Text.CalcSize(label).x;
            }

        return width;
    }

    /// <summary>
    ///     Draws a button at the cursor and advances it by the button width plus the row gap.
    /// </summary>
    private static bool DrawButton(ref float x, float y, string? label, Texture2D? icon, ComponentSize size,
        ButtonVariant variant, bool disabled, float? maxWidth = null)
    {
        var width = MeasureButton(label, icon, size);
        if (maxWidth is { } max) width = Mathf.Min(width, max);
        var rect = new Rect(x, y, width, ButtonMetrics(size).height);
        x += width + Gap;
        return DrawButtonRect(rect, label, icon, size, variant, disabled);
    }

    /// <summary>
    ///     Same drawing as the VoidComponents Button: invisible hit box, atlas or hover highlight,
    ///     centered icon + label, dark overlay when disabled.
    /// </summary>
    private static bool DrawButtonRect(Rect rect, string? label, Texture2D? icon, ComponentSize size,
        ButtonVariant variant, bool disabled)
    {
        var (padding, _, iconSize, iconGap, font) = ButtonMetrics(size);
        var iconOnly = label == null && icon != null;
        if (iconOnly) padding = 0f;

        var clicked = Verse.Widgets.ButtonInvisible(rect);

        if (variant == ButtonVariant.Solid)
        {
            var atlas = Verse.Widgets.ButtonBGAtlas;
            if (Mouse.IsOver(rect) && !disabled)
            {
                atlas = Verse.Widgets.ButtonBGAtlasMouseover;
                if (Input.GetMouseButton(0)) atlas = Verse.Widgets.ButtonBGAtlasClick;
            }

            Verse.Widgets.DrawAtlas(rect, atlas);
        }
        else
        {
            Verse.Widgets.DrawHighlightIfMouseover(rect);
        }

        // Content: icon then label, centered as a group, label truncated to whatever is left.
        var content = rect.ContractedBy(padding, 0f);
        using (new TextBlock(font))
        {
            var contentWidth = 0f;
            var labelWidth = 0f;
            if (icon != null) contentWidth += iconSize;
            if (icon != null && label != null) contentWidth += iconGap;
            if (label != null)
            {
                labelWidth = Mathf.Max(0f, Mathf.Min(Text.CalcSize(label).x, content.width - contentWidth));
                contentWidth += labelWidth;
            }

            var cx = content.x + Mathf.Max(0f, (content.width - contentWidth) / 2f);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(cx, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize), icon);
                cx += iconSize + (label != null ? iconGap : 0f);
            }

            if (label != null)
                using (new TextBlock(null, TextAnchor.MiddleLeft, false))
                {
                    Verse.Widgets.Label(new Rect(cx, rect.y, labelWidth, rect.height), label.Truncate(labelWidth));
                }
        }

        if (disabled) Verse.Widgets.DrawBoxSolid(rect, Color.black with { a = 0.25f });

        return clicked && !disabled;
    }

    private enum Tab
    {
        Buttons,
        Text,
        Icons,
        Input,
        Collapsible,
        List,
        Context,
        Layout
    }
}
#endif