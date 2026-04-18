using RimWorld;
using Verse;
using Void;

namespace PawnEditor;

public sealed class PawnContext(Pawn pawn) : IContext<Pawn>
{
    public Pawn Value { get; } = pawn;
}

public sealed class FactionContext(Faction faction) : IContext<Faction>
{
    public Faction Value { get; } = faction;
}
