using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Random = UnityEngine.Random;

namespace PawnEditor;

[HotSwappable]
public class Dialog_ColorPicker : Window
{
    private readonly List<Color> _colors;
    private readonly Color? _defaultColor;
    private readonly Color? _favoriteColor;
    private readonly Color _oldColor;
    private readonly Action<Color> _onSelect;

    private readonly string[] _textfieldBuffers = new string[3];
    private bool _hsvColorWheelDragging;
    private string _previousFocusedControlName;
    private Vector2 _scrollPosition;

    private Color _selectedColor;

    private Color _textfieldColorBuffer;

    public Dialog_ColorPicker(Action<Color> onSelect, ColorType colorType, Color oldColor)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;

        _colors = DefDatabase<ColorDef>.AllDefs.Where(cd => cd.colorType == colorType)
           .Select(cd => cd.color)
           .ToList();
    }

    public Dialog_ColorPicker(Action<Color> onSelect, List<Color> colors, Color oldColor)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;
        _colors = colors
           .OrderBy(color =>
            {
                Color.RGBToHSV(color, out var colorHue, out var colorSat, out var colorVal);
                return colorSat < 0.1 ? 1 : 0; // Place colors with saturation 0 at the end
            })
           .ThenBy(color =>
            {
                Color.RGBToHSV(color, out var colorHue, out var colorSat, out var colorVal);
                return colorHue;
            })
           .ThenBy(color =>
            {
                Color.RGBToHSV(color, out var colorHue, out var colorSat, out var colorVal);
                return colorSat;
            })
           .ThenBy(color =>
            {
                Color.RGBToHSV(color, out var colorHue, out var colorSat, out var colorVal);
                return colorVal;
            })
           .ToList();

        closeOnAccept = false;
        absorbInputAroundWindow = true;
    }

    public Dialog_ColorPicker(Action<Color> onSelect, List<Color> colors, Color oldColor, Color? defaultColor) : this(onSelect, colors, oldColor) =>
        _defaultColor = defaultColor;

    public Dialog_ColorPicker(Action<Color> onSelect, List<Color> colors, Color oldColor, Color? defaultColor, Color? favoriteColor) : this(onSelect, colors,
        oldColor, defaultColor) =>
        _favoriteColor = favoriteColor;

    public override Vector2 InitialSize => new(600f, 450f);

    public override void DoWindowContents(Rect inRect)
    {
        using (TextBlock.Default())
        {
            var layout = new RectDivider(inRect, 91185);
            HeaderRow(ref layout);
            BottomButtons(ref layout);
            ColorPalette(ref layout, ref _selectedColor);
            ColorReadback(ref layout, _selectedColor, _oldColor);
            ColorTextFields(ref layout);
            var rectDivider = layout.NewRow(layout.Rect.width);
            Widgets.HSVColorWheel(rectDivider.Rect, ref _selectedColor, ref _hsvColorWheelDragging, 1f);

            if (Event.current.type != EventType.Layout)
                return;
            _previousFocusedControlName = GUI.GetNameOfFocusedControl();
        }
    }

    private static void HeaderRow(ref RectDivider layout)
    {
        using (new TextBlock(GameFont.Medium))
        {
            var taggedString = "ChooseAColor".Translate().CapitalizeFirst();
            var rectDivider = layout.NewRow(Text.CalcHeight(taggedString, layout.Rect.width));
            Widgets.Label(rectDivider, taggedString);
        }
    }

    private void BottomButtons(ref RectDivider layout)
    {
        var rectDivider = layout.NewRow(UIUtility.BottomButtonSize.y, VerticalJustification.Bottom);
        if (Widgets.ButtonText(rectDivider.NewCol(UIUtility.BottomButtonSize.x), "Cancel".Translate()))
            Close();
        if (Widgets.ButtonText(rectDivider.NewCol(UIUtility.BottomButtonSize.x, HorizontalJustification.Right),
                "Accept".Translate()))
            Accept();
    }

    private static void ColorReadback(ref RectDivider layout, Color color, Color oldColor)
    {
        var rectDivider1 = layout.NewRow(Text.LineHeightOf(GameFont.Small) * 2 + 26f + 8f, VerticalJustification.Bottom);
        // Widgets.DrawRectFast(rectDivider1.Rect, Color.green);
        var label1 = "CurrentColor".Translate().CapitalizeFirst();
        var label2 = "OldColor".Translate().CapitalizeFirst();

        var width = Mathf.Max(100f, label1.GetWidthCached(), label2.GetWidthCached());

        var rectDivider2 = rectDivider1.NewRow(Text.LineHeight);
        Widgets.Label(rectDivider2.NewCol(width), label1);
        Widgets.DrawBoxSolid(rectDivider2, color);
        var rectDivider3 = rectDivider1.NewRow(Text.LineHeight);
        Widgets.Label(rectDivider3.NewCol(width), label2);
        Widgets.DrawBoxSolid(rectDivider3, oldColor);
    }

    private void ColorPalette(
        ref RectDivider layout,
        ref Color color)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            const int columnCount = 10; // 10 colors per row
            const float rowHeight = 22f + 4f + 2f; // 22f for the color box, 4f for the margin, 2f for the divider
            var rowCount = (int)Math.Ceiling((float)_colors.Count / columnCount);
            var maxRows = _favoriteColor != null || _defaultColor != null ? 10 : 12; // More rows if no quick set buttons are added.
            var maxHeight = Math.Min(rowCount * rowHeight, maxRows * rowHeight);

            var rectDivider1 = layout.NewCol(columnCount * rowHeight + 16f, HorizontalJustification.Right);
            var rectDivider3 = rectDivider1.NewRow(maxHeight);

            var outRect = new Rect(rectDivider3.Rect.x, rectDivider3.Rect.y, rectDivider3.Rect.width, rectDivider3.Rect.height);
            var viewRect = rectDivider3.CreateViewRect(rowCount, rowHeight - 4f);
            Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);

            Widgets.ColorSelector(viewRect, ref color, _colors, out _);
            Widgets.EndScrollView();

            if (_favoriteColor != null && _defaultColor != null)
            {
                var rectDivider2 = rectDivider1.NewRow(UIUtility.RegularButtonHeight);

                var rect = rectDivider2.Rect;

                string label1 = "PawnEditor.DefaultColor".Translate();
                string label2 = "PawnEditor.FavoriteColor".Translate();
                var width1 = Text.CalcSize(label1).x;
                var width2 = Text.CalcSize(label2).x;
                if (_defaultColor != null)
                {
                    if (Widgets.ButtonText(rect.TakeLeftPart(width1 + 32f), label1)) color = _defaultColor.Value;

                    rect.xMin += 8f;
                }

                if (_favoriteColor != null)
                    if (Widgets.ButtonText(rect.TakeLeftPart(width2 + 32f), label2))
                        color = _favoriteColor.Value;
            }
        }
    }

    private void ColorTextFields(ref RectDivider layout)
    {
        var rectDivider1 = layout.NewCol(layout.Rect.width / 2);

        var label1 = "Hue".Translate().CapitalizeFirst();
        var label2 = "Saturation".Translate().CapitalizeFirst();
        var label3 = "PawnEditor.Hex".Translate().CapitalizeFirst();
        const string controlName = "ColorTextfields";
        var focusedPrev = _previousFocusedControlName != null && _previousFocusedControlName.StartsWith(controlName);
        var focusedNow = GUI.GetNameOfFocusedControl().StartsWith(controlName);

        var width = Mathf.Max(40, label1.GetWidthCached(), label2.GetWidthCached(), label3.GetWidthCached());

        Color.RGBToHSV(_selectedColor, out var hue, out var sat, out var val);
        var rectDivider2 = rectDivider1.NewRow(Text.LineHeight);
        Widgets.Label(rectDivider2.NewCol(width), label1);
        var hueText = Widgets.ToIntegerRange(hue, 0, 360).ToString();
        var newHueText = Widgets.DelayedTextField(rectDivider2, hueText, ref _textfieldBuffers[0], _previousFocusedControlName, controlName + "_hue");
        if (hueText != newHueText && int.TryParse(newHueText, out var newHue)) _textfieldColorBuffer = Color.HSVToRGB(newHue / 360f, sat, val);

        var rectDivider3 = rectDivider1.NewRow(Text.LineHeight);
        Widgets.Label(rectDivider3.NewCol(width), label2);
        var satText = Widgets.ToIntegerRange(sat, 0, 100).ToString();
        var newSatText = Widgets.DelayedTextField(rectDivider3, satText, ref _textfieldBuffers[1], _previousFocusedControlName, controlName + "_sat");
        if (satText != newSatText && int.TryParse(newHueText, out var newSat)) _textfieldColorBuffer = Color.HSVToRGB(hue, newSat / 100f, val);

        var rectDivider4 = rectDivider1.NewRow(Text.LineHeight);
        Widgets.Label(rectDivider4.NewCol(width), label3);
        var hex = ColorUtility.ToHtmlStringRGB(_selectedColor);
        var newHex = Widgets.DelayedTextField(rectDivider4, hex, ref _textfieldBuffers[2], _previousFocusedControlName, controlName + "_hex");
        if (hex != newHex) ColorUtility.TryParseHtmlString(newHex, out _textfieldColorBuffer);
        if (focusedPrev)
        {
            if (!focusedNow) _selectedColor = _textfieldColorBuffer;
        }
        else _textfieldColorBuffer = _selectedColor;

        var rectDivider5 = rectDivider1.NewRow(UIUtility.RegularButtonHeight);
        if (Widgets.ButtonText(rectDivider5, "Random".Translate())) _selectedColor = Random.ColorHSV();
    }

    private void Accept()
    {
        _onSelect?.Invoke(_selectedColor);
        Close();
    }
}
