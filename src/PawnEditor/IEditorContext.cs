using RimWorld;
using Verse;
using Void;

namespace PawnEditor;

public sealed class PawnContext(Pawn pawn) : IContext<Pawn>
{
    public Pawn Value { get; } = pawn;
    public int HashCode { get; } = pawn.thingIDNumber;
}

public sealed class ThingContext(Thing thing) : IContext<Thing>
{
    public Thing Value { get; } = thing;
    public int HashCode { get; } = thing.thingIDNumber;
}

public sealed class FactionContext(Faction faction) : IContext<Faction>
{
    public Faction Value { get; } = faction;
    public int HashCode { get; } = faction.GetUniqueLoadID().GetHashCode();
}