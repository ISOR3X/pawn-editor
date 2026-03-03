using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class DefTable
{
    private readonly Color BorderColor = new(1f, 1f, 1f, 0.2f);
    private TableDef def;
    private Func<IEnumerable<Def>> thingsGetter;
    private bool dirty;
    private List<bool> columnAtMaxWidth = new();
    private List<bool> columnAtOptimalWidth = new();
    private Vector2 scrollPosition;
    private ColumnDef sortByColumn;
    private bool sortDescending;
    private Vector2 cachedSize;
    private List<Def> cachedThings = new();
    private List<float> cachedColumnWidths = new();
    private List<float> cachedRowHeights = new();
    private List<ColumnDef> columns = new();
    public static float defaultRowHeight = 30f;
    private float cachedHeaderHeight;
    private float cachedHeightNoScrollbar;
    public Def Selected;
    public Def Default;
    public QuickSearchWidget quickSearchWidget = new QuickSearchWidget();

    #region Properties

    public List<ColumnDef> Columns
    {
        get
        {
            columns.Clear();
            foreach (ColumnDef column in def.columns)
            {
                if (column.Worker.VisibleCurrently)
                    columns.Add(column);
            }

            return columns;
        }
    }

    public ColumnDef SortingBy => sortByColumn;

    public bool SortingDescending => SortingBy != null && sortDescending;

    public Vector2 Size
    {
        get
        {
            RecacheIfDirty();
            return cachedSize;
        }
    }

    public float HeightNoScrollbar
    {
        get
        {
            RecacheIfDirty();
            return cachedHeightNoScrollbar;
        }
    }

    public float HeaderHeight
    {
        get
        {
            RecacheIfDirty();
            return cachedHeaderHeight;
        }
    }

    public List<Def> ThingListForReading
    {
        get
        {
            RecacheIfDirty();
            return cachedThings;
        }
    }

    #endregion

    protected DefTable(
        TableDef def,
        Func<IEnumerable<Def>> thingsGetter,
        Def defaultThing = null)
    {
        this.def = def;
        this.Default = defaultThing;
        this.thingsGetter = thingsGetter;
        SetDirty();
    }

    // TODO:Rework to use Rect instead of Vector2
    private void TableOnGUI(Vector2 position)
    {
        if (Event.current.type == EventType.Layout)
            return;
        RecacheIfDirty();
        float availableWidth = cachedSize.x - 18f;
        List<ColumnDef> columns = Columns;
        int num2 = 0;
        for (int index = 0; index < columns.Count; ++index)
        {
            int width = index != columns.Count - 1 ? (int)cachedColumnWidths[index] : (int)(availableWidth - (double)num2); // Last column takes up all remaining space.
            Rect rect = new Rect((int)position.x + num2, (int)position.y, width, (int)cachedHeaderHeight);
            columns[index].Worker.DoHeader(rect, this);
            num2 += width;
        }

        GUI.color = BorderColor;
        Widgets.DrawLineHorizontal(position.x, position.y + cachedHeaderHeight, num2); // Draw line under header.
        GUI.color = Color.white;

        Rect outRect = new Rect((int)position.x, (int)position.y + (int)cachedHeaderHeight, (int)cachedSize.x, (int)cachedSize.y - (int)cachedHeaderHeight);
        Rect viewRect = new Rect(0.0f, 0.0f, outRect.width - 16f, (int)cachedHeightNoScrollbar - (int)cachedHeaderHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        int x = 0;
        for (int columnIndex = 0; columnIndex < columns.Count; ++columnIndex)
        {
            int y = 0;
            ColumnDef columnDef = columns[columnIndex];
            // int columnWidth = columnIndex != columns.Count - 1 ? (int)cachedColumnWidths[columnIndex] : (int)(availableWidth - (double)x);
            int columnWidth = (int)cachedColumnWidths[columnIndex];
            for (int thingIndex = 0; thingIndex < cachedThings.Count; ++thingIndex)
            {
                var cachedRowHeight = cachedRowHeights[thingIndex];
                if (def.doAlternateStyle)
                {
                    GUI.color = BorderColor;
                    Widgets.DrawLineHorizontal(x, y, columnWidth);
                    GUI.color = Color.white;
                }
                else if (thingIndex % 2 == 1)
                {
                    int x2 = x;
                    int num4 = columnWidth;
                    if (columnDef.showIcon)
                    {
                        var iconSize = cachedRowHeight;
                        x2 += (int)iconSize;
                        num4 -= (int)iconSize;
                    }

                    Widgets.DrawLightHighlight(new Rect(x2, y, num4, cachedRowHeight));
                }

                Rect rect = new Rect(x, y, columnWidth, (int)cachedRowHeight);
                Def cachedThing = cachedThings[thingIndex];
                bool flag = false;

                if (Widgets.ButtonInvisible(rect))
                {
                    if (Selected != cachedThing && def.highlightSelected)
                    {
                        Selected = cachedThing;
                    }
                    else if (Default != null)
                    {
                        Selected = Default;
                    }

                    OnSelectChanged(Selected);
                }

                if (Selected == cachedThing)
                {
                    Widgets.DrawHighlightSelected(rect);
                }

                if (columnDef.groupable)
                {
                    int num4 = thingIndex;
                    for (int index3 = thingIndex + 1;
                         index3 < cachedThings.Count && columns[columnIndex].Worker.CanGroupWith(cachedThings[thingIndex], cachedThings[index3]);
                         ++index3)
                    {
                        rect.yMax += (int)cachedRowHeights[index3];
                        num4 = index3;
                        flag = true;
                    }

                    thingIndex = num4;
                }

                if ((y - (double)scrollPosition.y + (int)cachedRowHeight < 0.0 ? 1 : (y - (double)scrollPosition.y > outRect.height ? 1 : 0)) == 0)
                {
                    DoRow(columns[columnIndex], rect, cachedThing);
                    if (columnDef.groupable & flag)
                    {
                        GUI.color = BorderColor;
                        Widgets.DrawLineVertical(rect.xMin, rect.yMin, rect.height);
                        Widgets.DrawLineVertical(rect.xMax, rect.yMin, rect.height);
                        GUI.color = Color.white;
                    }
                }

                GUI.color = Color.white;
                y += (int)rect.height;
            }

            x += columnWidth;
        }

        int y1 = 0;
        for (int index = 0; index < cachedThings.Count; ++index)
        {
            Rect rect = new Rect(0.0f, y1, viewRect.width, (int)cachedRowHeights[index]);
            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
                DoRowHover(rect, cachedThings[index]);
            }

            y1 += (int)cachedRowHeights[index];
        }

        Widgets.EndScrollView();
    }

    public virtual void DoRow(ColumnDef columnDef, Rect rect, Def cachedThing)
    {
        columnDef.Worker.DoCell(rect, cachedThing, this);
    }

    public void TableOnGUI(Rect inRect)
    {
        if (def.searchColumn != null)
        {
            var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
            this.quickSearchWidget.OnGUI(footerRect.RightPartPixels(150f), SetDirty);
            inRect.yMax -= 4f;
        }

        if (cachedSize != inRect.size)
        {
            cachedSize = inRect.size;
            SetDirty();
        }

        TableOnGUI(inRect.position);
    }

    protected abstract void OnSelectChanged(Def thing);

    public void SetDirty() => dirty = true;

    protected virtual void DoRowHover(Rect inRect, Def thing) => UIUtility.DefIconPreview(inRect, thing);


    public void SortBy(ColumnDef column, bool descending)
    {
        sortByColumn = column;
        sortDescending = descending;
        SetDirty();
    }

    private void RecacheIfDirty()
    {
        if (!dirty)
            return;
        dirty = false;
        RecacheThings();
        RecacheRowHeights();
        cachedHeaderHeight = CalculateHeaderHeight();
        cachedHeightNoScrollbar = CalculateTotalRequiredHeight();
        RecacheColumnWidths();
    }

    private void RecacheThings()
    {
        cachedThings.Clear();

        if (def.searchColumn?.Worker is ColumnWorker_Text col && quickSearchWidget.filter.Text != null)
        {
            cachedThings.AddRange(thingsGetter().Where(t =>
            {
                var text = col.GetTextFor(t);
                if (text == null)
                    return false;
                return text.ToLower().Contains(quickSearchWidget.filter.Text.ToLower());
            }));
        }
        else
        {
            cachedThings.AddRange(thingsGetter());
        }

        cachedThings = LabelSortFunction(cachedThings).ToList();
        if (sortByColumn != null)
        {
            if (sortDescending)
                cachedThings.SortStable((arg1, arg2) => sortByColumn.Worker.Compare(arg1, arg2));
            else
                cachedThings.SortStable((a, b) => sortByColumn.Worker.Compare(b, a));
        }

        cachedThings = PrimarySortFunction(cachedThings).ToList();
    }

    protected virtual IEnumerable<Def> LabelSortFunction(IEnumerable<Def> input) => input.OrderBy(p => p.label);

    protected virtual IEnumerable<Def> PrimarySortFunction(IEnumerable<Def> input) => input;

    #region COLUMN WIDTHS CACHING

    private void RecacheColumnWidths()
    {
        float totalAvailableSpaceForColumns = cachedSize.x - 16f;
        RecacheColumnWidths_StartWithMinWidths(out var minWidthsSum);
        if (Mathf.Approximately(minWidthsSum, totalAvailableSpaceForColumns))
            return;
        if (minWidthsSum > (double)totalAvailableSpaceForColumns)
        {
            SubtractProportionally(minWidthsSum - totalAvailableSpaceForColumns, minWidthsSum);
        }
        else
        {
            RecacheColumnWidths_DistributeUntilOptimal(totalAvailableSpaceForColumns, ref minWidthsSum, out var noMoreFreeSpace);
            if (noMoreFreeSpace)
                return;
            RecacheColumnWidths_DistributeAboveOptimal(totalAvailableSpaceForColumns, ref minWidthsSum);
        }
    }

    private void RecacheColumnWidths_StartWithMinWidths(out float minWidthsSum)
    {
        minWidthsSum = 0.0f;
        cachedColumnWidths.Clear();
        List<ColumnDef> col = Columns;
        foreach (var minWidth in col.Select(GetMinWidth))
        {
            cachedColumnWidths.Add(minWidth);
            minWidthsSum += minWidth;
        }
    }

    private void RecacheColumnWidths_DistributeUntilOptimal(
        float totalAvailableSpaceForColumns,
        ref float usedWidth,
        out bool noMoreFreeSpace)
    {
        columnAtOptimalWidth.Clear();
        List<ColumnDef> cols = Columns;
        for (int index = 0; index < cols.Count; ++index)
            columnAtOptimalWidth.Add(cachedColumnWidths[index] >= (double)GetOptimalWidth(cols[index]));
        int num1 = 0;
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

            float a = cols.Where((t, index) => !columnAtOptimalWidth[index]).Aggregate(float.MinValue, (current, t) => Mathf.Max(current, t.widthPriority));

            float optimalWidth = 0.0f;
            for (int index = 0; index < cachedColumnWidths.Count; ++index)
            {
                if (!columnAtOptimalWidth[index] && Mathf.Approximately(cols[index].widthPriority, a))
                    optimalWidth += GetOptimalWidth(cols[index]);
            }

            float remainingWidth = totalAvailableSpaceForColumns - usedWidth;
            flag1 = false;
            flag2 = false;
            for (int index = 0; index < cachedColumnWidths.Count; ++index)
            {
                if (columnAtOptimalWidth[index]) continue;
                
                if (!Mathf.Approximately(cols[index].widthPriority, a))
                {
                    flag1 = true;
                }
                else
                {
                    float usableWidth = remainingWidth * GetOptimalWidth(cols[index]) / optimalWidth;
                    float num5 = GetOptimalWidth(cols[index]) - cachedColumnWidths[index];
                    if (usableWidth >= (double)num5)
                    {
                        usableWidth = num5;
                        columnAtOptimalWidth[index] = true;
                        flag2 = true;
                    }
                    else
                        flag1 = true;

                    if (usableWidth > 0.0)
                    {
                        cachedColumnWidths[index] += usableWidth;
                        usedWidth += usableWidth;
                    }
                }
            }

            if (usedWidth >= totalAvailableSpaceForColumns - 0.100000001490116)
            {
                noMoreFreeSpace = true;
                break;
            }
        } while (flag1 && flag2);

        noMoreFreeSpace = false;
    }

    private void RecacheColumnWidths_DistributeAboveOptimal(
        float totalAvailableSpaceForColumns,
        ref float usedWidth)
    {
        columnAtMaxWidth.Clear();
        int sumWidthPriority = 0;
        List<ColumnDef> cols = Columns;
        for (int index = 0; index < cols.Count; ++index)
        {
            columnAtMaxWidth.Add(cachedColumnWidths[index] >= (double)GetMaxWidth(cols[index]));
            sumWidthPriority += cols[index].widthPriority;
        }

        int num1 = 0;
        bool flag;
        do
        {
            ++num1;
            if (num1 >= 10000)
            {
                Log.Error("Too many iterations.");
                return;
            }

            // float optimalWidth = 0.0f;
            // for (int index = 0; index < cols.Count; ++index)
            // {
            //     if (!columnAtMaxWidth[index])
            //         optimalWidth += Mathf.Max(GetOptimalWidth(cols[index]), 1f);
            // }

            float remainingWidth = totalAvailableSpaceForColumns - usedWidth;
            flag = false;
            for (int index = 0; index < cols.Count; ++index)
            {
                if (!columnAtMaxWidth[index])
                {
                    float num4 = remainingWidth * cols[index].widthPriority / sumWidthPriority;
                    float num5 = GetMaxWidth(cols[index]) - cachedColumnWidths[index];
                    if (num4 >= (double)num5)
                    {
                        num4 = num5;
                        columnAtMaxWidth[index] = true;
                    }
                    else
                        flag = true;

                    if (num4 > 0.0)
                    {
                        cachedColumnWidths[index] += num4;
                        usedWidth += num4;
                    }
                }
            }

            if (usedWidth >= totalAvailableSpaceForColumns - 0.100000001490116)
                goto label_23;
        } while (flag);

        goto label_22;
        label_23:
        return;
        label_22:
        DistributeRemainingWidthProportionallyAboveMax(totalAvailableSpaceForColumns - usedWidth);
    }

    private void SubtractProportionally(float toSubtract, float totalUsedWidth)
    {
        for (int index = 0; index < cachedColumnWidths.Count; ++index)
            cachedColumnWidths[index] -= toSubtract * cachedColumnWidths[index] / totalUsedWidth;
    }

    private void DistributeRemainingWidthProportionallyAboveMax(float toDistribute)
    {
        float num = 0.0f;
        List<ColumnDef> cols = Columns;
        for (int index = 0; index < cols.Count; ++index)
            num += Mathf.Max(GetOptimalWidth(cols[index]), 1f);
        for (int index = 0; index < cols.Count; ++index)
            cachedColumnWidths[index] += toDistribute * Mathf.Max(GetOptimalWidth(cols[index]), 1f) / num;
    }

    #endregion

    private void RecacheRowHeights()
    {
        cachedRowHeights.Clear();
        for (int index = 0; index < cachedThings.Count; ++index)
            cachedRowHeights.Add(CalculateRowHeight(cachedThings[index]));
    }


    private float GetOptimalWidth(ColumnDef column) => Mathf.Max(column.Worker.GetOptimalWidth(this), 0.0f);

    private float GetMinWidth(ColumnDef column) => Mathf.Max(column.Worker.GetMinWidth(this), 0.0f);

    private float GetMaxWidth(ColumnDef column) => Mathf.Max(column.Worker.GetMaxWidth(this), 0.0f);

    private float CalculateRowHeight(Def thing)
    {
        float a = 0.0f;
        List<ColumnDef> cols = Columns;
        foreach (var t in cols)
            a = Mathf.Max(def.defaultRowHeight, t.Worker.GetMinCellHeight(thing));

        return a;
    }

    private float CalculateHeaderHeight()
    {
        float a = 0.0f;
        List<ColumnDef> cols = Columns;
        for (int index = 0; index < cols.Count; ++index)
            a = Mathf.Max(a, cols[index].Worker.GetMinHeaderHeight(this));
        return a;
    }

    private float CalculateTotalRequiredHeight()
    {
        float headerHeight = CalculateHeaderHeight();
        for (int index = 0; index < cachedThings.Count; ++index)
            headerHeight += CalculateRowHeight(cachedThings[index]);
        return headerHeight;
    }
}