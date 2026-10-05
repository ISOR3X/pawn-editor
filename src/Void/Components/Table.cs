using System.Runtime.CompilerServices;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Verse.Sound;
using Void;
using Void.Taffy;
using static VoidComponents;
using Widgets = Verse.Widgets;

public static partial class VoidComponents
{
    private const float TableHeaderHeight = UIUtility.ButtonHeight;

    // Wide enough to cover any table; the scroll view clips the part that is not visible.
    // Workaround until Taffy implements Table (https://github.com/DioxusLabs/taffy/issues/467)
    private const float TableRowHoverWidth = 100000f;

    private static readonly Style TableHeaderTextStyle = new()
    {
        textAnchor = TextAnchor.MiddleLeft,
        wordWrap = false,
        minWidth = Dimension.Px(0f)
    };

    // Base of every cell: a flex row, so a bare Text is centred vertically and a Button or Icon keeps its size.
    // Columns touch (the grid has no gap, so row painting is continuous), hence the padding on the right.
    private static readonly Style TableCellBaseStyle = new()
    {
        display = TaffyDisplay.Flex,
        alignItems = TaffyAlignItems.Center,
        gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall)),
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(GenUI.GapSmall), Dimension.Px(0f),
            Dimension.Px(0f)),
        minWidth = Dimension.Px(0f)
    };

    private static readonly Style TableStickyStyle = new() { sticky = true };

    private static readonly StyleCache<int> TableMessageStyles = new(columns => new Style
    {
        gridColumn = new TaffyGridPlacement { span = (ushort)columns },
        textAnchor = TextAnchor.MiddleLeft,
        color = ColoredText.SubtleGrayColor,
        wordWrap = false
    });

    // Stands in for the rows that are not rendered: all columns wide and as many rows tall as it replaces.
    private static readonly StyleCache<(int columns, int rows)> TableSpacerStyles = new(k => new Style
    {
        gridColumn = new TaffyGridPlacement { span = (ushort)k.columns },
        gridRow = new TaffyGridPlacement { span = (ushort)k.rows }
    });

    private static readonly List<string> TableCellSuffixes = [];

    // Memo of the styles that depend on a column list. Keyed weakly on the list itself, so a list built
    // fresh every call just gets collected, and nothing is kept alive or stored on the tree.
    private static readonly ConditionalWeakTable<object, TableLayout> TableLayouts = [];

    private sealed class TableLayout(TaffyTrackSizingFunction[] tracks)
    {
        private readonly Dictionary<(float rowHeight, int visibleRows), Style> _roots = [];

        public int Columns => tracks.Length;

        /// <summary>
        ///     The scrolling grid. The header is its first row, so header and body share the column tracks.
        /// </summary>
        public Style Root(float rowHeight, int visibleRows)
        {
            if (_roots.TryGetValue((rowHeight, visibleRows), out var style)) return style;

            return _roots[(rowHeight, visibleRows)] = new Style
            {
                display = TaffyDisplay.Grid,
                gridTemplateColumns = tracks,
                gridTemplateRows = [TrackSizingFunction.Px(TableHeaderHeight)],
                gridAutoRows = [TrackSizingFunction.Px(rowHeight)],
                width = Dimension.Percent(1f),
                height = Dimension.Px(TableHeaderHeight + visibleRows * rowHeight),
                flexShrink = 0f,
                overflowX = TaffyOverflow.Hidden,
                overflowY = TaffyOverflow.Scroll
            };
        }
    }

    private static TableLayout GetTableLayout<TRow>(IReadOnlyList<TableColumn<TRow>> columns)
    {
        if (TableLayouts.TryGetValue(columns, out var layout)) return layout;

        var tracks = new TaffyTrackSizingFunction[columns.Count];
        for (var i = 0; i < tracks.Length; i++) tracks[i] = columns[i].Track;

        layout = new TableLayout(tracks);
        TableLayouts.Add(columns, layout);
        return layout;
    }

    // Cell keys are the row key plus this, so a cell keeps its node (and any state) while its row scrolls.
    private static string TableCellSuffix(int column)
    {
        for (var i = TableCellSuffixes.Count; i <= column; i++) TableCellSuffixes.Add(":" + i);
        return TableCellSuffixes[column];
    }

    /// <summary>
    ///     Paints one cell's slice of its row: selection or zebra, and hover. A row has no node of its own, so every
    ///     cell does this for itself, and hover looks at the row's whole band instead of only the cell.
    /// </summary>
    private static void DrawTableCell<TRow>(Rect r, int index, TRow item, Func<TRow, bool>? highlightRow,
        Action<TRow>? onRowClick)
    {
        if (highlightRow?.Invoke(item) ?? false) Widgets.DrawHighlightSelected(r);
        else if (index % 2 == 1) Widgets.DrawLightHighlight(r);

        if (Mouse.IsOver(new Rect(0f, r.y, TableRowHoverWidth, r.height))) Widgets.DrawHighlight(r);

        // Only the cell under the mouse reports the click, so a row fires once.
        if (onRowClick != null
            && Event.current.type == EventType.MouseDown
            && Event.current.button == 0
            && Mouse.IsOver(r))
            onRowClick(item);
    }

    /// <summary>
    ///     A table column. <see cref="Track" /> is its width in the table's grid, so header and body cells line up
    ///     whatever they contain. Build columns once and keep them (a static field, say): the styles are made once
    ///     and used as-is every frame.
    ///     Prefer <see cref="Fixed" />, <see cref="Flexible" /> and <see cref="Auto" />.
    /// </summary>
    public sealed record TableColumn<TRow>(string Header, Style Style, Action<UIBranch, TRow> Cell)
    {
        /// <summary>
        ///     The column's grid track. Auto when not set.
        /// </summary>
        public TaffyTrackSizingFunction Track { get; init; } = TrackSizingFunction.AutoTrack();

        /// <summary>
        ///     <see cref="Style" /> of the header cell, which is pinned while the body scrolls.
        /// </summary>
        public Style HeaderStyle { get; } = Style.Merge(TableStickyStyle);

        /// <summary>
        ///     How to order rows by this column. Optional: with it, and a table given <c>sorting</c> and
        ///     <c>onSortingChange</c>, the header sorts. Sorted columns need distinct header text, which is their id.
        /// </summary>
        public Comparison<TRow>? Sort { get; init; }

        /// <summary>
        ///     Draws the header's content in place of the plain label, or of the default sorting header.
        /// </summary>
        public Action<UIBranch, TableHeader>? DrawHeader { get; init; }

        // These two are deliberately not a generic By<TKey>. EditCompileReload (Debug builds) emits a broken
        // ECR.Ptrs field reference for a generic method inside a generic type, and Mono hard-crashes when it
        // first compiles it. Boxing a key costs nothing here: the sorted list is cached by the caller.

        /// <summary>
        ///     Builds a <see cref="Sort" /> from a key. Any <see cref="IComparable" /> works, nulls first.
        ///     Use <see cref="ByText" /> for strings, which sorts by an explicit string comparison.
        /// </summary>
        public static Comparison<TRow> By(Func<TRow, IComparable?> key)
        {
            return (a, b) =>
            {
                var x = key(a);
                var y = key(b);
                if (x == null) return y == null ? 0 : -1;
                return y == null ? 1 : x.CompareTo(y);
            };
        }

        /// <summary>
        ///     Builds a <see cref="Sort" /> from a text key, case-insensitive unless told otherwise.
        /// </summary>
        public static Comparison<TRow> ByText(Func<TRow, string?> key,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return (a, b) => string.Compare(key(a), key(b), comparison);
        }

        /// <summary>
        ///     A column of a fixed width in pixels.
        /// </summary>
        public static TableColumn<TRow> Fixed(float width, string header, Action<UIBranch, TRow> cell,
            Style? style = null, Comparison<TRow>? sort = null, Action<UIBranch, TableHeader>? drawHeader = null)
        {
            return new TableColumn<TRow>(header, (style ?? new Style()).Merge(TableCellBaseStyle), cell)
            {
                Track = TrackSizingFunction.Px(width),
                Sort = sort,
                DrawHeader = drawHeader
            };
        }

        /// <summary>
        ///     A column that takes the space left over by the others. Several flexible columns share it in
        ///     proportion to <paramref name="grow" />. Its width does not depend on its content.
        /// </summary>
        public static TableColumn<TRow> Flexible(string header, Action<UIBranch, TRow> cell, float grow = 1f,
            Style? style = null, Comparison<TRow>? sort = null, Action<UIBranch, TableHeader>? drawHeader = null)
        {
            // minmax(0, Nfr) instead of the default minmax(auto, Nfr): with only some rows rendered, the auto
            // minimum would resize the column as different content scrolls into view.
            return new TableColumn<TRow>(header, (style ?? new Style()).Merge(TableCellBaseStyle), cell)
            {
                Track = TrackSizingFunction.MinMax(Dimension.Px(0f), Dimension.Fr(grow)),
                Sort = sort,
                DrawHeader = drawHeader
            };
        }

        /// <summary>
        ///     A column as wide as its widest rendered cell, such as one holding a button. Only the rows that are
        ///     rendered count, so with different content per row the width can change as rows scroll in.
        /// </summary>
        public static TableColumn<TRow> Auto(string header, Action<UIBranch, TRow> cell, Style? style = null,
            Comparison<TRow>? sort = null, Action<UIBranch, TableHeader>? drawHeader = null)
        {
            return new TableColumn<TRow>(header, (style ?? new Style()).Merge(TableCellBaseStyle), cell)
            {
                Track = TrackSizingFunction.AutoTrack(),
                Sort = sort,
                DrawHeader = drawHeader
            };
        }
    }

    extension(UIBranch branch)
    {
        /// <summary>
        ///     Table: one scrolling grid. The header cells are its first row, pinned with <see cref="Style.sticky" />
        ///     so only the body scrolls, and the cells of every row follow in row order, so all of them share the
        ///     column tracks. Rows have a fixed <paramref name="rowHeight" /> and are virtualized: only those in
        ///     view (plus <paramref name="overscan" />) exist, with a spacer standing in for the rest, and they are
        ///     destroyed when scrolled out, so cells should not carry state that <paramref name="rowKey" /> does not
        ///     identify. Rows have no node of their own: each cell paints its slice of the zebra, hover and
        ///     selection.
        ///     Sorting is optional: with <paramref name="sorting" /> and <paramref name="onSortingChange" />, headers of
        ///     columns that have a <see cref="TableColumn{TRow}.Sort" /> can be clicked and the rows are sorted here, in
        ///     the order of <paramref name="sorting" />. Like <c>v-model:sorting</c> the value is the caller's, and each
        ///     change is reported, never stored. Set <paramref name="manualSorting" /> to sort the rows yourself and use
        ///     the sorting only for the headers.
        ///     <paramref name="onRowClick" /> fires on mouse-down without consuming the event, so a button
        ///     inside a cell will fire as well.
        /// </summary>
        public void Table<TRow>(
            IReadOnlyList<TRow> rows,
            IReadOnlyList<TableColumn<TRow>> columns,
            float rowHeight = UIUtility.ButtonHeight,
            int? maxRowsVisibleAtOnce = null,
            Func<TRow, string>? rowKey = null,
            Func<TRow, bool>? highlightRow = null,
            Action<TRow>? onRowClick = null,
            int overscan = 2,
            TableSorting? sorting = null,
            Action<TableSorting?>? onSortingChange = null,
            bool manualSorting = false,
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);

            var layout = GetTableLayout(columns);
            var view = manualSorting || onSortingChange == null ? rows : SortTableRows(rows, columns, sorting);
            var count = view.Count;
            var visibleRows = Mathf.Clamp(count, 1, maxRowsVisibleAtOnce ?? Math.Max(count, 1));
            var root = layout.Root(rowHeight, visibleRows);

            branch.Div(grid =>
            {
                for (var c = 0; c < columns.Count; c++)
                {
                    var column = columns[c];
                    grid.Div(cell => DrawTableHeader(cell, column, sorting, onSortingChange),
                        style: column.HeaderStyle, id: column.Header);
                }

                if (count == 0)
                {
                    grid.Text("No results available.", TableMessageStyles.Get(layout.Columns), "empty");
                    return;
                }

                // Written by DrawNode during the previous pass; on the very first build the declared
                // height is exact. pos is the scroll offset of the body, which starts below the header.
                var scroll = grid.GetState<ScrollState>("scroll");
                var viewTop = scroll?.pos.y ?? 0f;
                var viewHeight = scroll is { VisibleRect.height: > 0f } ? scroll.VisibleRect.height : visibleRows * rowHeight;

                var first = Mathf.Max(0, Mathf.FloorToInt(viewTop / rowHeight) - overscan);
                var last = Mathf.Min(count - 1, Mathf.CeilToInt((viewTop + viewHeight) / rowHeight) + overscan);

                if (first > 0) grid.Div(style: TableSpacerStyles.Get((layout.Columns, first)), id: "top");

                for (var i = first; i <= last; i++)
                {
                    var item = view[i];
                    var index = i;
                    var rowId = rowKey?.Invoke(item) ?? i.ToString();

                    // One painter per row, shared by its cells.
                    Action<Rect> paint = r => DrawTableCell(r, index, item, highlightRow, onRowClick);

                    for (var c = 0; c < columns.Count; c++)
                    {
                        var column = columns[c];
                        grid.Div(cell => column.Cell(cell, item), draw: paint, style: column.Style,
                            id: rowId + TableCellSuffix(c));
                    }
                }

                if (last < count - 1)
                    grid.Div(style: TableSpacerStyles.Get((layout.Columns, count - 1 - last)), id: "bottom");
            }, draw: r => { Widgets.DrawLineHorizontal(r.x, r.y + TableHeaderHeight, r.width, PawnTable.BorderColor); },
            style: style == null ? root : style.Merge(root), id: key);
        }

    }

    /// <summary>
    ///     The column rows are sorted by (its header text) and the direction, like TanStack's <c>[{ id, desc }]</c>
    ///     for a single column.
    /// </summary>
    public readonly record struct TableSorting(string Id, bool Descending);

    public enum TableSortDirection
    {
        None,
        Ascending,
        Descending
    }

    /// <summary>
    ///     What a column's <see cref="TableColumn{TRow}.DrawHeader" /> sees: its label, whether and how it is sorted,
    ///     and a way to sort. Made fresh every frame, so the column itself stays a static definition.
    /// </summary>
    public readonly struct TableHeader
    {
        private readonly string _id;
        private readonly Action<TableSorting?>? _onSorting;
        private readonly TableSorting? _sorting;

        public TableHeader(string label, TableSorting? sorting, Action<TableSorting?>? onSorting)
        {
            _id = label;
            _sorting = sorting;
            _onSorting = onSorting;
            Label = label;
        }

        public string Label { get; }

        /// <summary>
        ///     Whether clicking sorts: the column has a sort and the table was given <c>onSortingChange</c>.
        /// </summary>
        public bool Sortable => _onSorting != null;

        public TableSortDirection Sorted =>
            _sorting is { } s && s.Id == _id
                ? s.Descending ? TableSortDirection.Descending : TableSortDirection.Ascending
                : TableSortDirection.None;

        /// <summary>
        ///     Cycles ascending, descending, off (descending, ascending, off when <paramref name="descendingFirst" />).
        ///     A click on another column starts a new cycle.
        /// </summary>
        public void ToggleSorting(bool descendingFirst = false)
        {
            if (_onSorting == null) return;

            if (_sorting is not { } s || s.Id != _id)
                _onSorting(new TableSorting(_id, descendingFirst));
            else
                _onSorting(s.Descending == descendingFirst ? new TableSorting(_id, !descendingFirst) : null);
        }
    }

    private sealed class TableSortCache
    {
        public int Count;
        public bool Descending;
        public string? Id;
        public object? Result;
    }

    // Weak, keyed on the caller's rows list: nothing is kept alive, and nothing lives on the tree.
    private static readonly ConditionalWeakTable<object, TableSortCache> TableSortCaches = new();

    // Fills the header cell; the arrow is drawn over it, so it does not move the label.
    private static readonly Style SortableHeaderStyle = new()
    {
        display = TaffyDisplay.Flex,
        alignItems = TaffyAlignItems.Center,
        alignSelf = TaffyAlignItems.Stretch,
        flexGrow = 1f,
        minWidth = Dimension.Px(0f)
    };

    /// <summary>
    ///     <paramref name="rows" /> in the order of <paramref name="sorting" />, stable, and only re-sorted when the
    ///     list, its count or the sorting changed. A list edited in place keeps its reference and count, so that is
    ///     not noticed; give the table a new list instead.
    /// </summary>
    private static IReadOnlyList<TRow> SortTableRows<TRow>(IReadOnlyList<TRow> rows,
        IReadOnlyList<TableColumn<TRow>> columns, TableSorting? sorting)
    {
        if (sorting is not { } s) return rows;

        Comparison<TRow>? compare = null;
        for (var i = 0; i < columns.Count; i++)
            if (columns[i].Header == s.Id)
            {
                compare = columns[i].Sort;
                break;
            }

        if (compare == null) return rows;

        var cache = TableSortCaches.GetOrCreateValue(rows);
        if (cache.Result is List<TRow> cached && cache.Count == rows.Count && cache.Id == s.Id
            && cache.Descending == s.Descending)
            return cached;

        var comparer = Comparer<TRow>.Create(compare);
        var sorted = s.Descending
            ? new List<TRow>(rows.OrderByDescending(r => r, comparer))
            : new List<TRow>(rows.OrderBy(r => r, comparer));

        cache.Result = sorted;
        cache.Count = rows.Count;
        cache.Id = s.Id;
        cache.Descending = s.Descending;
        return sorted;
    }

    private static void DrawTableHeader<TRow>(UIBranch cell, TableColumn<TRow> column, TableSorting? sorting,
        Action<TableSorting?>? onSorting)
    {
        var header = new TableHeader(column.Header, sorting, column.Sort != null ? onSorting : null);

        if (column.DrawHeader != null) column.DrawHeader(cell, header);
        else DrawDefaultTableHeader(cell, header);
    }

    /// <summary>
    ///     Default header: the label, and for a sortable column a hover highlight, the arrow and the click.
    ///     Left click sorts ascending first, right click descending first.
    /// </summary>
    private static void DrawDefaultTableHeader(UIBranch cell, TableHeader header)
    {
        if (!header.Sortable)
        {
            cell.Text(header.Label, TableHeaderTextStyle);
            return;
        }

        cell.Div(b => b.Text(header.Label, TableHeaderTextStyle), draw: r =>
        {
            Widgets.DrawHighlightIfMouseover(r);

            if (header.Sorted != TableSortDirection.None)
            {
                var icon = header.Sorted == TableSortDirection.Descending
                    ? PawnColumnWorker.SortingDescendingIcon
                    : PawnColumnWorker.SortingIcon;
                GUI.DrawTexture(new Rect(r.xMax - icon.width - 1f, r.yMax - icon.height - 1f, icon.width, icon.height),
                    icon);
            }

            if (Event.current.type != EventType.MouseDown || !Mouse.IsOver(r)) return;
            var button = Event.current.button;
            if (button > 1) return;

            header.ToggleSorting(button == 1);
            (button == 0 ? SoundDefOf.Tick_Low : SoundDefOf.Tick_High).PlayOneShotOnCamera();
            Event.current.Use();
        }, style: SortableHeaderStyle);
    }
}
