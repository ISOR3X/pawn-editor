using Verse;

namespace PawnEditor.Table;

public interface ITableContext
{
}

public interface ITableContext<T> : ITableContext
{
    T Value { get; }
}

public sealed class PawnContext : ITableContext<Pawn>
{
    public Pawn Value { get; }
    public PawnContext(Pawn pawn) => Value = pawn;
}