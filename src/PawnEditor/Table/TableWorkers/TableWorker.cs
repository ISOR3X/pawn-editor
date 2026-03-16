using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class TableWorker<T> where T : class
{
    public const float DefaultRowHeight = 30f;
    private const float ScrollbarWidth = 16f;

    private readonly Color _borderColor = new(1f, 1f, 1f, 0.2f);
    private readonly List<ColumnWorker<T>> _cachedColumns = [];
    private readonly List<float> _cachedColumnWidths = [];
    private readonly List<float> _cachedRowHeights = [];

    // Precomputed cumulative Y positions for each row (index i = Y offset of row i from top of content).
    // Count is _cachedThings.Count + 1: the extra entry is the total content height.
    private readonly List<float> _cachedRowYPositions = [];

    private readonly TableDef _def;
    private readonly T? _default;
    private readonly QuickSearchWidget _quickSearchWidget = new();
    private readonly Func<IEnumerable<T>> _thingsGetter;
    private float _cachedHeaderHeight;
    private float _cachedHeightNoScrollbar;
    private Vector2 _cachedSize;
    private List<T> _cachedThings = [];
    private bool _dirty;
    private Vector2 _scrollPosition;
    private T? _selected;
    private bool _sortDescending;

    private float? _rowHeightOverride;

    #region Properties

    public ColumnWorker<T>? SortingBy { get; private set; }

    public bool SortingDescending => SortingBy != null && _sortDescending;

    public float HeaderHeight
    {
        get
        {
            RecacheIfDirty();
            return _cachedHeaderHeight;
        }
    }

    public List<T> ThingListForReading
    {
        get
        {
            RecacheIfDirty();
            return _cachedThings;
        }
    }

    public float RowHeight
    {
        get => _rowHeightOverride ?? _def.defaultRowHeight;
        set
        {
            _rowHeightOverride = value;
            SetDirty();
        }
    }

    public Rect BoundRect => new(0, 0, _cachedSize.x, _cachedSize.y);

    protected abstract IEnumerable<ColumnWorker<T>> AllColumns { get; }

    #endregion


    protected TableWorker(
        TableDef def,
        Func<IEnumerable<T>> thingsGetter,
        T? defaultThing = null)
    {
        _def = def;
        _default = defaultThing;
        _thingsGetter = thingsGetter;
        SetDirty();
    }


    private void TableOnGUI(Vector2 position)
    {
        if (Event.current.type == EventType.Layout)
            return;
        RecacheIfDirty();

        // --- Header ---
        var availableWidth = _cachedSize.x - UIUtility.ScrollBarWidth;
        var headerX = 0;
        for (var colIndex = 0; colIndex < _cachedColumns.Count; ++colIndex)
        {
            var width = colIndex != _cachedColumns.Count - 1
                ? (int)_cachedColumnWidths[colIndex]
                : (int)(availableWidth - headerX);
            var rect = new Rect((int)position.x + headerX, (int)position.y, width, (int)_cachedHeaderHeight);
            _cachedColumns[colIndex].DoHeader(rect, this);
            headerX += width;
        }

        using (new GUIColor(_borderColor))
            Verse.Widgets.DrawLineHorizontal(position.x, position.y + _cachedHeaderHeight, headerX);

        // --- Scroll view ---
        var outRect = new Rect(
            (int)position.x,
            (int)position.y + (int)_cachedHeaderHeight,
            (int)_cachedSize.x,
            (int)_cachedSize.y - (int)_cachedHeaderHeight);

        var contentHeight = _cachedRowYPositions.Count > 0
            ? _cachedRowYPositions[^1]
            : 0f;
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, (int)contentHeight);

        Verse.Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);

        var visibleTop = _scrollPosition.y;
        var visibleBottom = _scrollPosition.y + outRect.height;

        // Row-major loop: process one row at a time across all columns.
        for (var rowIndex = 0; rowIndex < _cachedThings.Count; ++rowIndex)
        {
            var rowY = _cachedRowYPositions[rowIndex];
            var rowHeight = _cachedRowHeights[rowIndex];

            // Skip rows above the viewport.
            if (rowY + rowHeight < visibleTop)
                continue;

            // All further rows are below the viewport — stop entirely.
            if (rowY > visibleBottom)
                break;

            var thing = _cachedThings[rowIndex];
            var rowRect = new Rect(0f, rowY, viewRect.width, (int)rowHeight);

            // Hover highlight + hover callback (replaces the separate hover pass).
            if (Mouse.IsOver(rowRect))
            {
                GUI.DrawTexture(rowRect, TexUI.HighlightTex);
                DoRowHover(rowRect, thing);
            }

            // Selection highlight.
            if (_selected == thing)
                Verse.Widgets.DrawHighlightSelected(rowRect);

            // Per-column cells.
            var cellX = 0;
            for (var colIndex = 0; colIndex < _cachedColumns.Count; ++colIndex)
            {
                var columnWorker = _cachedColumns[colIndex];
                var columnWidth = (int)_cachedColumnWidths[colIndex];
                var cellRect = new Rect(cellX, rowY, columnWidth, (int)rowHeight);

                // Alternating the row background (per column, respecting icon offset).
                if (rowIndex % 2 == 1)
                {
                    var bgX = cellX;
                    var bgW = columnWidth;
                    if (columnWorker.Def.showIcon)
                    {
                        bgX += (int)rowHeight;
                        bgW -= (int)rowHeight;
                    }
                    Verse.Widgets.DrawLightHighlight(new Rect(bgX, rowY, bgW, rowHeight));
                }
                
                columnWorker.DoCell(cellRect, thing, this);
                
                cellX += columnWidth;
            }

            // Click handler for the full row.
            if (Verse.Widgets.ButtonInvisible(rowRect))
                OnRowClicked(thing);
        }

        Verse.Widgets.EndScrollView();
    }

    public void TableOnGUI(Rect inRect)
    {
        if (_def.SearchColumn != null)
        {
            var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
            inRect.yMax -= 4f;
            _quickSearchWidget.OnGUI(footerRect.RightPartPixels(180f), SetDirty);
        }

        if (_cachedSize != inRect.size)
        {
            _cachedSize = inRect.size;
            SetDirty();
        }
        
        var position = inRect.position;

        TableOnGUI(inRect.position);
    }

    protected virtual void OnSelectChanged(T thing)
    {
    }

    protected virtual void OnRowClicked(T thing)
    {
        if (!_def.highlightSelected) return;
        if (_selected != thing)
            _selected = thing;
        else if (_default != null) _selected = _default;
        if (_selected != null) OnSelectChanged(_selected);
    }

    public void SetDirty()
    {
        _dirty = true;
    }

    protected virtual void DoRowHover(Rect inRect, T thing)
    {
    }

    public void SortBy(ColumnWorker<T>? column, bool descending)
    {
        SortingBy = column;
        _sortDescending = descending;
        SetDirty();
    }

    private void RecacheIfDirty()
    {
        if (!_dirty)
            return;
        _dirty = false;
        RecacheColumns();
        RecacheThings();
        RecacheRowHeights();
        _cachedHeaderHeight = CalculateHeaderHeight();
        _cachedHeightNoScrollbar = CalculateTotalRequiredHeight();
        RecacheColumnWidths();
    }

    private void RecacheColumns()
    {
        _cachedColumns.Clear();
        _cachedColumns.AddRange(AllColumns.Where(w => w.VisibleCurrently));
    }

    protected virtual IEnumerable<T> FilterBySearch(IEnumerable<T> input)
    {
        if (_def.SearchColumn is not { } searchCol)
            return input;

        var searchWorker = _cachedColumns.FirstOrDefault(w => w.Def == searchCol);
        if (searchWorker is not ColumnWorker_Text<T> textWorker
            || _quickSearchWidget.filter.Text.NullOrEmpty())
            return input;

        return input.Where(t =>
        {
            var text = textWorker.GetTextFor(t);
            return text != null &&
                   text.IndexOf(_quickSearchWidget.filter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    private void RecacheThings()
    {
        _cachedThings.Clear();
        _cachedThings.AddRange(_thingsGetter());
        _cachedThings = FilterBySearch(_cachedThings).ToList();
        _cachedThings = LabelSortFunction(_cachedThings).ToList();

        if (SortingBy != null)
        {
            var sortMult = SortingDescending ? -1 : 1;
            _cachedThings.SortStable((a, b) => SortingBy.Compare(a, b) * sortMult);
        }

        _cachedThings = PrimarySortFunction(_cachedThings).ToList();
    }

    protected virtual IEnumerable<T> LabelSortFunction(IEnumerable<T> input)
    {
        return input;
    }

    protected virtual IEnumerable<T> PrimarySortFunction(IEnumerable<T> input)
    {
        return input;
    }

    private void RecacheRowHeights()
    {
        _cachedRowHeights.Clear();
        _cachedRowYPositions.Clear();

        var y = 0f;
        foreach (var h in _cachedThings.Select(CalculateRowHeight))
        {
            _cachedRowHeights.Add(h);
            _cachedRowYPositions.Add(y);
            y += h;
        }

        // Sentinel: total content height, used for viewRect construction.
        _cachedRowYPositions.Add(y);
    }

    private float CalculateRowHeight(T thing)
    {
        return _cachedColumns.Aggregate(RowHeight,
            (current, col) => Mathf.Max(current, col.GetMinCellHeight(thing)));
    }

    private float CalculateHeaderHeight()
    {
        return _cachedColumns.Aggregate(0.0f, (current, t) => Mathf.Max(current, t.GetMinHeaderHeight(this)));
    }

    private float CalculateTotalRequiredHeight()
    {
        return CalculateHeaderHeight() + _cachedThings.Sum(CalculateRowHeight);
    }

    private void RecacheColumnWidths()
    {
        var available = _cachedSize.x - ScrollbarWidth;

        var line = _cachedColumns.Select(col => new LayoutNode<ColumnWorker<T>>
        {
            leaf = col,
            flexBasis = Mathf.Max(col.Def.flexBasis, col.MeasureHeaderWidth()),
            flexGrow = col.Def.flexGrow,
            maxWidth = col.Def.maxWidth,
        }).ToList();

        var widths = FlexLayoutEngine.ResolveWidths(line, available, 0f);

        _cachedColumnWidths.Clear();
        _cachedColumnWidths.AddRange(widths);
    }
}

public abstract class DefTableWorker(DefTableDef def, Func<IEnumerable<Def>> thingsGetter, Def? defaultThing = null)
    : TableWorker<Def>(def, thingsGetter, defaultThing)
{
    protected override IEnumerable<ColumnWorker<Def>> AllColumns => def.columns.Select(c => c.Worker);
}