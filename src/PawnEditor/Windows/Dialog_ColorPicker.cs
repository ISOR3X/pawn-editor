using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Random = UnityEngine.Random;

namespace PawnEditor;

[HotSwappable]
public class Dialog_ColorPicker : Window
{
    private const float CellSize = 22f + CellPadding; // 22f for the color box, 4f for the margin.
    private const float CellPadding = 4f;
    private const float CellGap = 2f;
    private const int ColumnCount = 10; // 10 colors per row.
    private readonly List<Color> _colors;
    private readonly Color _oldColor;
    private readonly Action<Color> _onSelect;

    private readonly float _singleCharWidth = "X".GetWidthCached() + UIUtility.LabelPadding;

    private readonly Dictionary<string, Color>?
        _specialColors; // A dictionary of special colors to display below the color palette.

    private readonly string?[] _textfieldBuffers = new string[4];
    private bool _doHSV = true;

    private bool _hsvColorWheelDragging;
    private string? _lastFocusedSlider;
    private string? _previousFocusedControlName;
    private Vector2 _scrollPosition;
    private Color _selectedColor;

    /// <notes>
    ///     BUG - When entering a value in the RGB fields, then clicking outside, the dragging of the color wheel does not
    ///     work. Just clicking a color works fine.
    ///     Clicking outside the wheel again allows dragging again.
    ///     BUG - Trying to slide the Hue slider does not work the first time.
    /// </notes>
    public Dialog_ColorPicker(Action<Color> onSelect, Color oldColor, List<Color>? colors = null,
        Dictionary<string, Color>? specialColors = null)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;

        _specialColors = specialColors;
        if (_specialColors != null && !_specialColors.TryGetValue("Old", out _)) _specialColors["Old"] = oldColor;

