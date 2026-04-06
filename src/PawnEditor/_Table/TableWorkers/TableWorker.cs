using HotSwap;
using PawnEditor.Extensions;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Display = Taffy.Display;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

[HotSwappable]
public abstract class TableWorker<T> where T : class
{
    public const float DefaultRowHeight = 30f;

    private readonly Color _borderColor = new(1f, 1f, 1f, 0.2f);
    private readonly List<ColumnWorker<T>> _cachedColumns = [];
    private readonly List<float> _cachedColumnWidths = [];
    private readonly List<float> _cachedRowHeights = [];

    // Precomputed cumulative Y positions for each row (index i = Y offset of row i from top of content).
    // Count is _cachedThings.Count + 1: the extra entry is the total content height.
    private readonly List<float> _cachedRowYPositions = [];

    private readonly TableDef _def;
    private readonly QuickSearchWidget _quickSearchWidget = new();
    private readonly Func<IEnumerable<T>> _thingsGetter;
    private float _cachedHeaderHeight;
    private Vector2 _cachedSize;
    private readonly List<T> _cachedThings = [];
    private bool _dirty;
    private Vector2 _scrollPosition;

    public T? Selected
    {
        get;
        set
        {
            field = value;
            OnSelectChanged(field);
        }
    }

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
        Selected = defaultThing;
        _thingsGetter = thingsGetter;
        SetDirty();
    }

    protected virtual Rect DoFooter(Rect inRect)
    {
        var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);

        if (_def.SearchColumn != null && inRect.height < 9000f)
        {
            inRect.yMax -= 4f;
            _quickSearchWidget.OnGUI(footerRect.RightPartPixels(180f), SetDirty);
        }

        return footerRect;
    }

    public void TableOnGUI(Rect inRect)
    {
        DoFooter(inRect.TakeBottomPart(UIUtility.ButtonHeight));

        if (_cachedSize != inRect.size)
        {
            _cachedSize = inRect.size;
            SetDirty();
        }

        if (Event.current.type == EventType.Layout)
            return;
        RecacheIfDirty();

        // --- Header ---
        var headerRect = inRect.TakeTopPart(_cachedHeaderHeight);

        using (new GUIColor(_borderColor))
            Verse.Widgets.DrawLineHorizontal(headerRect.x, headerRect.yMax, inRect.width);

        for (var colIndex = 0; colIndex < _cachedColumns.Count; ++colIndex)
        {
            var headerColRect = headerRect.TakeLeftPart(_cachedColumnWidths[colIndex]);
            _cachedColumns[colIndex].DoHeader(headerColRect, this);
        }

        // --- Scroll view ---
        var contentHeight = _cachedThings.Count > 0 ? _cachedRowYPositions[^1] : UIUtility.ButtonHeight;
        var viewRect = new Rect(0f, 0f, inRect.width - UIUtility.ScrollBarWidth, contentHeight);

        Verse.Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

        var visibleTop = _scrollPosition.y;
        var visibleBottom = _scrollPosition.y + inRect.height;

        if (_cachedThings.Count == 0)
        {
            using (new GUIColor(ColoredText.SubtleGrayColor))
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(viewRect, "No results available.");
        }

        for (var rowIndex = 0; rowIndex < _cachedThings.Count; ++rowIndex)
        {
            var rowY = _cachedRowYPositions[rowIndex];
            var rowHeight = _cachedRowHeights[rowIndex];

            // Skip rows above the viewport.
            if (rowY + rowHeight < visibleTop)
                continue;

            // All further rows are below the viewport, stop entirely.
            if (rowY > visibleBottom)
                break;

            var thing = _cachedThings[rowIndex];
            var rowRect = new Rect(0f, rowY, viewRect.width, rowHeight);

            // Row highlights
            if (Selected == thing && _def.highlightSelected)
                Verse.Widgets.DrawHighlightSelected(rowRect);
            else if (rowIndex % 2 == 1)
            {
                Verse.Widgets.DrawLightHighlight(rowRect);
            }

            if (Mouse.IsOver(rowRect))
            {
                GUI.DrawTexture(rowRect, TexUI.HighlightTex);
                DoRowHover(rowRect, thing);
            }

            // Click handler for the full row. We can't use Widgets.InvisibleButton as that consumes the click event.
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                OnRowClicked(thing);

            for (var colIndex = 0; colIndex < _cachedColumns.Count; ++colIndex)
            {
                var columnWorker = _cachedColumns[colIndex];
                var columnWidth = (int)_cachedColumnWidths[colIndex];
                var cellRect = rowRect.TakeLeftPart(columnWidth);

                columnWorker.DoCell(cellRect, thing, this);
            }
        }

        Verse.Widgets.EndScrollView();
    }

    protected virtual void OnSelectChanged(T? thing)
    {
    }

    protected virtual void OnRowClicked(T thing)
    {
        if (Selected == thing) return;
        SoundDefOf.Click.PlayOneShotOnCamera();
        Selected = thing;
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
        var things = _thingsGetter();
        things = FilterBySearch(things).ToList();
        things = SortFunction(things).ToList();
        _cachedThings.AddRange(things);

        if (SortingBy != null)
        {
            var sortDir = SortingDescending ? -1 : 1;
            _cachedThings.SortStable((a, b) => SortingBy.Compare(a, b) * sortDir);
        }
    }

    protected virtual IEnumerable<T> SortFunction(IEnumerable<T> input)
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

    private void RecacheColumnWidths()
    {
        _cachedColumnWidths.Clear();
        if (_cachedColumns.Count == 0) return;

        var available = _cachedSize.x - UIUtility.ScrollBarWidth;
        var tree = new TaffyTree();
        var childIds = new List<NodeId>(_cachedColumns.Count);

        for (var i = 0; i < _cachedColumns.Count; i++)
        {
            var col = _cachedColumns[i];
            var style = new Style { flexGrow = col.Def.flexGrow, flexShrink = 1f };
            style.size =
                style.size.MapWidth(_ => Dimension.Length(Mathf.Max(col.Def.flexBasis, col.MeasureHeaderWidth())));
            if (col.Def.maxWidth > 0)
                style.maxSize = style.maxSize.MapWidth(_ => Dimension.Length(col.Def.maxWidth));
            childIds.Add(tree.NewLeaf(style));
        }

        var rootStyle = new Style
        {
            display = Display.Flex,
            flexDirection = FlexDirection.Row,
            size = new Size<Dimension>(Dimension.Length(available), Dimension.AUTO),
        };
        var root = tree.NewWithChildren(rootStyle, childIds);

        tree.ComputeLayout(root, new Size<AvailableSpace>(
            AvailableSpace.Definite(available),
            AvailableSpace.MaxContent));

        for (var i = 0; i < childIds.Count; i++)
            _cachedColumnWidths.Add(tree.Layout(childIds[i]).Size.Width);
    }
}