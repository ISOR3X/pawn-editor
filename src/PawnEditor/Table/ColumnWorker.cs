using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor.Table;

public abstract class ColumnWorker<TRow>
{
    public abstract TrackSizingFunction TrackSize { get; }
    public abstract void DrawHeader(Rect r);
    public abstract void DrawCell(TaffyBuilder grid, TRow row);

    public virtual bool Sortable => false;
    public virtual int Compare(TRow a, TRow b) => 0;

    public static ColumnWorker<TRow> Create(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow> drawCell,
        string? header = null,
        Func<TRow, TRow, int>? compare = null)
        => new DelegateColumn(trackSize, drawCell, header, compare);

    public static ColumnWorker<TRow> Create<TContext>(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow, TContext> drawCell,
        string? header = null,
        Func<TRow, TRow, int>? compare = null)
        where TContext : ITableContext
        => new DelegateContextColumn<TContext>(trackSize, drawCell, header, compare);

    /// <summary>
    /// Creates a sortable string column. The text projection is used both for rendering and
    /// for case-insensitive alphabetical sorting.
    /// </summary>
    public static ColumnWorker<TRow> CreateText(
        TrackSizingFunction trackSize,
        Func<TRow, string> getText,
        string? header = null,
        Color? color = null)
        => new TextColumn(trackSize, getText, header, color);

    private sealed class DelegateColumn(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow> drawCell,
        string? header,
        Func<TRow, TRow, int>? compare)
        : ColumnWorker<TRow>
    {
        public override TrackSizingFunction TrackSize => trackSize;
        public override bool Sortable => compare != null;
        public override int Compare(TRow a, TRow b) => compare?.Invoke(a, b) ?? 0;

        public override void DrawHeader(Rect r)
        {
            if (header == null) return;
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(r, (TaggedString)header);
        }

        public override void DrawCell(TaffyBuilder grid, TRow row) => drawCell(grid, row);
    }

    private sealed class DelegateContextColumn<TContext>(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow, TContext> drawCell,
        string? header,
        Func<TRow, TRow, int>? compare)
        : ColumnWorker<TRow, TContext>
        where TContext : ITableContext
    {
        public override TrackSizingFunction TrackSize => trackSize;
        public override bool Sortable => compare != null;
        public override int Compare(TRow a, TRow b) => compare?.Invoke(a, b) ?? 0;

        public override void DrawHeader(Rect r)
        {
            if (header == null) return;
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(r, (TaggedString)header);
        }

        protected override void DrawCell(TaffyBuilder grid, TRow row, TContext ctx) => drawCell(grid, row, ctx);
    }

    private sealed class TextColumn(
        TrackSizingFunction trackSize,
        Func<TRow, string> getText,
        string? header,
        Color? color)
        : ColumnWorker<TRow>
    {
        public override TrackSizingFunction TrackSize => trackSize;
        public override bool Sortable => true;

        public override int Compare(TRow a, TRow b)
            => string.Compare(getText(a), getText(b), StringComparison.CurrentCultureIgnoreCase);

        public override void DrawHeader(Rect r)
        {
            if (header == null) return;
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(r, (TaggedString)header);
        }

        public override void DrawCell(TaffyBuilder grid, TRow row)
        {
            if (color.HasValue)
                grid.Text(getText(row), color: color.Value);
            else
                grid.Text(getText(row));
        }
    }
}

internal interface IContextColumn<in TRow>
{
    void DrawCell(TaffyBuilder grid, TRow row, ITableContext ctx);
}

public abstract class ColumnWorker<TRow, TContext> : ColumnWorker<TRow>, IContextColumn<TRow>
    where TContext : ITableContext
{
    protected abstract void DrawCell(TaffyBuilder grid, TRow row, TContext ctx);

    public override void DrawCell(TaffyBuilder grid, TRow row)
    {
    }

    public void DrawCell(TaffyBuilder grid, TRow row, ITableContext ctx)
        => DrawCell(grid, row, (TContext)ctx);
}