        if (colors == null)
        {
            _colors = [];
            DefDatabase<ColorDef>.AllDefsListForReading.ForEach(c => _colors.Add(c.color));
        }
        else
        {
            _colors = colors;
        }
    }


    public override Vector2 InitialSize => new(600f, 450f);

    public override void DoWindowContents(Rect inRect)
    {
        using (TextBlock.Default())
        {
            DoHeader(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium)));
            DoFooter(inRect.TakeBottomPart(UIUtility.BottomButtonSize.y));

            inRect = inRect.ContractedBy(0f, 8f);
            inRect.yMax -= 8f; // Extra bottom clearance for the footer buttons.
            inRect.SplitVerticallyWithMargin(out var leftRect, out var rightRect, out _, 64f,
                rightWidth: (CellSize + CellGap) * ColumnCount);

            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
            {
                ColorReadback(rightRect.TakeBottomPart(CellSize * 2 + CellGap), ref _selectedColor, _oldColor);
                ColorPalette(rightRect, ref _selectedColor);


                leftRect.SplitHorizontallyWithMargin(out var hsvWidgetsRect, out var fieldsRect, out _, 16f, 150f);
                ColorTextFields(fieldsRect);

                hsvWidgetsRect.SplitVerticallyWithMargin(out var hsvRect, out var widgetsRect, out _, 16,
                    rightWidth: Verse.Widgets.InfoCardButtonSize);
                var min = Mathf.Min(hsvRect.width, hsvRect.height);
                hsvRect = hsvRect with { width = min, height = min };
                hsvRect.x += _singleCharWidth;
                Verse.Widgets.HSVColorWheel(hsvRect, ref _selectedColor, ref _hsvColorWheelDragging, 1f);
                DoWidgets(widgetsRect);
            }

            if (Event.current.type != EventType.Layout)
                return;
            _previousFocusedControlName = GUI.GetNameOfFocusedControl();
        }
    }

    private static void DoHeader(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium))
        {
            var label = "ChooseAColor".Translate().CapitalizeFirst();
            Verse.Widgets.Label(inRect, label);
        }
    }

    private void DoFooter(Rect inRect)
    {
        if (Verse.Widgets.ButtonText(inRect.TakeLeftPart(UIUtility.BottomButtonSize.x), "Cancel".Translate()))
            Close();
        if (Verse.Widgets.ButtonText(inRect.TakeRightPart(UIUtility.BottomButtonSize.x), "Accept".Translate()))
            Accept();
    }

    private void DoWidgets(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        if (listing.ButtonImage(TexPawnEditor.Reroll, CopyPasteUI.CopyPasteIconHeight, CopyPasteUI.CopyPasteIconHeight))
        {
            _selectedColor = Random.ColorHSV();
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        if (listing.ButtonImage(TexButton.Copy, CopyPasteUI.CopyPasteIconHeight, CopyPasteUI.CopyPasteIconHeight))
        {
            var hex = ColorUtility.ToHtmlStringRGB(_selectedColor);
            GUIUtility.systemCopyBuffer = hex;
            Messages.Message($"Copied HEX color {hex} to clipboard", MessageTypeDefOf.SilentInput);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }

        if (listing.ButtonImage(TexButton.Paste, CopyPasteUI.CopyPasteIconHeight, CopyPasteUI.CopyPasteIconHeight))
        {
            var clipBoard = GUIUtility.systemCopyBuffer;
            clipBoard = clipBoard.Insert(0, "#");
            if (ColorUtility.TryParseHtmlString(clipBoard, out var color))
            {
                _selectedColor = color;
                Messages.Message("Succesfully pasted HEX color from clipboard", MessageTypeDefOf.SilentInput);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            else
            {
                Messages.Message(
                    $"Failed pasting clipboard value ({clipBoard}) as color. The value should be in the #RRGGBB format.",
                    MessageTypeDefOf.SilentInput);
                SoundDefOf.Designate_Failed.PlayOneShotOnCamera();
            }
        }

        listing.End();
    }

    private static void ColorReadback(Rect inRect, ref Color color, Color oldColor)
    {
        var currentLabel = "CurrentColor".Translate().CapitalizeFirst();
        var oldLabel = "OldColor".Translate().CapitalizeFirst();
        var width = Mathf.Max(100f, currentLabel.GetWidthCached(), oldLabel.GetWidthCached());
        inRect.SplitHorizontallyEqual(out var currentRect, out var oldRect, CellGap);
        Verse.Widgets.Label(currentRect.TakeLeftPart(width), currentLabel);
        Verse.Widgets.DrawBoxSolid(currentRect, color);
        oldRect = oldRect.CenteredVertically(CellSize - CellPadding);
        if (Verse.Widgets.ButtonInvisible(oldRect)) color = oldColor;
        Verse.Widgets.Label(oldRect.TakeLeftPart(width), oldLabel);
        Verse.Widgets.DrawBoxSolid(oldRect.CenteredVertically(CellSize - CellPadding), oldColor);
    }

    private void ColorPalette(Rect inRect, ref Color color)
    {
        var rectDivider = new RectDivider(inRect, inRect.GetHashCode());
        var rowCount = (int)Math.Ceiling((double)_colors.Count / ColumnCount);
        var viewRectDivider = rectDivider.CreateViewRect(rowCount, CellSize);
        var viewRect = viewRectDivider.Rect;
        if (_specialColors is { Count: > 0 }) viewRect.height += _specialColors.Count * CellSize + CellSize / 2;

        Verse.Widgets.BeginScrollView(rectDivider.Rect, ref _scrollPosition, viewRect);
        Verse.Widgets.ColorSelector(viewRect, ref color, _colors, out var height);

        viewRect.yMin +=
            height + CellSize /
            2; // The gap between the regular colors and the special colors is the size of exactly one cell.
        if (_specialColors is { Count: > 0 })
            for (var i = 0; i < _specialColors.Count; i++)
            {
                if (i % 2 != 0) continue; // Skip odd pairs, these should already be drawn.

                var kvp = _specialColors.ElementAt(i);
                var rowRect = viewRect.TakeTopPart(CellSize);
                var leftRect = rowRect.LeftHalf();

                Verse.Widgets.ColorBox(leftRect.TakeLeftPart(CellSize), ref color, kvp.Value);
                leftRect.xMin += CellPadding;
                Verse.Widgets.Label(leftRect, kvp.Key);


                if (i + 1 < _specialColors.Count)
                {
                    var kvp2 = _specialColors.ElementAt(i + 1);
                    var rightRect = leftRect with
                    {
                        x = leftRect.x + (CellSize + CellGap) * 3 - CellGap
                    }; // Align right kvp with the 4th cell.

                    Verse.Widgets.ColorBox(rightRect.TakeLeftPart(CellSize), ref color, kvp2.Value);
                    rightRect.xMin += CellPadding;
                    Verse.Widgets.Label(rightRect, kvp2.Key);
                }
            }

        Verse.Widgets.EndScrollView();
    }

    private void ColorTextFields(Rect inRect)
    {
        var rect1 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        inRect.yMin += CellPadding;
        var rect2 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        inRect.yMin += CellPadding;
        var rect3 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        inRect.yMin += CellPadding;
        var rect4 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        var hexRect = rect4.RightHalf();
        var buttonRect = rect4.LeftHalf();

        if (_doHSV)
        {
            Widgets.GradientSlider_LabeledWithField(rect1, Verse.Widgets.ColorComponents.Hue, ref _selectedColor,
                ref _textfieldBuffers[0], ref _lastFocusedSlider,
                _previousFocusedControlName);
            Widgets.GradientSlider_LabeledWithField(rect2, Verse.Widgets.ColorComponents.Sat, ref _selectedColor,
                ref _textfieldBuffers[1], ref _lastFocusedSlider,
                _previousFocusedControlName);
            Widgets.GradientSlider_LabeledWithField(rect3, Verse.Widgets.ColorComponents.Value, ref _selectedColor,
                ref _textfieldBuffers[2], ref _lastFocusedSlider,
                _previousFocusedControlName);
        }
        else
        {
            Widgets.GradientSlider_LabeledWithField(rect1, Verse.Widgets.ColorComponents.Red, ref _selectedColor,
                ref _textfieldBuffers[0], ref _lastFocusedSlider,
                _previousFocusedControlName);
            Widgets.GradientSlider_LabeledWithField(rect2, Verse.Widgets.ColorComponents.Green, ref _selectedColor,
                ref _textfieldBuffers[1], ref _lastFocusedSlider,
                _previousFocusedControlName);
            Widgets.GradientSlider_LabeledWithField(rect3, Verse.Widgets.ColorComponents.Blue, ref _selectedColor,
                ref _textfieldBuffers[2], ref _lastFocusedSlider,
                _previousFocusedControlName);
        }

        using (new TextBlock(GameFont.Tiny))
        {
            buttonRect = buttonRect.TakeLeftPart("XXX".GetWidthCached() + UIUtility.ButtonPadding / 2);
            if (Mouse.IsOver(buttonRect))
                TooltipHandler.TipRegion(buttonRect, "Switch between RGB and HSV color modes");
            if (Verse.Widgets.ButtonText(buttonRect, _doHSV ? "RGB" : "HSV"))
            {
                _doHSV = !_doHSV;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        Verse.Widgets.Label(hexRect.TakeLeftPart(_singleCharWidth), "#".Colorize(ColoredText.SubtleGrayColor));
        _selectedColor = Widgets.DelayedHexField(hexRect, _selectedColor, ref _textfieldBuffers[3],
            _previousFocusedControlName);
    }

    private void Accept()
    {
        _onSelect?.Invoke(_selectedColor);
        Close();
    }
}