using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Random = UnityEngine.Random;

namespace PawnEditor;

[HotSwappable]
public class Dialog_ColorPicker : Window
{
    private Color _selectedColor;
    private readonly List<Color> _colors;
    private readonly Dictionary<string, Color> specialColors; // A dictionary of special colors to display below the color palette.
    private readonly Color _oldColor;
    private readonly Action<Color> _onSelect;

    private readonly float singleCharWidth = "X".GetWidthCached() + UIUtility.LabelPadding;
    private readonly string[] _textfieldBuffers = new string[4];

    private bool _hsvColorWheelDragging;
    private string _previousFocusedControlName;
    private string lastFocusedSlider;
    private Vector2 _scrollPosition;
    private bool doHSV = true;

    public const float cellSize = 22f + cellPadding; // 22f for the color box, 4f for the margin.
    public const float cellPadding = 4f;
    public const float cellGap = 2f;
    const int columnCount = 10; // 10 colors per row.

    /// <notes>
    /// BUG - When entering a value in the RGB fields, then clicking outside, the dragging of the color wheel does not work. Just clicking a color works fine.
    /// Clicking outside the wheel again allows dragging again.
    /// </notes>
    public Dialog_ColorPicker(Action<Color> onSelect, ColorType colorType, Color oldColor, Dictionary<string, Color> specialColors = null)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
        // draggable = true;

        this.specialColors = specialColors;

