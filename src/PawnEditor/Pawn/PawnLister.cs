using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PawnEditor;

[Reloadable]
public static class PawnLister
{
    private static readonly Dictionary<Faction, List<Pawn>> PawnsByFactionTemporary = new();
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
    public static Dictionary<Faction, List<Pawn>> Pawns_ByFaction
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
            // PawnsByFactionTemporary[null!] = [];

            // Bucket pawns
            foreach (var pawn in availablePawns)
            {
                var key = pawn.Faction != null && PawnsByFactionTemporary.ContainsKey(pawn.Faction)
                    ? pawn.Faction
                    : null;
                if (key != null) PawnsByFactionTemporary[key].Add(pawn);
            }

            return PawnsByFactionTemporary;
        }
    }
}