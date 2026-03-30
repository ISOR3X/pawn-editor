using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Display = PawnEditor.TaffySharp.Display;

namespace PawnEditor.Table;

[HotSwappable]
public sealed class Table<TRow>
{
    private const float DefaultRowHeight = 30f;
    private const float HeaderHeight = 30f;

    private readonly Color _borderColor = new(1f, 1f, 1f, 0.2f);
    private readonly IReadOnlyList<ColumnWorker<TRow>> _columns;
    private readonly ITableContext? _context;
    private readonly IReadOnlyList<IRowFilter<TRow>>? _filters;
    private readonly List<TRow> _rows;

    private readonly List<float> _cachedColumnWidths = [];
    private readonly List<TRow> _cachedFilteredRows = [];
    private readonly List<float> _cachedRowYPositions = [];

    private bool _dirty = true;
    private Vector2 _cachedSize;
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
        _context = context;
        _filters = filters;
    }

    public void SetDirty() => _dirty = true;

    public void Draw(Rect r)
    {
        if (Event.current.type == EventType.Layout)
            return;

        if (_cachedSize != r.size)
        {
            _cachedSize = r.size;
            _dirty = true;
        }

        if (_dirty)
        {
            _dirty = false;
            RecacheFilteredRows();
            RecacheColumnWidths(r.width);
            RecacheRowYPositions();
        }

        // --- Header ---
        var headerRect = r.TakeTopPart(HeaderHeight);
        for (var colIndex = 0; colIndex < _columns.Count; ++colIndex)
            _columns[colIndex].DrawHeader(headerRect.TakeLeftPart(_cachedColumnWidths[colIndex]));

        using (new GUIColor(_borderColor))
            Verse.Widgets.DrawLineHorizontal(r.x, r.y, r.width);

        // --- Scroll view ---
        var contentHeight = _cachedFilteredRows.Count > 0 ? _cachedRowYPositions[^1] : UIUtility.ButtonHeight;
        var viewRect = new Rect(0f, 0f, r.width - UIUtility.ScrollBarWidth, contentHeight);

        Verse.Widgets.BeginScrollView(r, ref _scrollPosition, viewRect);

        var visibleTop = _scrollPosition.y;
        var visibleBottom = _scrollPosition.y + r.height;

        if (_cachedFilteredRows.Count == 0)
        {
            using (new GUIColor(ColoredText.SubtleGrayColor))
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(viewRect, "No results available.");
        }

        for (var rowIndex = 0; rowIndex < _cachedFilteredRows.Count; ++rowIndex)
        {
            var rowY = _cachedRowYPositions[rowIndex];

            if (rowY + DefaultRowHeight < visibleTop)
                continue;
            if (rowY > visibleBottom)
                break;

            var row = _cachedFilteredRows[rowIndex];
            var rowRect = new Rect(0f, rowY, viewRect.width, DefaultRowHeight);

            if (Selected != null && EqualityComparer<TRow>.Default.Equals(row, Selected))
                Verse.Widgets.DrawHighlightSelected(rowRect);
            else if (rowIndex % 2 == 1)
                Verse.Widgets.DrawLightHighlight(rowRect);

            if (Mouse.IsOver(rowRect))
                GUI.DrawTexture(rowRect, TexUI.HighlightTex);

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                Selected = row;
                SoundDefOf.Click.PlayOneShotOnCamera();
                Event.current.Use();
            }

            for (var colIndex = 0; colIndex < _columns.Count; ++colIndex)
            {
                var col = _columns[colIndex];
                var cellRect = rowRect.TakeLeftPart((int)_cachedColumnWidths[colIndex]);

                if (col is IContextColumn<TRow> ctxCol && _context != null)
                    ctxCol.DrawCell(cellRect, row, _context);
                else
                    col.DrawCell(cellRect, row);
            }
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

    private void RecacheRowYPositions()
    {
        _cachedRowYPositions.Clear();
        var y = 0f;
        for (var i = 0; i < _cachedFilteredRows.Count; i++)
        {
            _cachedRowYPositions.Add(y);
            y += DefaultRowHeight;
        }

        _cachedRowYPositions.Add(y); // sentinel: total content height
    }

    private void RecacheColumnWidths(float totalWidth)
    {
        _cachedColumnWidths.Clear();
        if (_columns.Count == 0) return;

        var available = totalWidth - UIUtility.ScrollBarWidth;
        var tree = new TaffyTree();
        var childIds = new List<NodeId>(_columns.Count);

        for (var i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            var style = new Style { flexGrow = 1f, flexShrink = 1f };
            style.size = style.size.MapWidth(_ => Dimension.Length(col.Width));
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