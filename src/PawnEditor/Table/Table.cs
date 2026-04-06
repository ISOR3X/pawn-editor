using HotSwap;
using PawnEditor.Extensions;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor.Table;

/// <summary>
/// TODO:
/// - Sortable headers
/// - Offset in header last column (scrollbar)
/// - Search widget
/// </summary>
[HotSwappable]
public sealed class Table<TRow>(
    IEnumerable<TRow> rows,
    IReadOnlyList<ColumnWorker<TRow>> columns,
    ITableContext? context = null,
    IReadOnlyList<IRowFilter<TRow>>? filters = null,
    Action<Rect, TRow, ITableContext?>? onRowHover = null
)
{
    private const float DefaultRowHeight = 30f;
    private const float HeaderHeight = 30f;

    private readonly IReadOnlyList<TrackSizingFunction> _columnTracks = columns.Select(c => c.TrackSize).ToList();
    private readonly List<TRow> _rows = rows.ToList();

    private readonly List<TRow> _cachedFilteredRows = [];

    private bool _dirty = true;
    private Vector2 _scrollPosition;

    public TRow? Selected { get; private set; }
    public IReadOnlyList<IRowFilter<TRow>> Filters => filters ?? [];

    public void SetDirty() => _dirty = true;

    public void Draw(Rect r)
    {
        if (Event.current.type == EventType.Layout)
            return;

        if (_dirty)
        {
            _dirty = false;
            RecacheFilteredRows();
        }

        // --- Header ---
        var headerRect = r.TakeTopPart(HeaderHeight);
        Taffy.Grid(headerRect, _columnTracks, GenUI.GapSmall, 0f, HeaderHeight, grid =>
        {
            foreach (var col in columns)
                grid.Item(draw: colRect => col.DrawHeader(colRect));
        });

        using (new GUIColor(PawnTable.BorderColor))
            Verse.Widgets.DrawLineHorizontal(r.x, r.y, r.width);

        // --- Scroll view ---
        var contentHeight = _cachedFilteredRows.Count > 0
            ? _cachedFilteredRows.Count * DefaultRowHeight
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

            var firstVisible = Math.Max(0, (int)(visibleTop / DefaultRowHeight));
            var lastVisible = Math.Min(_cachedFilteredRows.Count - 1, (int)(visibleBottom / DefaultRowHeight));

            // Row backgrounds and interaction
            for (var i = firstVisible; i <= lastVisible; i++)
            {
                var row = _cachedFilteredRows[i];
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
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Event.current.Use(); // Use the event so other widgets don't get it.
                }
            }

            // Cell content — one CSS Grid layout pass for all visible cells
            var gridRect = new Rect(0f, firstVisible * DefaultRowHeight,
                viewRect.width, (lastVisible - firstVisible + 1) * DefaultRowHeight );

            Taffy.Grid(gridRect, _columnTracks, GenUI.GapSmall, 0f, DefaultRowHeight, grid =>
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
    }

    private void RecacheFilteredRows()
    {
        _cachedFilteredRows.Clear();
        foreach (var row in _rows)
        {
            if (filters == null || filters.All(f => f.Passes(row, context)))
                _cachedFilteredRows.Add(row);
        }
    }
}