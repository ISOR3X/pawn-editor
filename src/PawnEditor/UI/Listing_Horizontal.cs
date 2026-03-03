using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public class Listing_Horizontal
{
    private const float DefaultRowHeight = 30f;
    private const float LabelPadding = 18f;

    private readonly int ColumnCount;
    public Vector2 Spacing = new Vector2(8f, 8f);

    private Rect ListingRect;
    public float totalHeight;

    private readonly List<ListingCell> _cells = new();
    private readonly List<Rect> _cachedRects = new();
    private List<Rect> Rects = new();

    private int _cellIndex;
    private int _rowWidth;

    private float _maxLabelWidth;

    private bool dirty;


    public Listing_Horizontal(int columnCount = 12)
    {
        ColumnCount = columnCount;
    }

    public void Begin(Rect inRect)
    {
        Widgets.BeginGroup(inRect);
        Text.Anchor = TextAnchor.MiddleLeft;

        if (dirty) ClearCache();
        ListingRect = inRect.AtZero();
        _cellIndex = 0;
        _rowWidth = 0;
    }

    public void End()
    {
        if (!Rects.NullOrEmpty() && Rects.Count != _cellIndex) dirty = true; // Reset if we have more cells than rects.

        if (Rects.NullOrEmpty() && !dirty)
        {
            if (_rowWidth > 0) CacheCurrentRow(); // Cache final row.
            Rects = _cachedRects.ListFullCopy();
        }

        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndGroup();
    }

    public void ClearCache()
    {
        _cells.Clear();
        _cachedRects.Clear();
        Rects.Clear();
        _rowWidth = 0;
        totalHeight = 0;
        _maxLabelWidth = 0;
        dirty = false;
    }

    public Rect GetRect(int relativeWidth = -1, float height = DefaultRowHeight, bool grow = false)
    {
        int trueWidth = relativeWidth == -1 ? ColumnCount : relativeWidth;
        if (!Rects.NullOrEmpty())
        {
            _cellIndex++;
            if (_cellIndex <= Rects.Count)
            {
                var output = Rects[_cellIndex - 1];
                if (height > output.height) dirty = true;
                else return output with { height = height };
            }
            else dirty = true;
        }

        if (_rowWidth + trueWidth > ColumnCount)
        {
            CacheCurrentRow();
            _rowWidth = 0;
            _cells.Clear();
        }

        _rowWidth += trueWidth;
        _cells.Add(new ListingCell(trueWidth, grow, height));

        return new Rect(0, 0, ListingRect.width * ((float)relativeWidth / ColumnCount) - Spacing.x, height);
    }

    public void NewRow()
    {
        _rowWidth = ColumnCount;
        // _rowWidth = 0;
        // _cells.Clear();
    }

    public void CacheCurrentRow()
    {
        float availableSpace = ListingRect.width - (Spacing.x * (_cells.Count - 1));
        int leftOverWidthRel = ColumnCount - _cells.Sum(c => c.RelativeWidth);
        int sumGrow = _cells.Sum(c => c.Grow ? 1 : 0);
        float rowMaxHeight = _cells.Max(c => c.Height);

        Rect rowRect = ListingRect.TakeTopPart(rowMaxHeight);
        ListingRect.yMin += Spacing.y;
        totalHeight += rowMaxHeight + Spacing.y;

        foreach (var cell in _cells)
        {
            float widthRel = cell.RelativeWidth;
            widthRel += cell.Grow ? leftOverWidthRel > 0 ? 1 / sumGrow * leftOverWidthRel : 0 : 0;
            _cachedRects.Add(rowRect.TakeLeftPart(widthRel / ColumnCount * availableSpace));
            rowRect.xMin += Spacing.x;
        }
    }

    public Rect RectLabeled(string label, int width = -1, float height = DefaultRowHeight, string tooltip = null, bool grow = false)
    {
        Rect r = GetRect(width, height, grow);

        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(r, (TipSignal)tooltip);
        }

        if (Event.current.type == EventType.Layout)
        {
            _maxLabelWidth = Mathf.Max(_maxLabelWidth, Text.CalcSize(label).x);
        }

        Rect labelRect = r.TakeLeftPart(_maxLabelWidth + LabelPadding);
        Widgets.Label(labelRect.TopPartPixels(DefaultRowHeight), label);

        return r;
    }

    public bool ButtonTextLabeled(string label, string buttonLabel, int width = -1, string tooltip = null, bool grow = false)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public bool ButtonText(string buttonLabel, int width = -1, string tooltip = null, bool grow = false)
    {
        Rect r = GetRect(width, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public float SliderLabeled(string label, float val, float min, float max, int width = -1, string tooltip = null, bool grow = false)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        return Widgets.HorizontalSlider(r, val, min, max, true);
    }

    public float SliderLabeled(string label, float val, float min, float max, string valLabel, string minLabel, string maxLabel, int width = -1, string tooltip = null,
        bool grow = false)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        return Widgets.HorizontalSlider(r, val, min, max, true, valLabel, minLabel, maxLabel);
    }

    public void ColorPickerLabeled(string label, float height, ref Color color, Dictionary<string, Color> specialColors, List<Color> colors, Action<Color> onApply,
        out float selectorHeight,
        int width = -1,
        string tooltip = null, bool grow = false)
    {
        var oldColor = color;
        Rect r = RectLabeled(label, width, height, tooltip: tooltip, grow: grow);
        var availableColorsWithTransparent = colors.Append(new Color(0, 0, 0, 0f)).ToList(); // Little hack to add the colorpicker button to the end.

        if (r.width <= 0)
        {
            selectorHeight = height;
            return;
        }

        Widgets.ColorSelector(r, ref color, availableColorsWithTransparent, out var selectorHeight1, extraOnGUI: ((currentColor, rect) =>
        {
            if (currentColor.a != 0) return;
            if (Widgets.ButtonImage(rect.ExpandedBy(2f), Designator_Eyedropper.EyeDropperTex))
            {
                Find.WindowStack.Add(new Dialog_ColorPicker(onApply, oldColor, colors, specialColors));
            }
        }));

        selectorHeight = selectorHeight1;
    }

    public void CheckboxLabeled(string label, ref bool val, int width = -1, string tooltip = null, bool grow = false)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        Widgets.Checkbox(new Vector2(r.x, r.y), ref val);
    }

    private struct ListingCell
    {
        public readonly float Height;
        public readonly int RelativeWidth;
        public readonly bool Grow;

        public ListingCell(int relativeWidth, bool grow, float height = DefaultRowHeight)
        {
            RelativeWidth = relativeWidth;
            Grow = grow;
            Height = height;
        }
    }
}