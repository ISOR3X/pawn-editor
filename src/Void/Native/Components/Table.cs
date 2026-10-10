using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Void.Native;

public static partial class Components
{
    private const float TableHeaderHeight = UIUtility.ButtonHeight;

    private static readonly Style TableHeaderTextStyle = new()
    {
        textAnchor = TextAnchor.MiddleLeft,
        wordWrap = false,
        minWidth = Dimension.Px(0f)
    };

    // Base of every cell: a flex row, so a bare Text is centred vertically and a Button or Icon keeps its size.
    // Cells touch (rows have no gap, so row painting is continuous), hence the padding on the right.
    private static readonly Style TableCellBaseStyle = new()
    {
        display = TaffyDisplay.Flex,
        alignItems = TaffyAlignItems.Center,
        gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall)),
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(GenUI.GapSmall), Dimension.Px(0f),
            Dimension.Px(0f))
    };

    private static readonly Style TableRootStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        width = Dimension.Percent(1f),
        flexShrink = 0f
    };

    // The header sits outside the scrolling list, so while the list shows a scrollbar the header leaves the same
    // gutter free and the columns line up.
    private static readonly StyleCache<bool> TableHeaderStyles = new(scrolls => new Style
    {
        display = TaffyDisplay.Flex,
        height = Dimension.Px(TableHeaderHeight),
        flexShrink = 0f,
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(scrolls ? Style.ScrollbarGutter : 0f),
            Dimension.Px(0f), Dimension.Px(0f))
    });

    // A list scrolls by default. A table whose rows all fit does not, so it has no empty scrollbar gutter.
    private static readonly Style TableListFitsStyle = new() { overflowY = TaffyOverflow.Hidden };

    // Fills the header cell; the arrow is drawn over it, so it does not move the label.
    private static readonly Style SortableHeaderStyle = new()
    {
        display = TaffyDisplay.Flex,
        // alignItems = TaffyAlignItems.Center,
        // alignSelf = TaffyAlignItems.Stretch,
        padding = new TaffyEdges(Dimension.Px(0f)),
        flexGrow = 1f,
        minWidth = Dimension.Px(0f)
    };

    // Cell style per column, made on first use. Keyed weakly on the column itself, so a column made by
    // `with` gets its own, and nothing is kept alive or stored on the tree.
    private static readonly ConditionalWeakTable<object, Style> TableCellStyles = new();

    private static readonly List<string> TableCellIds = [];

    private static Style GetTableCellStyle<TRow>(TableColumn<TRow> column)
    {
        if (TableCellStyles.TryGetValue(column, out var style)) return style;

        var width = column.Width;
        var sized = new Style
        {
            width = width.Grow > 0f ? Dimension.Px(0f) : Dimension.Px(width.Px),
            minWidth = Dimension.Px(0f),
            flexGrow = width.Grow,
            flexShrink = width.Grow > 0f ? 1f : 0f
        }.Merge(TableCellBaseStyle);

        style = column.Style?.Merge(sized) ?? sized;
        TableCellStyles.Add(column, style);
        return style;
    }

    // Cells are keyed by column under their row, so a cell keeps its node (and any state) while its row scrolls.
    private static string TableCellId(int column)
    {
        for (var i = TableCellIds.Count; i <= column; i++) TableCellIds.Add("c" + i);
        return TableCellIds[column];
    }

    /// <summary>
    ///     How wide a column is. Every column declares it, so the header and every row, which are laid out on their own,
    ///     line up without measuring each other, and nothing moves as rows scroll in.
    /// </summary>
    public readonly record struct TableColumnWidth(float Px, float Grow)
    {
        /// <summary>
        ///     A fixed width in pixels.
        /// </summary>
        public static TableColumnWidth Fixed(float px) => new(px, 0f);

        /// <summary>
        ///     A share of the space left over by the fixed columns, in proportion to <paramref name="grow" />.
        /// </summary>
        public static TableColumnWidth Fill(float grow = 1f) => new(0f, grow);
    }

    /// <summary>
    ///     A table column. Build columns once and keep them (a static field, say): the styles are made on first use
    ///     and reused every frame.
    /// </summary>
    public sealed record TableColumn<TRow>(string Header, Action<UIBranch, TRow> Cell)
    {
        public TableColumnWidth Width { get; init; } = TableColumnWidth.Fill();

        /// <summary>
        ///     Merged over the cell's base. Used for the header cell too, so alignment carries over.
        /// </summary>
        public Style? Style { get; init; }

        /// <summary>
        ///     How to order rows by this column. With it, and a table given <c>onSortingChange</c>, the header
        ///     sorts. The table only shows and reports the sorting: order the rows with
        ///     <see cref="TableSorting.Apply{TRow}" />.
        /// </summary>
        public Comparison<TRow>? Sort { get; init; }

        /// <summary>
        ///     Draws the header's content in place of the plain label, or of the default sorting header.
        /// </summary>
        public Action<UIBranch, TableHeader> DrawHeader { get; init; } = TableHeader.DrawDefaultTableHeader;

        // These two are deliberately not a generic By<TKey>. EditCompileReload (Debug builds) emits a broken
        // ECR.Ptrs field reference for a generic method inside a generic type, and Mono hard-crashes when it
        // first compiles it. Boxing a key costs nothing here: rows are only sorted when the view is rebuilt.

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
    }

    /// <summary>
    ///     The column rows are sorted by (its index) and the direction. The caller owns it, like
    ///     <c>v-model:sorting</c>: the table shows it in the headers and reports changes, but never sorts.
    /// </summary>
    public readonly record struct TableSorting(int Column, bool Descending)
    {
        /// <summary>
        ///     <paramref name="rows" /> as a new list in this order, stable. Call it where the view is rebuilt
        ///     (after filtering, say), not every frame.
        /// </summary>
        public List<TRow> Apply<TRow>(IEnumerable<TRow> rows, IReadOnlyList<TableColumn<TRow>> columns)
        {
            if (columns[Column].Sort is not { } sort) return [.. rows];

            var comparer = Comparer<TRow>.Create(sort);
            return Descending ? [.. rows.OrderByDescending(r => r, comparer)] : [.. rows.OrderBy(r => r, comparer)];
        }
    }

    public enum TableSortDirection
    {
        None,
        Ascending,
        Descending
    }

    /// <summary>
    ///     What a column's <see cref="TableColumn{TRow}.DrawHeader" /> sees: its label, whether and how it is
    ///     sorted, and a way to sort. Made fresh every frame, so the column itself stays a static definition.
    /// </summary>
    public readonly struct TableHeader(string label, int column, TableSorting? sorting,
        Action<TableSorting?>? onSortingChange)
    {
        public string Label => label;

        /// <summary>
        ///     Whether clicking sorts: the column has a sort and the table was given <c>onSortingChange</c>.
        /// </summary>
        public bool Sortable => onSortingChange != null;

        public TableSortDirection Sorted =>
            sorting is { } s && s.Column == column
                ? s.Descending ? TableSortDirection.Descending : TableSortDirection.Ascending
                : TableSortDirection.None;

        /// <summary>
        ///     Cycles ascending, descending, off (descending, ascending, off when <paramref name="descendingFirst" />).
        ///     A click on another column starts a new cycle.
        /// </summary>
        public void ToggleSorting(bool descendingFirst = false)
        {
            if (onSortingChange == null) return;

            if (sorting is not { } s || s.Column != column)
                onSortingChange(new TableSorting(column, descendingFirst));
            else
                onSortingChange(s.Descending == descendingFirst ? new TableSorting(column, !descendingFirst) : null);
        }

        public static void DrawDefaultTableHeader(UIBranch cell, TableHeader header)
        {
            Texture2D icon = null!;
            if (header.Sorted != TableSortDirection.None)
                icon = header.Sorted == TableSortDirection.Descending ? TexUI.ArrowDown : TexUI.ArrowUp;
            cell.Button(label: header.Label, icon: icon, onClick: _ => header.ToggleSorting(), variant: ButtonVariant.Ghost, disabled: !header.Sortable, style: SortableHeaderStyle);
        }
    }

    extension(UIBranch branch)
    {
        /// <summary>
        ///     Table: a header above a <see cref="List{T}" /> of rows. Like in HTML, a row is a node of its own that the
        ///     cells are nested in, so it can be painted as a whole: it paints its zebra, hover and selection. Every row
        ///     is a flex row whose cells take their width from <see cref="TableColumn{TRow}.Width" />, so header and rows
        ///     line up.
        ///     Rows have a fixed <paramref name="rowHeight" /> and are virtualized by the list: only those in view (plus
        ///     <paramref name="overscan" />) exist, and they are destroyed when scrolled out, so cells should not carry
        ///     state that <paramref name="rowKey" /> does not identify.
        ///     Sorting is optional: with <paramref name="onSortingChange" />, headers of columns that have a
        ///     <see cref="TableColumn{TRow}.Sort" /> can be clicked. <paramref name="rows" /> are drawn in the order
        ///     given; sort them yourself with <see cref="TableSorting.Apply{TRow}" />.
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
            Style? style = null,
            string? id = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            var key = UIBranch.ResolveKey(id, file, line);

            var count = rows.Count;
            var visibleRows = Mathf.Clamp(count, 1, maxRowsVisibleAtOnce ?? Math.Max(count, 1));
            var scrolls = count > visibleRows;

            branch.Div(table =>
            {
                table.Div(header =>
                    {
                        for (var c = 0; c < columns.Count; c++)
                        {
                            var column = columns[c];
                            var info = new TableHeader(column.Header, c, sorting,
                                column.Sort != null ? onSortingChange : null);
                            header.Div(cell => column.DrawHeader(cell, info),
                                style: GetTableCellStyle(column), id: TableCellId(c));
                        }
                    }, draw: r => Verse.Widgets.DrawLineHorizontal(r.x, r.yMax, r.width, PawnTable.BorderColor),
                    style: TableHeaderStyles.Get(scrolls), id: "header");

                table.List(rows, (row, item) =>
                    {
                        for (var c = 0; c < columns.Count; c++)
                        {
                            var column = columns[c];
                            row.Div(cell => column.Cell(cell, item), style: GetTableCellStyle(column),
                                id: TableCellId(c));
                        }
                    }, rowHeight, visibleRows, rowKey, overscan,
                    (item, index) => r => DrawTableRow(r, index, item, highlightRow, onRowClick),
                    scrolls ? null : TableListFitsStyle, "rows");
            }, style: style == null ? TableRootStyle : style.Merge(TableRootStyle), id: key);
        }
    }

    /// <summary>
    ///     Paints a row: selection or zebra, and hover; and reports the click.
    /// </summary>
    private static void DrawTableRow<TRow>(Rect r, int index, TRow item,
        Func<TRow, bool>? highlightRow, Action<TRow>? onRowClick)
    {
        if (highlightRow?.Invoke(item) ?? false) Verse.Widgets.DrawHighlightSelected(r);
        else if (index % 2 == 1) Verse.Widgets.DrawLightHighlight(r);

        Verse.Widgets.DrawHighlightIfMouseover(r);
        onRowClick?.Invoke(item);
    }
}
