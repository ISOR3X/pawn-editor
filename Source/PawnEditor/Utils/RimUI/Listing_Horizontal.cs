using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor;
using UnityEngine;
using Verse;

namespace RimUI;

[StaticConstructorOnStartup]
public class Listing_Horizontal
{
    public const float DefaultRowHeight = 30f;

    public readonly int ColumnCount;
    public float BlockSpacing = 4f;
    public float InlineSpacing = 12f;

    public Rect ListingRect;
    public float curHeight;

    private List<ListingCell> _cells = new();
    private List<Rect> _cachedRects = new();
    public List<Rect> Rects = new();

    private int _cellIndex;
    private int _rowWidth;

    private float _maxLabelWidth;
    public const float LabelPadding = 18f;

    public Listing_Horizontal(int columnCount = 12)
    {
        ColumnCount = columnCount;
    }

    public void Begin(Rect rect)
    {
        Widgets.BeginGroup(rect);
        Text.Anchor = TextAnchor.MiddleLeft;
        ListingRect = rect.AtZero();
        _cellIndex = 0;
    }

    public void End()
    {
        if (Rects.NullOrEmpty())
        {
            if (_rowWidth > 0) CacheCurrentRow(); // Cache final row
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
        curHeight = 0;
        _maxLabelWidth = 0;
    }

    public Rect GetRect(int relativeWidth = -1, float height = DefaultRowHeight, bool grow = true)
    {
        int trueWidth = relativeWidth == -1 ? ColumnCount : relativeWidth;

        if (!Rects.NullOrEmpty())
        {
            _cellIndex++;
            return _cachedRects[_cellIndex - 1];
        }

        if (_rowWidth + trueWidth > ColumnCount)
        {
            CacheCurrentRow();
            NewRow();
        }

        _rowWidth += trueWidth;
        _cells.Add(new ListingCell(trueWidth, grow, height));

        return new Rect();
    }

    public void NewRow()
    {
        _rowWidth = 0;
        _cells.Clear();
    }

    public void CacheCurrentRow()
    {
        float availableSpace = ListingRect.width - (InlineSpacing * (_cells.Count - 1));
        int leftOverWidthRel = ColumnCount - _cells.Sum(c => c.RelativeWidth);
        int sumGrow = _cells.Sum(c => c.Grow ? 1 : 0);
        float rowMaxHeight = _cells.Max(c => c.Height);

        Rect rowRect = ListingRect.TakeTopPart(rowMaxHeight);
        ListingRect.yMin += BlockSpacing;
        curHeight += rowMaxHeight + BlockSpacing;

        foreach (var cell in _cells)
        {
            float widthRel = cell.RelativeWidth;
            widthRel += cell.Grow ? leftOverWidthRel > 0 ? 1 / sumGrow * leftOverWidthRel : 0 : 0;
            _cachedRects.Add(rowRect.TakeLeftPart(widthRel / ColumnCount * availableSpace));
            rowRect.xMin += InlineSpacing;
        }
    }

    public Rect RectLabeled(string label, int width = -1, float height = DefaultRowHeight, string tooltip = null, bool grow = true)
    {
        Rect r = GetRect(width, height, grow) with { height = height };

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

    public Rect RectLabeledVStack(string label, int width = -1, float height = DefaultRowHeight + Text.SmallFontHeight, string tooltip = null, bool grow = true)
    {
        Rect r = GetRect(width, height, grow) with { height = height };

        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(r, (TipSignal)tooltip);
        }

        if (Event.current.type == EventType.Layout)
        {
            _maxLabelWidth = Mathf.Max(_maxLabelWidth, Text.CalcSize(label).x);
        }

        Rect labelRect = r.TakeBottomPart(Text.SmallFontHeight);
        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(labelRect.TopPartPixels(DefaultRowHeight), label);
        return r;
    }

    public bool ButtonTextLabeled(string label, string buttonLabel, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public bool ButtonTextLabeledVStack(string label, string buttonLabel, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = RectLabeledVStack(label, width, tooltip: tooltip, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public bool ButtonImageLabeledVStack(string label, Texture2D buttonText, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = RectLabeledVStack(label, width, tooltip: tooltip, grow: grow);
        var flag = Widgets.ButtonImageWithBG(r, buttonText, new Vector2(22, 22));
        return flag;
    }

    public bool ButtonText(string buttonLabel, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = GetRect(width, grow: grow);
        var flag = Widgets.ButtonText(r, buttonLabel.Truncate(r.width - 20f));
        return flag;
    }

    public bool ButtonDefLabeled(string label, Def def, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        var flag = UIUtility.ButtonTextImage(r, def);
        return flag;
    }

    public float SliderLabeled(string label, float val, float min, float max, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        return Widgets.HorizontalSlider(r, val, min, max, true);
    }

    public float SliderLabeled(string label, float val, float min, float max, string valLabel, string minLabel, string maxLabel, int width = -1, string tooltip = null, bool grow = true)
    {
        Rect r = RectLabeled(label, width, tooltip: tooltip, grow: grow);
        return Widgets.HorizontalSlider(r, val, min, max, true, valLabel, minLabel, maxLabel);
    }

    public void CheckboxLabeled(string label, ref bool val, int width = -1, string tooltip = null, bool grow = true)
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