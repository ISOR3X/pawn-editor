using System;
using System.Collections.Generic;
using System.Globalization;
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

    private string[] _textfieldBuffers = new string[6];
    private bool _hsvColorWheelDragging;
    private string _previousFocusedControlName;
    private Vector2 _scrollPosition;

    private Color _selectedColor;

    private Color _textfieldColorBuffer;
    private string _hexfieldStringBuffer;

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
        layout.currentRect.y -= 250;
        var rectDivider1 = layout.NewCol(layout.Rect.width / 2);
        var rect = rectDivider1.Rect;
        RectAggregator aggregator = new RectAggregator(rect, layout.GetHashCode());
        const string controlName = "ColorTextfields";
        bool hue = Widgets.ColorTextfields(ref aggregator, ref _selectedColor, ref _textfieldBuffers, ref _textfieldColorBuffer, _previousFocusedControlName, controlName + "_hue", Widgets.ColorComponents.Hue, Widgets.ColorComponents.Hue);
        if (hue)
        {
            Color.RGBToHSV(_selectedColor, out var H, out var S, out var _);
            _selectedColor = Color.HSVToRGB(H, S, 1f);
        }
        bool sat = Widgets.ColorTextfields(ref aggregator, ref _selectedColor, ref _textfieldBuffers, ref _textfieldColorBuffer, _previousFocusedControlName, controlName + "_sat", Widgets.ColorComponents.Sat, Widgets.ColorComponents.Sat);
        if (sat)
        {
            Color.RGBToHSV(_selectedColor, out var H, out var S, out var _);
            _selectedColor = Color.HSVToRGB(H, S, 1f);
        }
        var label = "PawnEditor.Hex".Translate().CapitalizeFirst();
        var hexRect = new Rect(aggregator.Rect.x, aggregator.Rect.yMax + 4, label.GetWidthCached(), 30f);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(hexRect, label);
            var hexFieldRect = new Rect(hexRect.x + 50, hexRect.y, 74, 30f);
            if (_hexfieldStringBuffer.NullOrEmpty())
            {
                _hexfieldStringBuffer = ColorUtility.ToHtmlStringRGB(_selectedColor);
            }
            var newHex = Widgets.TextField(hexFieldRect, _hexfieldStringBuffer);
            if (_hexfieldStringBuffer != newHex && TryGetColorFromHex(newHex, out var tempColor))
            {
                _selectedColor = tempColor;
            }
            _hexfieldStringBuffer = newHex;
        }
        var randomRect = new Rect(hexRect.x, hexRect.yMax + 4, rectDivider1.currentRect.width, UIUtility.RegularButtonHeight);
        if (Widgets.ButtonText(randomRect, "Random".Translate())) 
            _selectedColor = Random.ColorHSV();
        layout.currentRect.y += 250;
    }

    public static bool TryGetColorFromHex(string hex, out Color color)
    {
        color = Color.white;
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }
        if (hex.Length != 6 && hex.Length != 8)
        {
            return false;
        }
        int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        int a = 255;
        if (hex.Length == 8)
        {
            a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
        }
        color = GenColor.FromBytes(r, g, b, a);
        return true;
    }

    private void Accept()
    {
        _onSelect?.Invoke(_selectedColor);
        Close();
    }
}
