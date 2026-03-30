using UnityEngine;
using Verse;


namespace PawnEditor.Table;

public abstract class ColumnWorker<TRow>
{
    public abstract float Width { get; }
    public abstract void DrawHeader(Rect r);
    public abstract void DrawCell(Rect r, TRow row);

    public static ColumnWorker<TRow> Create(
        string header,
        float width,
        Action<Rect, TRow> drawCell)
        => new DelegateColumn(header, width, drawCell);

    public static ColumnWorker<TRow> Create<TContext>(
        string header,
        float width,
        Action<Rect, TRow, TContext> drawCell)
        where TContext : ITableContext
        => new DelegateContextColumn<TContext>(header, width, drawCell);

    private sealed class DelegateColumn(string header, float width, Action<Rect, TRow> drawCell)
        : ColumnWorker<TRow>
    {
        public override float Width => width;

        public override void DrawHeader(Rect r)
        {
            using (new TextBlock(TextAnchor.MiddleCenter))
                Verse.Widgets.Label(r, header);
        }

        public override void DrawCell(Rect r, TRow row) => drawCell(r, row);
    }

    private sealed class DelegateContextColumn<TContext>(
        string header,
        float width,
        Action<Rect, TRow, TContext> drawCell)
        : ColumnWorker<TRow, TContext>
        where TContext : ITableContext
    {
        public override float Width => width;

        public override void DrawHeader(Rect r)
        {
            using (new TextBlock(TextAnchor.MiddleCenter))
                Verse.Widgets.Label(r, header);
        }

        protected override void DrawCell(Rect r, TRow row, TContext ctx) => drawCell(r, row, ctx);
    }
}

internal interface IContextColumn<in TRow>
{
    void DrawCell(Rect r, TRow row, ITableContext ctx);
}

public abstract class ColumnWorker<TRow, TContext> : ColumnWorker<TRow>, IContextColumn<TRow>
    where TContext : ITableContext
{
    protected abstract void DrawCell(Rect r, TRow row, TContext ctx);

    public override void DrawCell(Rect r, TRow row) { }

    public void DrawCell(Rect r, TRow row, ITableContext ctx)
        => DrawCell(r, row, (TContext)ctx);

}
