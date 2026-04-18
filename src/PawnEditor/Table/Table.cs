using HotSwap;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using PawnEditor;
using Void;
using Void.Extensions;

namespace PawnEditor.Table;

[HotSwappable]
public class Table<TRow>(
    IEnumerable<TRow> rows,
    IReadOnlyList<ColumnWorker<TRow>> columns,
    IContext? context = null,
    IReadOnlyList<IRowFilter<TRow>>? filters = null,
    Action<Rect, TRow, IContext?>? onRowHover = null,
    Action<TRow?>? onRowClick = null,
    Func<TRow, string>? searchProjection = null,
    float rowHeight = 30f
)
{
    public const float HeaderHeight = UIUtility.ButtonHeight;
    public const float FooterHeight = UIUtility.ButtonHeight;

    // Fr tracks must use minmax(0, Nfr) instead of the default minmax(auto, Nfr).
    // In a virtualized table only a subset of rows is rendered each frame, so the
    // auto minimum causes columns to resize as different content scrolls into view.
    private readonly IReadOnlyList<TrackSizingFunction> _columnTracks = columns
        .Select(c => NormalizeTrack(c.TrackSize))
        .ToList();

    private static TrackSizingFunction NormalizeTrack(TrackSizingFunction t) =>
        t.Max.IsFr() && t.Min.Equals(MinTrackSizingFunction.AUTO)
            ? TrackSizingFunction.MinMax(MinTrackSizingFunction.ZERO, t.Max)
            : t;

    private readonly QuickSearchWidget? _searchWidget = searchProjection != null ? new QuickSearchWidget() : null;

    private bool _dirty = true;
    private Vector2 _scrollPosition;
    private ColumnWorker<TRow>? _sortingBy;
    private bool _sortDescending;

    public TRow? SelectedItem { get; private set; }
    private readonly List<TRow> _cachedFilteredRows = [];
    public List<TRow> Rows => rows.ToList();
    public IReadOnlyList<IRowFilter<TRow>> Filters => filters ?? [];


    public void SetDirty() => _dirty = true;

    public void SortBy(ColumnWorker<TRow>? column, bool descending)
    {
        _sortingBy = column;
        _sortDescending = descending;
        SetDirty();
    }

    public void Draw(Rect r)
    {
        if (Event.current.type == EventType.Layout)
            return;

        if (_dirty)
        {
            _dirty = false;
            RecacheFilteredRows();
        }

        #region FOOTER

        if (_searchWidget != null)
        {
            var footerRect = r.TakeBottomPart(FooterHeight);
            r.yMax -= GenUI.GapTiny;
            _searchWidget.OnGUI(footerRect.RightPartPixels(180f), SetDirty);
        }

        #endregion

        #region HEADER

        // Use the same content width as the scroll view to keep columns aligned.
        var headerRect = r.TakeTopPart(HeaderHeight);
        var headerContentRect = new Rect(headerRect.x, headerRect.y, headerRect.width - UIUtility.ScrollBarWidth,
            headerRect.height);


        Void.Taffy.Grid(headerContentRect, _columnTracks, GenUI.GapSmall, 0f, HeaderHeight, grid =>
        {
            foreach (var col in columns)
            {
                grid.Item(draw: colRect =>
                {
                    col.DrawHeader(colRect);

                    if (ReferenceEquals(col, _sortingBy))
                    {
                        var icon = _sortDescending
                            ? PawnColumnWorker.SortingDescendingIcon
                            : PawnColumnWorker.SortingIcon;
                        GUI.DrawTexture(
                            new Rect(colRect.xMax - icon.width - 1f, colRect.yMax - icon.height - 1f, icon.width,
                                icon.height),
                            icon);
                    }

                    if (col.Sortable)
                    {
                        if (Mouse.IsOver(colRect))
                            Verse.Widgets.DrawHighlight(colRect);

                        if (Event.current.type == EventType.MouseDown && colRect.Contains(Event.current.mousePosition))
                        {
                            HandleHeaderClick(col, Event.current.button);
                            Event.current.Use();
                        }
                    }
                });
            }
        });

        using (new GUIColor(PawnTable.BorderColor))
            Verse.Widgets.DrawLineHorizontal(r.x, r.y, r.width);

        #endregion

        #region SCROLL VIEW

        var contentHeight = _cachedFilteredRows.Count > 0
            ? _cachedFilteredRows.Count * rowHeight
            : UIUtility.ButtonHeight;
        var viewRect = new Rect(0f, 0f, r.width - UIUtility.ScrollBarWidth, contentHeight);

        Verse.Widgets.BeginScrollView(r, ref _scrollPosition, viewRect);

        if (_cachedFilteredRows.Count == 0)
        {
            using (new GUIColor(ColoredText.SubtleGrayColor))
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(viewRect, "No results available.");
        }
        else
        {
            var visibleTop = _scrollPosition.y;
            var visibleBottom = _scrollPosition.y + r.height;

            var firstVisible = Math.Max(0, (int)(visibleTop / rowHeight));
            var lastVisible = Math.Min(_cachedFilteredRows.Count - 1, (int)(visibleBottom / rowHeight));

            // Row backgrounds and interaction
            for (var i = firstVisible; i <= lastVisible; i++)
            {
                var row = _cachedFilteredRows[i];
                var rowRect = new Rect(0f, i * rowHeight, viewRect.width, rowHeight);

                if (SelectedItem != null && EqualityComparer<TRow>.Default.Equals(row, SelectedItem))
                    Verse.Widgets.DrawHighlightSelected(rowRect);
                else if (i % 2 == 1)
                    Verse.Widgets.DrawLightHighlight(rowRect);

                Verse.Widgets.DrawHighlightIfMouseover(rowRect);
                MouseoverSounds.DoRegion(rowRect);
                onRowHover?.Invoke(rowRect, row, context);

                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    SelectedItem = row;
                    onRowClick?.Invoke(row);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            // Cell content — one CSS Grid layout pass for all visible cells
            var gridRect = new Rect(0f, firstVisible * rowHeight,
                viewRect.width, (lastVisible - firstVisible + 1) * rowHeight);

            Void.Taffy.Grid(gridRect, _columnTracks, GenUI.GapSmall, 0f, rowHeight, grid =>
            {
                for (var i = firstVisible; i <= lastVisible; i++)
                {
                    var row = _cachedFilteredRows[i];
                    foreach (var col in columns)
                    {
                        if (col is IContextColumn<TRow> ctxCol && context != null)
                            ctxCol.DrawCell(grid, row, context);
                        else
                            col.DrawCell(grid, row);
                    }
                }
            });
        }

        Verse.Widgets.EndScrollView();

        #endregion
    }

    public void Draw(TaffyBuilder builder, int rowCount = 6, StyleOverride? style = null)
    {
        const float chrome = HeaderHeight + FooterHeight + GenUI.GapTiny;
        var resolvedStyle = (style ?? new StyleOverride()).Merge(new StyleOverride
        {
            minWidth = 400f,
            minHeight = chrome + rowHeight,
            height = chrome + Mathf.Clamp(_cachedFilteredRows.Count, 1, rowCount) * rowHeight,
            width = Dimension.Percent(1),
        });

        builder.Item(Draw, resolvedStyle);
    }

    private void HandleHeaderClick(ColumnWorker<TRow> col, int button)
    {
        if (button == 0) // LMB: off -> asc, asc -> desc, desc -> off
        {
            if (!ReferenceEquals(_sortingBy, col)) SortBy(col, true);
            else if (_sortDescending) SortBy(col, false);
            else SortBy(null, false);
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        else if (button == 1) // RMB: off -> desc, desc -> asc, asc -> off
        {
            if (!ReferenceEquals(_sortingBy, col)) SortBy(col, false);
            else if (_sortDescending) SortBy(col, true);
            else SortBy(null, false);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
    }

    private void RecacheFilteredRows()
    {
        _cachedFilteredRows.Clear();

        var searchText = _searchWidget?.filter.Text;
        foreach (var row in rows)
        {
            if (filters != null && !filters.All(f => f.Passes(row, context)))
                continue;
            if (!searchText.NullOrEmpty() && searchProjection != null)
            {
                var text = searchProjection(row);
                if (text == null || text.IndexOf(searchText!, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
            }

            _cachedFilteredRows.Add(row);
        }

        if (_sortingBy != null)
        {
            var dir = _sortDescending ? -1 : 1;
            _cachedFilteredRows.SortStable((a, b) => _sortingBy.Compare(a, b) * dir);
        }
    }
}