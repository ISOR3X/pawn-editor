using System;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor.Table;

[HotSwappable]
public sealed class Table<TRow>
{
    private const float DefaultRowHeight = 30f;
    private const float HeaderHeight = 30f;

    private readonly Color _borderColor = new(1f, 1f, 1f, 0.2f);
    private readonly IReadOnlyList<ColumnWorker<TRow>> _columns;
    private readonly IReadOnlyList<TrackSizingFunction> _columnTracks;
    private readonly ITableContext? _context;
    private readonly IReadOnlyList<IRowFilter<TRow>>? _filters;
    private readonly List<TRow> _rows;

    private readonly List<TRow> _cachedFilteredRows = [];

    private bool _dirty = true;
    private Vector2 _scrollPosition;

    public TRow? Selected { get; private set; }

    public Table(
        IEnumerable<TRow> rows,
        IReadOnlyList<ColumnWorker<TRow>> columns,
        ITableContext? context = null,
        IReadOnlyList<IRowFilter<TRow>>? filters = null)
    {
        _rows = rows.ToList();
        _columns = columns;
        _columnTracks = columns.Select(c => c.TrackSize).ToList();
        _context = context;
        _filters = filters;
    }

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
        Taffy.Grid(headerRect, _columnTracks, HeaderHeight, grid =>
        {
            foreach (var col in _columns)
                grid.Item(draw: colRect => col.DrawHeader(colRect));
        });

        using (new GUIColor(_borderColor))
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

                if (Mouse.IsOver(rowRect))
                    GUI.DrawTexture(rowRect, TexUI.HighlightTex);

                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    Selected = row;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Event.current.Use();
                }
            }

            // Cell content — one CSS Grid layout pass for all visible cells
            var gridRect = new Rect(0f, firstVisible * DefaultRowHeight,
                viewRect.width, (lastVisible - firstVisible + 1) * DefaultRowHeight);

            Taffy.Grid(gridRect, _columnTracks, DefaultRowHeight, grid =>
            {
                for (var i = firstVisible; i <= lastVisible; i++)
                {
                    var row = _cachedFilteredRows[i];
                    foreach (var col in _columns)
                    {
                        if (col is IContextColumn<TRow> ctxCol && _context != null)
                            ctxCol.DrawCell(grid, row, _context);
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
            if (_filters == null || _filters.All(f => f.Passes(row, _context)))
                _cachedFilteredRows.Add(row);
        }
    }
}
