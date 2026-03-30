using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor.Table;

public abstract class ColumnWorker<TRow>
{
    public abstract TrackSizingFunction TrackSize { get; }
    public abstract void DrawHeader(Rect r);
    public abstract void DrawCell(TaffyBuilder grid, TRow row);

    public static ColumnWorker<TRow> Create(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow> drawCell,
        string? header = null)
        => new DelegateColumn(trackSize, drawCell, header);

    public static ColumnWorker<TRow> Create<TContext>(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow, TContext> drawCell,
        string? header = null)
        where TContext : ITableContext
        => new DelegateContextColumn<TContext>(trackSize, drawCell, header);

    private sealed class DelegateColumn(TrackSizingFunction trackSize, Action<TaffyBuilder, TRow> drawCell, string? header)
        : ColumnWorker<TRow>
    {
        public override TrackSizingFunction TrackSize => trackSize;

        public override void DrawHeader(Rect r)
        {
            if (header == null) return;
            using (new TextBlock(TextAnchor.MiddleCenter))
                Verse.Widgets.Label(r, (TaggedString)header);
        }

        public override void DrawCell(TaffyBuilder grid, TRow row) => drawCell(grid, row);
    }

    private sealed class DelegateContextColumn<TContext>(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow, TContext> drawCell,
        string? header)
        : ColumnWorker<TRow, TContext>
        where TContext : ITableContext
    {
        public override TrackSizingFunction TrackSize => trackSize;

        public override void DrawHeader(Rect r)
        {
            if (header == null) return;
            using (new TextBlock(TextAnchor.MiddleCenter))
                Verse.Widgets.Label(r, (TaggedString)header);
        }

        protected override void DrawCell(TaffyBuilder grid, TRow row, TContext ctx) => drawCell(grid, row, ctx);
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

    public override void DrawCell(TaffyBuilder grid, TRow row) { }

    public void DrawCell(TaffyBuilder grid, TRow row, ITableContext ctx)
        => DrawCell(grid, row, (TContext)ctx);
}
