using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class DefTable
{
    public const float DefaultRowHeight = 30f;
    private const float ScrollbarWidth = 16f;

    private readonly Color _borderColor = new(1f, 1f, 1f, 0.2f);
    private readonly List<ColumnDef> _cachedColumns = [];
    private readonly List<float> _cachedColumnWidths = [];
    private readonly List<float> _cachedRowHeights = [];
    private readonly List<bool> _columnAtMaxWidth = [];
    private readonly List<bool> _columnAtOptimalWidth = [];
    private readonly TableDef _def;
    private readonly Def? _default;
    private readonly QuickSearchWidget _quickSearchWidget = new();
    private readonly Func<IEnumerable<Def>> _thingsGetter;
    private float _cachedHeaderHeight;
    private float _cachedHeightNoScrollbar;
    private Vector2 _cachedSize;
    private List<Def> _cachedThings = [];
    private bool _dirty;
    private Vector2 _scrollPosition;
    private Def? _selected;
    private bool _sortDescending;

    protected DefTable(
        TableDef def,
        Func<IEnumerable<Def>> thingsGetter,
        Def? defaultThing = null)
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
                : (int)(availableWidth - (double)num2); // Last column takes up all remaining space.
            var rect = new Rect((int)position.x + num2, (int)position.y, width, (int)_cachedHeaderHeight);
            _cachedColumns[index].Worker.DoHeader(rect, this);
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
            var columnDef = _cachedColumns[columnIndex];
            var columnWidth = (int)_cachedColumnWidths[columnIndex];
            for (var thingIndex = 0; thingIndex < _cachedThings.Count; ++thingIndex)
            {
                var cachedRowHeight = _cachedRowHeights[thingIndex];
                if (_def.doAlternateStyle)
                {
                    using (new GUIColor(_borderColor))
                    {
                        Verse.Widgets.DrawLineHorizontal(x, y, columnWidth);
                    }
                }
                else if (thingIndex % 2 == 1)
                {
                    var x2 = x;
                    var num4 = columnWidth;
                    if (columnDef.showIcon)
                    {
                        var iconSize = cachedRowHeight;
                        x2 += (int)iconSize;
                        num4 -= (int)iconSize;
                    }

                    Verse.Widgets.DrawLightHighlight(new Rect(x2, y, num4, cachedRowHeight));
                }

                var rect = new Rect(x, y, columnWidth, (int)cachedRowHeight);
                var cachedThing = _cachedThings[thingIndex];
                var flag = false;

                if (Verse.Widgets.ButtonInvisible(rect))
                {
                    if (_selected != cachedThing && _def.highlightSelected)
                        _selected = cachedThing;
                    else if (_default != null) _selected = _default;
                    if (_selected != null) OnSelectChanged(_selected);
                }

                if (_selected == cachedThing) Verse.Widgets.DrawHighlightSelected(rect);

                if (columnDef.groupable)
                {
                    var num4 = thingIndex;
                    for (var index3 = thingIndex + 1;
                         index3 < _cachedThings.Count && _cachedColumns[columnIndex].Worker
                             .CanGroupWith(_cachedThings[thingIndex], _cachedThings[index3]);
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
                    if (columnDef.groupable & flag)
                        using (new GUIColor(_borderColor))
                        {
                            Verse.Widgets.DrawLineVertical(rect.xMin, rect.yMin, rect.height);
                            Verse.Widgets.DrawLineVertical(rect.xMax, rect.yMin, rect.height);
                        }
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

    protected virtual void DoRow(ColumnDef columnDef, Rect rect, Def cachedThing)
    {
        columnDef.Worker.DoCell(rect, cachedThing, this);
    }

    public void TableOnGUI(Rect inRect)
    {   
        if (_def.searchColumn != null)
        {
            var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
            inRect.yMax -= 4f;
            _quickSearchWidget.OnGUI(footerRect.RightPartPixels(150f), () => {});
        }

        if (_cachedSize != inRect.size)
        {
            _cachedSize = inRect.size;
            SetDirty();
        }

        TableOnGUI(inRect.position);
    }

    protected abstract void OnSelectChanged(Def thing);

    public void SetDirty()
    {
        _dirty = true;
    }

    protected virtual void DoRowHover(Rect inRect, Def thing)
    {
        UIUtility.DefIconPreview(inRect, thing);
    }

    public void SortBy(ColumnDef? column, bool descending)
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
        foreach (var column in _def.columns.Where(column => column.Worker.VisibleCurrently))
            _cachedColumns.Add(column);
    }

    private void RecacheThings()
    {
        _cachedThings.Clear();

        if (_def.searchColumn?.Worker is ColumnWorker_Text col && _quickSearchWidget.filter.Text != null)
            _cachedThings.AddRange(_thingsGetter().Where(t =>
            {
                var text = col.GetTextFor(t);
                if (text == null)
                    return false;
                return text.ToLower().Contains(_quickSearchWidget.filter.Text.ToLower());
            }));
        else
            _cachedThings.AddRange(_thingsGetter());

        _cachedThings = LabelSortFunction(_cachedThings).ToList();
        if (SortingBy != null)
        {
            if (_sortDescending)
                _cachedThings.SortStable((arg1, arg2) => SortingBy.Worker.Compare(arg1, arg2));
            else
                _cachedThings.SortStable((a, b) => SortingBy.Worker.Compare(b, a));
        }

        _cachedThings = PrimarySortFunction(_cachedThings).ToList();
    }

    protected virtual IEnumerable<Def> LabelSortFunction(IEnumerable<Def> input)
    {
        return input.OrderBy(p => p.label);
    }

    protected virtual IEnumerable<Def> PrimarySortFunction(IEnumerable<Def> input)
    {
        return input;
    }

    private void RecacheRowHeights()
    {
        _cachedRowHeights.Clear();
        foreach (var t in _cachedThings)
            _cachedRowHeights.Add(CalculateRowHeight(t));
    }

    private float GetOptimalWidth(ColumnDef column)
    {
        return Mathf.Max(column.Worker.GetOptimalWidth(this), 0.0f);
    }

    private float GetMinWidth(ColumnDef column)
    {
        return Mathf.Max(column.Worker.GetMinWidth(this), 0.0f);
    }

    private float GetMaxWidth(ColumnDef column)
    {
        return Mathf.Max(column.Worker.GetMaxWidth(this), 0.0f);
    }

    private float CalculateRowHeight(Def thing)
    {
        var height = _def.defaultRowHeight;
        foreach (var col in _cachedColumns)
            height = Mathf.Max(height, col.Worker.GetMinCellHeight(thing));
        return height;
    }

    private float CalculateHeaderHeight()
    {
        return _cachedColumns.Aggregate(0.0f, (current, t) => Mathf.Max(current, t.Worker.GetMinHeaderHeight(this)));
    }

    private float CalculateTotalRequiredHeight()
    {
        return CalculateHeaderHeight() + _cachedThings.Sum(CalculateRowHeight);
    }

    #region Properties

    public ColumnDef? SortingBy { get; private set; }

    public bool SortingDescending => SortingBy != null && _sortDescending;

    public Vector2 Size
    {
        get
        {
            RecacheIfDirty();
            return _cachedSize;
        }
    }

    public float HeightNoScrollbar
    {
        get
        {
            RecacheIfDirty();
            return _cachedHeightNoScrollbar;
        }
    }

    public float HeaderHeight
    {
        get
        {
            RecacheIfDirty();
            return _cachedHeaderHeight;
        }
    }

    public List<Def> ThingListForReading
    {
        get
        {
            RecacheIfDirty();
            return _cachedThings;
        }
    }

    #endregion

    #region COLUMN WIDTHS CACHING

    private void RecacheColumnWidths()
    {
        var totalAvailableSpaceForColumns = _cachedSize.x - ScrollbarWidth;
        RecacheColumnWidths_StartWithMinWidths(out var minWidthsSum);
        if (Mathf.Approximately(minWidthsSum, totalAvailableSpaceForColumns))
            return;
        if (minWidthsSum > (double)totalAvailableSpaceForColumns)
        {
            SubtractProportionally(minWidthsSum - totalAvailableSpaceForColumns, minWidthsSum);
        }
        else
        {
            RecacheColumnWidths_DistributeUntilOptimal(totalAvailableSpaceForColumns, ref minWidthsSum,
                out var noMoreFreeSpace);
            if (noMoreFreeSpace)
                return;
            RecacheColumnWidths_DistributeAboveOptimal(totalAvailableSpaceForColumns, ref minWidthsSum);
        }
    }

    private void RecacheColumnWidths_StartWithMinWidths(out float minWidthsSum)
    {
        minWidthsSum = 0.0f;
        _cachedColumnWidths.Clear();
        foreach (var minWidth in _cachedColumns.Select(GetMinWidth))
        {
            _cachedColumnWidths.Add(minWidth);
            minWidthsSum += minWidth;
        }
    }

    private void RecacheColumnWidths_DistributeUntilOptimal(
        float totalAvailableSpaceForColumns,
        ref float usedWidth,
        out bool noMoreFreeSpace)
    {
        _columnAtOptimalWidth.Clear();
        var cols = _cachedColumns;
        for (var index = 0; index < cols.Count; ++index)
            _columnAtOptimalWidth.Add(_cachedColumnWidths[index] >= (double)GetOptimalWidth(cols[index]));
        var num1 = 0;
        bool flag1;
        bool flag2;
        do
        {
            ++num1;
            if (num1 >= 10000)
            {
                Log.Error("Too many iterations.");
                break;
            }

            var a = cols.Where((_, index) => !_columnAtOptimalWidth[index]).Aggregate(float.MinValue,
                (current, t) => Mathf.Max(current, t.widthPriority));

            var optimalWidth = 0.0f;
            for (var index = 0; index < _cachedColumnWidths.Count; ++index)
                if (!_columnAtOptimalWidth[index] && Mathf.Approximately(cols[index].widthPriority, a))
                    optimalWidth += GetOptimalWidth(cols[index]);

            var remainingWidth = totalAvailableSpaceForColumns - usedWidth;
            flag1 = false;
            flag2 = false;
            for (var index = 0; index < _cachedColumnWidths.Count; ++index)
            {
                if (_columnAtOptimalWidth[index]) continue;

                if (!Mathf.Approximately(cols[index].widthPriority, a))
                {
                    flag1 = true;
                }
                else
                {
                    var usableWidth = remainingWidth * GetOptimalWidth(cols[index]) / optimalWidth;
                    var num5 = GetOptimalWidth(cols[index]) - _cachedColumnWidths[index];
                    if (usableWidth >= (double)num5)
                    {
                        usableWidth = num5;
                        _columnAtOptimalWidth[index] = true;
                        flag2 = true;
                    }
                    else
                    {
                        flag1 = true;
                    }

                    if (usableWidth > 0.0)
                    {
                        _cachedColumnWidths[index] += usableWidth;
                        usedWidth += usableWidth;
                    }
                }
            }

            if (usedWidth >= totalAvailableSpaceForColumns - 0.1f)
            {
                noMoreFreeSpace = true;
                return;
            }
        } while (flag1 && flag2);

        noMoreFreeSpace = false;
    }

    private void RecacheColumnWidths_DistributeAboveOptimal(
        float totalAvailableSpaceForColumns,
        ref float usedWidth)
    {
        _columnAtMaxWidth.Clear();
        var sumWidthPriority = 0;
        var cols = _cachedColumns;
        for (var index = 0; index < cols.Count; ++index)
        {
            _columnAtMaxWidth.Add(_cachedColumnWidths[index] >= (double)GetMaxWidth(cols[index]));
            sumWidthPriority += cols[index].widthPriority;
        }

        var num1 = 0;
        bool flag;
        do
        {
            ++num1;
            if (num1 >= 10000)
            {
                Log.Error("Too many iterations.");
                return;
            }

            var remainingWidth = totalAvailableSpaceForColumns - usedWidth;
            flag = false;
            for (var index = 0; index < cols.Count; ++index)
            {
                if (_columnAtMaxWidth[index]) continue;

                var num4 = remainingWidth * cols[index].widthPriority / sumWidthPriority;
                var num5 = GetMaxWidth(cols[index]) - _cachedColumnWidths[index];
                if (num4 >= (double)num5)
                {
                    num4 = num5;
                    _columnAtMaxWidth[index] = true;
                }
                else
                {
                    flag = true;
                }

                if (num4 > 0.0)
                {
                    _cachedColumnWidths[index] += num4;
                    usedWidth += num4;
                }
            }

            if (usedWidth >= totalAvailableSpaceForColumns - 0.1f)
                return;
        } while (flag);

        DistributeRemainingWidthProportionallyAboveMax(totalAvailableSpaceForColumns - usedWidth);
    }

    private void SubtractProportionally(float toSubtract, float totalUsedWidth)
    {
        for (var index = 0; index < _cachedColumnWidths.Count; ++index)
            _cachedColumnWidths[index] -= toSubtract * _cachedColumnWidths[index] / totalUsedWidth;
    }

    private void DistributeRemainingWidthProportionallyAboveMax(float toDistribute)
    {
        var cols = _cachedColumns;
        var num = cols.Sum(t => Mathf.Max(GetOptimalWidth(t), 1f));
        for (var index = 0; index < cols.Count; ++index)
            _cachedColumnWidths[index] += toDistribute * Mathf.Max(GetOptimalWidth(cols[index]), 1f) / num;
    }

    #endregion
}