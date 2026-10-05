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

    // The header sits outside the scrolling body, so while the body shows a scrollbar the header leaves the same
    // gutter free and the columns line up. Both cases set the padding, as Style.Push keeps old values over null.
    private static readonly StyleCache<bool> TableHeaderStyles = new(scrolls => new Style
    {
        display = TaffyDisplay.Flex,
        height = Dimension.Px(TableHeaderHeight),
        flexShrink = 0f,
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(scrolls ? Style.ScrollbarGutter : 0f),
            Dimension.Px(0f), Dimension.Px(0f))
    });

    // Only scrolls when the rows do not fit, so a short table has no empty scrollbar gutter.
    private static readonly StyleCache<(float height, bool scrolls)> TableBodyStyles = new(k => new Style
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        height = Dimension.Px(k.height),
        flexShrink = 0f,
        overflowX = TaffyOverflow.Hidden,
        overflowY = k.scrolls ? TaffyOverflow.Scroll : TaffyOverflow.Hidden
    });

    private static readonly StyleCache<float> TableRowStyles = new(height => new Style
    {
        display = TaffyDisplay.Flex,
        alignItems = TaffyAlignItems.Stretch,
        height = Dimension.Px(height),
        flexShrink = 0f
    });

    // Stands in for the rows that are not rendered.
    private static readonly StyleCache<float> TableSpacerStyles = new(height => new Style
    {
        height = Dimension.Px(height),
        flexShrink = 0f
    });

    private static readonly Style TableMessageStyle = new()
    {
        textAnchor = TextAnchor.MiddleLeft,
        color = ColoredText.SubtleGrayColor,
        wordWrap = false,
        alignSelf = TaffyAlignItems.Center
    };

    // Fills the header cell; the arrow is drawn over it, so it does not move the label.
    private static readonly Style SortableHeaderStyle = new()
    {
        display = TaffyDisplay.Flex,
        alignItems = TaffyAlignItems.Center,
        alignSelf = TaffyAlignItems.Stretch,
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
    ///     How wide a column is. Every column declares it, so the header and each row, which are separate flex
    ///     rows, line up without measuring each other, and nothing moves as rows scroll in.
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
    ///     A table column. Build columns once and keep them (a static field, say): the cell style is made on
    ///     first use and reused every frame.
    /// </summary>
    public sealed record TableColumn<TRow>(string Header, Action<UIBranch, TRow> Cell)
    {
        public TableColumnWidth Width { get; init; } = TableColumnWidth.Fill();

        /// <summary>
        ///     Merged over the cell's base and width. Used for the header cell too, so alignment carries over.
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
        public Action<UIBranch, TableHeader>? DrawHeader { get; init; }

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
    }

    extension(UIBranch branch)
    {
        /// <summary>
        ///     Table: a header row above a scrolling body of rows. Every row is a flex row whose cells take their
        ///     width from <see cref="TableColumn{TRow}.Width" />, so header and rows line up. Rows have a fixed
        ///     <paramref name="rowHeight" /> and are virtualized: only those in view (plus <paramref name="overscan" />)
        ///     exist, with spacers standing in for the rest, and they are destroyed when scrolled out, so cells should
        ///     not carry state that <paramref name="rowKey" /> does not identify. Each row paints its zebra, hover and
        ///     selection.
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
                            header.Div(cell => DrawTableHeader(cell, column, info),
                                style: GetTableCellStyle(column), id: TableCellId(c));
                        }
                    }, draw: r => Widgets.DrawLineHorizontal(r.x, r.yMax, r.width, PawnTable.BorderColor),
                    style: TableHeaderStyles.Get(scrolls), id: "header");

                table.Div(body =>
                    {
                        if (count == 0)
                        {
                            body.Div(m => m.Text("No results available.", TableMessageStyle),
                                style: TableRowStyles.Get(rowHeight), id: "empty");
                            return;
                        }

                        // Written by DrawNode during the previous pass; on the very first build the declared height
                        // is exact. Only read while scrolling: a body that stopped scrolling keeps its old state.
                        var scroll = scrolls ? body.GetState<ScrollState>("scroll") : null;
                        var first = 0;
                        var last = count - 1;
                        if (scroll != null)
                        {
                            var viewHeight = scroll.VisibleRect.height > 0f
                                ? scroll.VisibleRect.height
                                : visibleRows * rowHeight;
                            first = Mathf.Max(0, Mathf.FloorToInt(scroll.pos.y / rowHeight) - overscan);
                            last = Mathf.Min(count - 1,
                                Mathf.CeilToInt((scroll.pos.y + viewHeight) / rowHeight) + overscan);
                        }
                        else if (scrolls)
                        {
                            last = Mathf.Min(count - 1, visibleRows + overscan);
                        }

                        if (first > 0) body.Div(style: TableSpacerStyles.Get(first * rowHeight), id: "top");

                        for (var i = first; i <= last; i++)
                        {
                            var item = rows[i];
                            var index = i;
                            body.Div(row =>
                                {
                                    for (var c = 0; c < columns.Count; c++)
                                    {
                                        var column = columns[c];
                                        row.Div(cell => column.Cell(cell, item), style: GetTableCellStyle(column),
                                            id: TableCellId(c));
                                    }
                                }, draw: r => DrawTableRow(r, index, item, scroll, highlightRow, onRowClick),
                                style: TableRowStyles.Get(rowHeight), id: rowKey?.Invoke(item) ?? i.ToString());
                        }

                        if (last < count - 1)
                            body.Div(style: TableSpacerStyles.Get((count - 1 - last) * rowHeight), id: "bottom");
                    }, style: TableBodyStyles.Get((visibleRows * rowHeight, scrolls)), id: "body");
            }, style: style == null ? TableRootStyle : style.Merge(TableRootStyle), id: key);
        }
    }

    /// <summary>
    ///     Paints a row: selection or zebra, and hover; and reports the click.
    /// </summary>
    private static void DrawTableRow<TRow>(Rect r, int index, TRow item, ScrollState? scroll,
        Func<TRow, bool>? highlightRow, Action<TRow>? onRowClick)
    {
        if (highlightRow?.Invoke(item) ?? false) Widgets.DrawHighlightSelected(r);
        else if (index % 2 == 1) Widgets.DrawLightHighlight(r);

        // Overscan rows lie outside the visible part of the body, under the header say, where the mouse is still
        // reported in the body's coordinates.
        if (!Mouse.IsOver(r) || (scroll != null && !scroll.VisibleRect.Contains(Event.current.mousePosition)))
            return;

        Widgets.DrawHighlight(r);
        if (onRowClick != null && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            onRowClick(item);
    }

    private static void DrawTableHeader<TRow>(UIBranch cell, TableColumn<TRow> column, TableHeader header)
    {
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
