using PawnEditor.Table.ColumnWorkers;
using Taffy;
using UnityEngine;
using Verse;
using PawnEditor;
using Void;

namespace PawnEditor.Table;

public abstract class ColumnWorker<TRow>
{
    protected virtual string? HeaderLabel => null;
    protected virtual string? HeaderTip => null;
    public virtual TrackSizingFunction TrackSize => TrackSizingFunction.Auto();

    public virtual bool Sortable => false;
    public virtual int Compare(TRow a, TRow b) => 0;


    public virtual void DrawHeader(Rect r)
    {
        if (HeaderLabel != null)
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(r, HeaderLabel);
        if (HeaderTip != null) TooltipHandler.TipRegion(r, (TipSignal)HeaderTip);
    }

    public abstract void DrawCell(TaffyBuilder grid, TRow row);

    #region ENTRY POINTS

    public static ColumnWorker<TRow> Create(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow> drawCell,
        string? header = null,
        Func<TRow, TRow, int>? compare = null,
        string? headerTip = null)
        => new DelegateColumn(trackSize, drawCell, header, compare, headerTip);

    public static ColumnWorker<TRow> Create<TContext>(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow, TContext> drawCell,
        string? header = null,
        Func<TRow, TRow, int>? compare = null,
        string? headerTip = null)
        where TContext : IContext
        => new DelegateContextColumn<TContext>(trackSize, drawCell, header, compare, headerTip);

    /// <summary>
    /// Creates a sortable string column. The text projection is used both for rendering and
    /// for case-insensitive alphabetical sorting.
    /// </summary>
    public static ColumnWorker<TRow> CreateText(
        TrackSizingFunction trackSize,
        Func<TRow, string> getText,
        string? header = null,
        Color? color = null,
        string? headerTip = null)
        => new TextColumnWorker<TRow>(trackSize, getText, header, color, headerTip);

    #endregion

    #region FACTORIES

    private sealed class DelegateColumn(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow> drawCell,
        string? header,
        Func<TRow, TRow, int>? compare,
        string? headerTip)
        : ColumnWorker<TRow>
    {
        public override TrackSizingFunction TrackSize => trackSize;
        public override bool Sortable => compare != null;
        public override int Compare(TRow a, TRow b) => compare?.Invoke(a, b) ?? 0;
        protected override string? HeaderLabel => header;
        protected override string? HeaderTip => headerTip;

        public override void DrawCell(TaffyBuilder grid, TRow row) => drawCell(grid, row);
    }

    private sealed class DelegateContextColumn<TContext>(
        TrackSizingFunction trackSize,
        Action<TaffyBuilder, TRow, TContext> drawCell,
        string? header,
        Func<TRow, TRow, int>? compare,
        string? headerTip)
        : ColumnWorker<TRow, TContext>
        where TContext : IContext
    {
        public override TrackSizingFunction TrackSize => trackSize;
        public override bool Sortable => compare != null;
        public override int Compare(TRow a, TRow b) => compare?.Invoke(a, b) ?? 0;

        protected override string? HeaderLabel => header;
        protected override string? HeaderTip => headerTip;
        protected override void DrawCell(TaffyBuilder grid, TRow row, TContext ctx) => drawCell(grid, row, ctx);
    }

    #endregion
}

internal interface IContextColumn<in TRow>
{
    void DrawCell(TaffyBuilder grid, TRow row, IContext ctx);
}

public abstract class ColumnWorker<TRow, TContext> : ColumnWorker<TRow>, IContextColumn<TRow>
    where TContext : IContext
{
    protected abstract void DrawCell(TaffyBuilder grid, TRow row, TContext ctx);

    public override void DrawCell(TaffyBuilder grid, TRow row)
    {
    }

    public void DrawCell(TaffyBuilder grid, TRow row, IContext ctx)
        => DrawCell(grid, row, (TContext)ctx);
}