        _colors = DefDatabase<ColorDef>.AllDefs.Where(cd => cd.colorType == colorType)
            .Select(cd => cd.color)
            .ToList();
    }

    public Dialog_ColorPicker(Action<Color> onSelect, List<Color> colors, Color oldColor, Dictionary<string, Color> specialColors = null)
    {
        _onSelect = onSelect;
        _oldColor = oldColor;
        _selectedColor = oldColor;

        closeOnAccept = false;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
        // draggable = true;

        this.specialColors = specialColors;

        _colors = colors;
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
            inRect.SplitVerticallyWithMargin(out Rect leftRect, out Rect rightRect, out float _, 64f, rightWidth: (cellSize + cellGap) * columnCount);

            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
            {
                ColorReadback(rightRect.TakeBottomPart(cellSize * 2 + cellGap), ref _selectedColor, _oldColor);
                ColorPalette(rightRect, ref _selectedColor);


                leftRect.SplitHorizontallyWithMargin(out Rect hsvWidgetsRect, out Rect fieldsRect, out float _, 16f, topHeight: 150f);
                ColorTextFields(fieldsRect);

                hsvWidgetsRect.SplitVerticallyWithMargin(out Rect hsvRect, out Rect widgetsRect, out float _, 16, rightWidth: Widgets.InfoCardButtonSize);
                var min = Mathf.Min(hsvRect.width, hsvRect.height);
                hsvRect = hsvRect with { width = min, height = min };
                hsvRect.x += singleCharWidth;
                Widgets.HSVColorWheel(hsvRect, ref _selectedColor, ref _hsvColorWheelDragging, 1f);
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
            Widgets.Label(inRect, label);
        }
    }

    private void DoFooter(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart((UIUtility.BottomButtonSize.x)), "Cancel".Translate()))
            Close();
        if (Widgets.ButtonText(inRect.TakeRightPart((UIUtility.BottomButtonSize.x)), "Accept".Translate()))
            Accept();
    }

    private void DoWidgets(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        if (listing.ButtonImage(TexPawnEditor.Randomize, CopyPasteUI.CopyPasteIconHeight, CopyPasteUI.CopyPasteIconHeight))
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
            string clipBoard = GUIUtility.systemCopyBuffer;
            clipBoard = clipBoard.Insert(0, "#");
            if (ColorUtility.TryParseHtmlString(clipBoard, out Color color))
            {
                _selectedColor = color;
                Messages.Message("Succesfully pasted HEX color from clipboard", MessageTypeDefOf.SilentInput);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            else
            {
                Messages.Message($"Failed pasting clipboard value ({clipBoard}) as color. The value should be in the #RRGGBB format.", MessageTypeDefOf.SilentInput);
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
        inRect.SplitHorizontallyEqual(out Rect currentRect, out Rect oldRect, cellGap);
        Widgets.Label(currentRect.TakeLeftPart(width), currentLabel);
        Widgets.DrawBoxSolid(currentRect, color);
        oldRect = oldRect.HorizontalCenterPart(cellSize - cellPadding);
        if (Widgets.ButtonInvisible(oldRect)) color = oldColor;
        Widgets.Label(oldRect.TakeLeftPart(width), oldLabel);
        Widgets.DrawBoxSolid(oldRect.HorizontalCenterPart(cellSize - cellPadding), oldColor);
    }

    private void ColorPalette(Rect inRect, ref Color color)
    {
        var rectDivider = new RectDivider(inRect, inRect.GetHashCode());
        var rowCount = (int)Math.Ceiling((double)_colors.Count / columnCount);
        var viewRectDivider = rectDivider.CreateViewRect(rowCount, cellSize);
        var viewRect = viewRectDivider.Rect;
        if (!specialColors.NullOrEmpty()) viewRect.height += ((specialColors.Count * cellSize) + (cellSize / 2));

        Widgets.BeginScrollView(rectDivider.Rect, ref _scrollPosition, viewRect);
        Widgets.ColorSelector(viewRect, ref color, _colors, out float height);

        viewRect.yMin += (height + (cellSize / 2)); // The gap between the regular colors and the special colors is the size of exactly one cell.
        if (!specialColors.NullOrEmpty())
        {
            for (int i = 0; i < specialColors.Count; i++)
            {
                if (i % 2 != 0) continue; // Skip odd pairs, these should already be drawn.

                var kvp = specialColors.ElementAt(i);
                Rect rowRect = viewRect.TakeTopPart(cellSize);
                Rect leftRect = rowRect.LeftHalf();

                Widgets.ColorBox(leftRect.TakeLeftPart(cellSize), ref color, kvp.Value);
                leftRect.xMin += cellPadding;
                Widgets.Label(leftRect, kvp.Key);


                if (i + 1 < specialColors.Count)
                {
                    var kvp2 = specialColors.ElementAt(i + 1);
                    Rect rightRect = leftRect with { x = leftRect.x + (cellSize + cellGap) * 3 - cellGap }; // Align right kvp with the 4th cell.

                    Widgets.ColorBox(rightRect.TakeLeftPart(cellSize), ref color, kvp2.Value);
                    rightRect.xMin += cellPadding;
                    Widgets.Label(rightRect, kvp2.Key);
                }
            }
        }

        Widgets.EndScrollView();
    }

    private void ColorTextFields(Rect inRect)
    {
        var rect1 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        inRect.yMin += cellPadding;
        var rect2 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        inRect.yMin += cellPadding;
        var rect3 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        inRect.yMin += cellPadding;
        var rect4 = inRect.TakeTopPart(UIUtility.ButtonHeight);
        var hexRect = rect4.RightHalf();
        var buttonRect = rect4.LeftHalf();

        if (doHSV)
        {
            UIComponents.GradientSlider_LabeledWithField(rect1, Widgets.ColorComponents.Hue, ref _selectedColor, ref _textfieldBuffers[0], ref lastFocusedSlider,
                _previousFocusedControlName);
            UIComponents.GradientSlider_LabeledWithField(rect2, Widgets.ColorComponents.Sat, ref _selectedColor, ref _textfieldBuffers[1], ref lastFocusedSlider,
                _previousFocusedControlName);
            UIComponents.GradientSlider_LabeledWithField(rect3, Widgets.ColorComponents.Value, ref _selectedColor, ref _textfieldBuffers[2], ref lastFocusedSlider,
                _previousFocusedControlName);
        }
        else
        {
            UIComponents.GradientSlider_LabeledWithField(rect1, Widgets.ColorComponents.Red, ref _selectedColor, ref _textfieldBuffers[0], ref lastFocusedSlider,
                _previousFocusedControlName);
            UIComponents.GradientSlider_LabeledWithField(rect2, Widgets.ColorComponents.Green, ref _selectedColor, ref _textfieldBuffers[1], ref lastFocusedSlider,
                _previousFocusedControlName);
            UIComponents.GradientSlider_LabeledWithField(rect3, Widgets.ColorComponents.Blue, ref _selectedColor, ref _textfieldBuffers[2], ref lastFocusedSlider,
                _previousFocusedControlName);
        }

        using (new TextBlock(GameFont.Tiny))
        {
            buttonRect = buttonRect.TakeLeftPart("XXX".GetWidthCached() + UIUtility.ButtonPadding / 2);
            if (Mouse.IsOver(buttonRect)) TooltipHandler.TipRegion(buttonRect, "Switch between RGB and HSV color modes");
            if (Widgets.ButtonText(buttonRect, doHSV ? "RGB" : "HSV"))
            {
                doHSV = !doHSV;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        Widgets.Label(hexRect.TakeLeftPart(singleCharWidth), "#".Colorize(ColoredText.SubtleGrayColor));
        _selectedColor = UIComponents.DelayedHexField(hexRect, _selectedColor, ref _textfieldBuffers[3], _previousFocusedControlName);
    }

    private void Accept()
    {
        _onSelect?.Invoke(_selectedColor);
        Close();
    }
}