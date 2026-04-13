using RimWorld;
using Verse;

namespace PawnEditor;

public interface IEditorContext { }

public interface IEditorContext<T> : IEditorContext
{
    T Value { get; }
}

public sealed class PawnContext : IEditorContext<Pawn>
{
    public Pawn Value { get; }
    public PawnContext(Pawn pawn) => Value = pawn;
}

public sealed class FactionContext : IEditorContext<Faction>
{
    public Faction Value { get; }
    public FactionContext(Faction faction) => Value = faction;
}
