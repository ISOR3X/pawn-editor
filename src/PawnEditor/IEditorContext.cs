using RimWorld;
using Verse;

namespace PawnEditor;

public interface IEditorContext { }

public interface IEditorContext<T> : IEditorContext
{
    T Value { get; }
}

public sealed class PawnContext(Pawn pawn) : IEditorContext<Pawn>
{
    public Pawn Value { get; } = pawn;
}

public sealed class FactionContext(Faction faction) : IEditorContext<Faction>
{
    public Faction Value { get; } = faction;
}
