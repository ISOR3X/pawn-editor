using HotSwap;
using PawnEditor.Extensions;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor.Table;

[HotSwappable]
public class Table<TRow>(
    IEnumerable<TRow> rows,
    IReadOnlyList<ColumnWorker<TRow>> columns,
    ITableContext? context = null,
    IReadOnlyList<IRowFilter<TRow>>? filters = null,
    Action<Rect, TRow, ITableContext?>? onRowHover = null,
    Func<TRow, string>? searchProjection = null,
    Action<TRow?>? onSelectChanged = null
)
{
    private const float DefaultRowHeight = 30f;
    internal const float HeaderHeight = 30f;

    private readonly IReadOnlyList<TrackSizingFunction> _columnTracks = columns.Select(c => c.TrackSize).ToList();

    private readonly QuickSearchWidget? _searchWidget = searchProjection != null ? new QuickSearchWidget() : null;

    private bool _dirty = true;
    private Vector2 _scrollPosition;
    private ColumnWorker<TRow>? _sortingBy;
    private bool _sortDescending;

    public TRow? Selected { get; set; }
    public List<TRow> Rows { get; } = [];

    public int FilteredRowCount => Rows.Count;
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

        // --- Search footer ---
        if (_searchWidget != null)
        {
            var footerRect = r.TakeBottomPart(UIUtility.ButtonHeight);
            r.yMax -= GenUI.GapTiny;
            _searchWidget.OnGUI(footerRect.RightPartPixels(180f), SetDirty);
        }

        // --- Header ---
        // Use the same content width as the scroll view to keep columns aligned.
        var headerRect = r.TakeTopPart(HeaderHeight);
        var headerContentRect = new Rect(headerRect.x, headerRect.y, headerRect.width - UIUtility.ScrollBarWidth,
            headerRect.height);
        Taffy.Grid(headerContentRect, _columnTracks, GenUI.GapSmall, 0f, HeaderHeight, grid =>
        {
            foreach (var col in columns)
            {
                grid.Item(draw: colRect =>
                {
                    col.DrawHeader(colRect);

                    if (ReferenceEquals(col, _sortingBy))
                    {
                        var icon = _sortDescending ? PawnColumnWorker.SortingDescendingIcon : PawnColumnWorker.SortingIcon;
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

        // --- Scroll view ---
        var contentHeight = Rows.Count > 0
            ? Rows.Count * DefaultRowHeight
            : UIUtility.ButtonHeight;
        var viewRect = new Rect(0f, 0f, r.width - UIUtility.ScrollBarWidth, contentHeight);

        Verse.Widgets.BeginScrollView(r, ref _scrollPosition, viewRect);

        if (Rows.Count == 0)
        {
            using (new GUIColor(ColoredText.SubtleGrayColor))
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(viewRect, "No results available.");
        }
        else
        {
            var visibleTop = _scrollPosition.y;
            var visibleBottom = _scrollPosition.y + r.height;

            var firstVisible = Math.Max(0, (int)(visibleTop / DefaultRowHeight));
            var lastVisible = Math.Min(Rows.Count - 1, (int)(visibleBottom / DefaultRowHeight));

            // Row backgrounds and interaction
            for (var i = firstVisible; i <= lastVisible; i++)
            {
                var row = Rows[i];
                var rowRect = new Rect(0f, i * DefaultRowHeight, viewRect.width, DefaultRowHeight);

                if (Selected != null && EqualityComparer<TRow>.Default.Equals(row, Selected))
                    Verse.Widgets.DrawHighlightSelected(rowRect);
                else if (i % 2 == 1)
                    Verse.Widgets.DrawLightHighlight(rowRect);

                Verse.Widgets.DrawHighlightIfMouseover(rowRect);
                MouseoverSounds.DoRegion(rowRect);
                onRowHover?.Invoke(rowRect, row, context);

                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    Selected = row;
                    onSelectChanged?.Invoke(row);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Event.current.Use(); // Use the event so other widgets don't get it.
                }
            }

            // Cell content — one CSS Grid layout pass for all visible cells
            var gridRect = new Rect(0f, firstVisible * DefaultRowHeight,
                viewRect.width, (lastVisible - firstVisible + 1) * DefaultRowHeight);

            Taffy.Grid(gridRect, _columnTracks, GenUI.GapSmall, 0f, DefaultRowHeight, grid =>
            {
                for (var i = firstVisible; i <= lastVisible; i++)
                {
                    var row = Rows[i];
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
    }

    private void HandleHeaderClick(ColumnWorker<TRow> col, int button)
    {
        if (button == 0) // LMB: off → asc, asc → desc, desc → off
        {
            if (!ReferenceEquals(_sortingBy, col)) SortBy(col, true);
            else if (_sortDescending) SortBy(col, false);
            else SortBy(null, false);
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        else if (button == 1) // RMB: off → desc, desc → asc, asc → off
        {
            if (!ReferenceEquals(_sortingBy, col)) SortBy(col, false);
            else if (_sortDescending) SortBy(col, true);
            else SortBy(null, false);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
    }

    private void RecacheFilteredRows()
    {
        Rows.Clear();

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

            Rows.Add(row);
        }

        if (_sortingBy != null)
        {
            var dir = _sortDescending ? -1 : 1;
            Rows.SortStable((a, b) => _sortingBy.Compare(a, b) * dir);
        }
    }
}