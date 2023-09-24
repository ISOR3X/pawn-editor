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
        var rectDivider1 = layout.NewRow(Text.LineHeightOf(GameFont.Small) * 2 + 26f, VerticalJustification.Bottom);

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
            float height = _colors.Count / 10 * 24;
            var rectDivider1 = layout.NewCol(layout.Rect.width / 2, HorizontalJustification.Right);
            var viewRect = rectDivider1.Rect;
            var outRect = new Rect(rectDivider1.Rect.x, rectDivider1.Rect.y, rectDivider1.Rect.width + 16f, rectDivider1.Rect.height);
            viewRect.height = height;
            Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
            Widgets.ColorSelector(rectDivider1, ref color, _colors, out height);
            Widgets.EndScrollView();
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

        //        var rectAggregator = new RectAggregator(new(rectDivider1.Rect.position, new(125f, 0f)), 195906069);
//        if (Widgets.ColorTextfields(ref rectAggregator, ref _selectedColor, ref _textfieldBuffers, ref _textfieldColorBuffer,
//                _previousFocusedControlName, "colorTextfields")) { }
//
//       var size = rectAggregator.Rect.size;
//
//        rectDivider1.NewRow(size.y);
        var rectDivider5 = rectDivider1.NewRow(UIUtility.RegularButtonHeight);
        if (Widgets.ButtonText(rectDivider5, "Random".Translate())) _selectedColor = Random.ColorHSV();
    }

    private void Accept()
    {
        _onSelect?.Invoke(_selectedColor);
        Close();
    }
}
