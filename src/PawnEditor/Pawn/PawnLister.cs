using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class PawnLister
{
    private static readonly Dictionary<FactionKey, List<Pawn>> PawnsByFactionTemporary = new();
    private static readonly List<PawnLocation> AllLocationsTemporary = [];

    public static List<PawnLocation> AllLocations
    {
        get
        {
            AllLocationsTemporary.Clear();
            AllLocationsTemporary.AddRange(PawnsFinder.All_AliveOrDead.Select(p => p.GetLocation()).Distinct()
                .OrderBy(l => l.Label));
            return AllLocationsTemporary;
        }
    }

    /// <summary>
    ///     Returns a dictionary of pawns grouped by faction and a list of pawns with no faction.
    /// </summary>
    public static Dictionary<FactionKey, List<Pawn>> Pawns_ByFaction
    {
        get
        {
            // TODO: Keep an eye on performance of this.

            var availablePawns = PawnsFinder.All_AliveOrDead
                .Where(p => !(p.IsWorldPawn() && Find.WorldPawns.GetSituation(p) == WorldPawnSituation.Dead) ||
                            !PawnEditorMod.Settings.HideDeadWorldPawns);

            PawnsByFactionTemporary.Clear();

            // Ensure all known factions have an entry, even if empty
            foreach (var faction in Find.FactionManager.AllFactions)
                PawnsByFactionTemporary[faction] = [];
            PawnsByFactionTemporary[null!] = [];

            // Bucket pawns
            foreach (var pawn in availablePawns) PawnsByFactionTemporary[pawn.Faction].Add(pawn);

            return PawnsByFactionTemporary;
        }
    }

    // Some trickery so we can use null keys in a dict without our IDE complaining.
    public readonly struct FactionKey(Faction? faction) : IEquatable<FactionKey>
    {
        public static readonly FactionKey None = new(null);

        public readonly Faction? Faction = faction;

        // Allows using Faction as a key in the dict instead of FactionKey
        public static implicit operator FactionKey(Faction? faction)
        {
            return new FactionKey(faction);
        }

        public bool Equals(FactionKey other)
        {
            return Faction == other.Faction;
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Faction?.GetHashCode() ?? 0;
        }
    }
}