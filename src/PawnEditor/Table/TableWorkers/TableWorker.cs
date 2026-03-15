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
    private readonly List<bool> _columnAtMaxWidth = [];
    private readonly List<bool> _columnAtOptimalWidth = [];
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
        var availableWidth = _cachedSize.x - 18f;
        var num2 = 0;
        for (var index = 0; index < _cachedColumns.Count; ++index)
        {
            var width = index != _cachedColumns.Count - 1
                ? (int)_cachedColumnWidths[index]
                : (int)(availableWidth - (double)num2); // The last column takes up all remaining space.
            var rect = new Rect((int)position.x + num2, (int)position.y, width, (int)_cachedHeaderHeight);
            _cachedColumns[index].DoHeader(rect, this);
            num2 += width;
        }

        using (new GUIColor(_borderColor))
        {
            Verse.Widgets.DrawLineHorizontal(position.x, position.y + _cachedHeaderHeight,
                num2); // Draw a line under the header.
        }

        var outRect = new Rect((int)position.x, (int)position.y + (int)_cachedHeaderHeight, (int)_cachedSize.x,
            (int)_cachedSize.y - (int)_cachedHeaderHeight);
        var viewRect = new Rect(0.0f, 0.0f, outRect.width - 16f,
            (int)_cachedHeightNoScrollbar - (int)_cachedHeaderHeight);
        Verse.Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
        var x = 0;
        for (var columnIndex = 0; columnIndex < _cachedColumns.Count; ++columnIndex)
        {
            var y = 0;
            var columnWorker = _cachedColumns[columnIndex];
            var columnWidth = (int)_cachedColumnWidths[columnIndex];
            for (var thingIndex = 0; thingIndex < _cachedThings.Count; ++thingIndex)
            {
                var cachedRowHeight = _cachedRowHeights[thingIndex];
                if (thingIndex % 2 == 1)
                {
                    var x2 = x;
                    var num4 = columnWidth;
                    if (columnWorker.Def.showIcon)
                    {
                        x2 += (int)cachedRowHeight;
                        num4 -= (int)cachedRowHeight;
                    }

                    Verse.Widgets.DrawLightHighlight(new Rect(x2, y, num4, cachedRowHeight));
                }

                var rect = new Rect(x, y, columnWidth, (int)cachedRowHeight);
                var cachedThing = _cachedThings[thingIndex];
                var flag = false;

                if (_selected == cachedThing) Verse.Widgets.DrawHighlightSelected(rect);

                if (columnWorker.Def.groupable)
                {
                    var num4 = thingIndex;
                    for (var index3 = thingIndex + 1;
                         index3 < _cachedThings.Count &&
                         _cachedColumns[columnIndex].CanGroupWith(_cachedThings[thingIndex], _cachedThings[index3]);
                         ++index3)
                    {
                        rect.yMax += (int)_cachedRowHeights[index3];
                        num4 = index3;
                        flag = true;
                    }

                    thingIndex = num4;
                }

                if ((y - (double)_scrollPosition.y + (int)cachedRowHeight < 0.0 ? 1 :
                        y - (double)_scrollPosition.y > outRect.height ? 1 : 0) == 0)
                {
                    DoRow(_cachedColumns[columnIndex], rect, cachedThing);
                    if (columnWorker.Def.groupable & flag)
                        using (new GUIColor(_borderColor))
                        {
                            Verse.Widgets.DrawLineVertical(rect.xMin, rect.yMin, rect.height);
                            Verse.Widgets.DrawLineVertical(rect.xMax, rect.yMin, rect.height);
                        }
                }

                if (Verse.Widgets.ButtonInvisible(rect))
                {
                    OnRowClicked(cachedThing);
                }

                y += (int)rect.height;
            }

            x += columnWidth;
        }

        var y1 = 0;
        for (var index = 0; index < _cachedThings.Count; ++index)
        {
            var rect = new Rect(0.0f, y1, viewRect.width, (int)_cachedRowHeights[index]);
            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
                DoRowHover(rect, _cachedThings[index]);
            }

            y1 += (int)_cachedRowHeights[index];
        }

        Verse.Widgets.EndScrollView();
    }

    protected void DoRow(ColumnWorker<T> columnWorker, Rect cellRect, T cachedThing)
    {
        columnWorker.DoCell(cellRect, cachedThing, this);
    }

    public void TableOnGUI(Rect inRect)
    {
        if (_def.SearchColumn != null)
        {
            var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
            inRect.yMax -= 4f;
            // FIXME: Why is the search widget unfocused after typing a single character?
            // Potentially because of layout changes:
            // - Close button is added when the search bar is not empty.
            // - Most of the time the table transitions from a scroll view to a regular view.
            _quickSearchWidget.OnGUI(footerRect.RightPartPixels(180f), SetDirty);
        }

        if (_cachedSize != inRect.size)
        {
            _cachedSize = inRect.size;
            SetDirty();
        }

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
        foreach (var t in _cachedThings)
            _cachedRowHeights.Add(CalculateRowHeight(t));
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