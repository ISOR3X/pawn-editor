using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
[StaticConstructorOnStartup]
// [Obsolete("Use Listing_Standard or FlexLayout instead.")]
public class Listing_Horizontal
{
    private const float DefaultRowHeight = 30f;
    private const float LabelPadding = 18f;
    private readonly List<Rect> _cachedRects = [];

    private readonly List<ListingCell> _cells = [];

    private readonly int _columnCount;

    private int _cellIndex;

    private bool _dirty;

    private Rect _listingRect;

    private float _maxLabelWidth;
    private List<Rect> _rects = [];
    private int _rowWidth;
    public Vector2 Spacing = new(8f, 8f);
    public float TotalHeight;


    public Listing_Horizontal(int columnCount = 12)
    {
        _columnCount = columnCount;
    }

    public void Begin(Rect inRect)
    {
        Widgets.BeginGroup(inRect);
        Text.Anchor = TextAnchor.MiddleLeft;

        if (_dirty) ClearCache();
        _listingRect = inRect.AtZero();
        _cellIndex = 0;
        _rowWidth = 0;
    }

    public void End()
    {
        if (!_rects.NullOrEmpty() && _rects.Count != _cellIndex)
            _dirty = true; // Reset if we have more cells than rects.

        if (_rects.NullOrEmpty() && !_dirty)
        {
            if (_rowWidth > 0) CacheCurrentRow(); // Cache final row.
            _rects = _cachedRects.ListFullCopy();
        }

        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndGroup();
    }

    public void ClearCache()
    {
        _cells.Clear();
        _cachedRects.Clear();
        _rects.Clear();
        _rowWidth = 0;
        TotalHeight = 0;
        _maxLabelWidth = 0;
        _dirty = false;
    }

    public Rect GetRect(int relativeWidth = -1, float height = DefaultRowHeight, bool grow = false)
    {
        var trueWidth = relativeWidth == -1 ? _columnCount : relativeWidth;
        if (!_rects.NullOrEmpty())
        {
            _cellIndex++;
            if (_cellIndex <= _rects.Count)
            {
                var output = _rects[_cellIndex - 1];
                if (height > output.height) _dirty = true;
                else return output with { height = height };
            }
            else
            {
                _dirty = true;
            }
        }

        if (_rowWidth + trueWidth > _columnCount)
        {
            CacheCurrentRow();
            _rowWidth = 0;
            _cells.Clear();
        }

        _rowWidth += trueWidth;
        _cells.Add(new ListingCell(trueWidth, grow, height));

        return new Rect(0, 0, _listingRect.width * ((float)relativeWidth / _columnCount) - Spacing.x, height);
    }

    public void NewRow()
    {
        _rowWidth = _columnCount;
        // _rowWidth = 0;
        // _cells.Clear();
    }

    public void CacheCurrentRow()
    {
        var availableSpace = _listingRect.width - Spacing.x * (_cells.Count - 1);
        var leftOverWidthRel = _columnCount - _cells.Sum(c => c.RelativeWidth);
        var sumGrow = _cells.Sum(c => c.Grow ? 1 : 0);
        var rowMaxHeight = _cells.Max(c => c.Height);

        var rowRect = _listingRect.TakeTopPart(rowMaxHeight);
        _listingRect.yMin += Spacing.y;
        TotalHeight += rowMaxHeight + Spacing.y;

        foreach (var cell in _cells)
        {
            float widthRel = cell.RelativeWidth;
            widthRel += cell.Grow ? leftOverWidthRel > 0 ? 1f / sumGrow * leftOverWidthRel : 0 : 0;
            _cachedRects.Add(rowRect.TakeLeftPart(widthRel / _columnCount * availableSpace));
            rowRect.xMin += Spacing.x;
        }
    }

    public Rect RectLabeled(string label, int width = -1, float height = DefaultRowHeight, string? tooltip = null,
        bool grow = false)
    {
        var r = GetRect(width, height, grow);

        if (!tooltip.NullOrEmpty()) TooltipHandler.TipRegion(r, (TipSignal)tooltip);

        if (Event.current.type == EventType.Layout) _maxLabelWidth = Mathf.Max(_maxLabelWidth, Text.CalcSize(label).x);

        var labelRect = r.TakeLeftPart(_maxLabelWidth + LabelPadding);
        Widgets.Label(labelRect.TopPartPixels(DefaultRowHeight), label);

        return r;
    }

    public bool ButtonTextLabeled(string label, string buttonLabel, int width = -1, string? tooltip = null,
        bool grow = false)
    {
        var r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public bool ButtonText(string buttonLabel, int width = -1, string? tooltip = null, bool grow = false)
    {
        var r = GetRect(width, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public float SliderLabeled(string label, float val, float min, float max, int width = -1, string? tooltip = null,
        bool grow = false)
    {
        var r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        return Widgets.HorizontalSlider(r, val, min, max, true);
    }

    public float SliderLabeled(string label, float val, float min, float max, string valLabel, string minLabel,
        string maxLabel, int width = -1, string? tooltip = null,
        bool grow = false)
    {
        var r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        return Widgets.HorizontalSlider(r, val, min, max, true, valLabel, minLabel, maxLabel);
    }

    public void ColorPickerLabeled(string label, float height, ref Color color,
        Dictionary<string, Color>? specialColors, List<Color> colors, Action<Color> onApply,
        out float selectorHeight,
        int width = -1,
        string? tooltip = null, bool grow = false)
    {
        var oldColor = color;
        var r = RectLabeled(label, width, height, tooltip, grow);
        var availableColorsWithTransparent =
            colors.Append(new Color(0, 0, 0, 0f)).ToList(); // Little hack to add the color picker button to the end.

        if (r.width <= 0)
        {
            selectorHeight = height;
            return;
        }

        Widgets.ColorSelector(r, ref color, availableColorsWithTransparent, out var selectorHeight1,
            extraOnGUI: (currentColor, rect) =>
            {
                if (currentColor.a != 0) return;
                if (Widgets.ButtonImage(rect.ExpandedBy(2f), Designator_Eyedropper.EyeDropperTex))
                    Find.WindowStack.Add(new Dialog_ColorPicker(onApply, oldColor, colors, specialColors));
            });

        selectorHeight = selectorHeight1;
    }

    public void CheckboxLabeled(string label, ref bool val, int width = -1, string? tooltip = null, bool grow = false)
    {
        var r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
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