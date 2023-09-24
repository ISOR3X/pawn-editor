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
    private readonly Action<Color> _onSelect;

    private Color _selectedColor;
    private readonly Color _oldColor;
    private readonly List<Color> _colors;
    public override Vector2 InitialSize => new (600f, 450f);

    private readonly string[] _textfieldBuffers = new string[6];

    // private Color _textfieldColorBuffer;
    private string _previousFocusedControlName;
    private bool _hsvColorWheelDragging;
    private Vector2 _scrollPosition;

    private float _hue;
    private float _sat;
    private string _hex;

    private string Hex
    {
        get
        {
            ColorUtility.ToHtmlStringRGB(_selectedColor);
            return _hex;
        }
        set
        {
            _hex = value;
            ColorUtility.TryParseHtmlString(value, out _selectedColor);
        }
    }

    public Dialog_ColorPicker(Action<Color> onSelect, ColorType colorType, Color oldColor)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;

        _colors = DefDatabase<ColorDef>.AllDefs.Where(cd => cd.colorType == colorType)
            .Select(cd => cd.color).ToList();
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


    public override void DoWindowContents(Rect inRect)
    {
        using (TextBlock.Default())
        {
            RectDivider layout = new RectDivider(inRect, 91185);
            HeaderRow(ref layout);
            BottomButtons(ref layout);
            var tempColor = _selectedColor;
            ColorPalette(ref layout, ref _selectedColor);
            if (tempColor != _selectedColor)
            {
                Color.RGBToHSV(_selectedColor, out var h, out var s, out var v);
                Log.Message($"Hue: {h}, Sat: {s}, Val: {v}");
            }

            ColorReadback(ref layout, _selectedColor, _oldColor);
            ColorTextFields(ref layout);
            RectDivider rectDivider = layout.NewRow(layout.Rect.width);
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
            TaggedString taggedString = "ChooseAColor".Translate().CapitalizeFirst();
            RectDivider rectDivider = layout.NewRow(Text.CalcHeight(taggedString, layout.Rect.width));
            Widgets.Label(rectDivider, taggedString);
        }
    }

    private void BottomButtons(ref RectDivider layout)
    {
        RectDivider rectDivider = layout.NewRow(UIUtility.BottomButtonSize.y, VerticalJustification.Bottom);
        if (Widgets.ButtonText(rectDivider.NewCol(UIUtility.BottomButtonSize.x), "Cancel".Translate()))
            Close();
        if (Widgets.ButtonText(rectDivider.NewCol(UIUtility.BottomButtonSize.x, HorizontalJustification.Right),
                "Accept".Translate()))
            Accept();
    }

    private static void ColorReadback(ref RectDivider layout, Color color, Color oldColor)
    {
        RectDivider rectDivider1 = layout.NewRow(Text.LineHeightOf(GameFont.Small) * 2 + 26f, VerticalJustification.Bottom);

        TaggedString label1 = "CurrentColor".Translate().CapitalizeFirst();
        TaggedString label2 = "OldColor".Translate().CapitalizeFirst();

        float width = Mathf.Max(100f, label1.GetWidthCached(), label2.GetWidthCached());

        RectDivider rectDivider2 = rectDivider1.NewRow(Text.LineHeight);
        Widgets.Label(rectDivider2.NewCol(width), label1);
        Widgets.DrawBoxSolid(rectDivider2, color);
        RectDivider rectDivider3 = rectDivider1.NewRow(Text.LineHeight);
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
            RectDivider rectDivider1 = layout.NewCol(layout.Rect.width / 2, HorizontalJustification.Right);
            Rect viewRect = rectDivider1.Rect;
            Rect outRect = new Rect(rectDivider1.Rect.x, rectDivider1.Rect.y, rectDivider1.Rect.width + 16f, rectDivider1.Rect.height);
            viewRect.height = height;
            Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
            Widgets.ColorSelector(rectDivider1, ref color, _colors, out height);
            Widgets.EndScrollView();
        }
    }

    private void ColorTextFields(ref RectDivider layout)
    {
        RectDivider rectDivider1 = layout.NewCol(layout.Rect.width / 2);

        TaggedString label1 = "Hue".Translate().CapitalizeFirst();
        TaggedString label2 = "Saturation".Translate().CapitalizeFirst();
        TaggedString label3 = "PawnEditor.Hex".Translate().CapitalizeFirst();

        float width = Mathf.Max(40, label1.GetWidthCached(), label2.GetWidthCached(), label3.GetWidthCached());

        // RectDivider rectDivider2 = rectDivider1.NewRow(Text.LineHeight);
        // Widgets.Label(rectDivider2.NewCol(width), label1);
        // Widgets.TextFieldNumeric(rectDivider2, ref _hue, ref _textfieldBuffers[0]);
        // RectDivider rectDivider3 = rectDivider1.NewRow(Text.LineHeight);
        // Widgets.Label(rectDivider3.NewCol(width), label2);
        // // Widgets.TextFieldNumeric(rectDivider3, ref _sat, ref _textfieldBuffers[1]);
        // RectDivider rectDivider4 = rectDivider1.NewRow(Text.LineHeight);
        // Widgets.Label(rectDivider4.NewCol(width), label3);
        // Widgets.DelayedTextField(rectDivider4, "", ref _textfieldBuffers[2], _previousFocusedControlName);
        RectDivider rectDivider5 = rectDivider1.NewRow(UIUtility.RegularButtonHeight);
        if (Widgets.ButtonText(rectDivider5, "Random".Translate()))
        {
            _selectedColor = Random.ColorHSV();
            // Hex = ColorUtility.ToHtmlStringRGB(_selectedColor);
            // Color.RGBToHSV(_selectedColor, out _hue, out _sat, out _);
        }
    }

    private void Accept()
    {
        if (_onSelect != null)
        {
            _onSelect(_selectedColor);
        }

        Close();
    }
}