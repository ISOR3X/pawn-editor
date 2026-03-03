using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class PawnLister
{
    private static readonly Dictionary<Faction, List<Pawn>> PawnsByFactionTemporary = new();
    private static readonly List<Pawn> PawnsNoFactionTemporary = [];
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
    public static (Dictionary<Faction, List<Pawn>>, List<Pawn>) Pawns_ByFaction
    {
        get
        {
            // TODO: Keep an eye on performance of this.
            PawnsByFactionTemporary.Clear();
            PawnsNoFactionTemporary.Clear();
            PawnsByFactionTemporary.AddRange(
                Find.FactionManager.AllFactions.ToDictionary(f => f, _ => new List<Pawn>()));
            foreach (var group in PawnsFinder.All_AliveOrDead
                         .Where(p => !(p.IsWorldPawn() && Find.WorldPawns.GetSituation(p) == WorldPawnSituation.Dead) ||
                                     !PawnEditorMod.Settings.HideDeadWorldPawns)
                         .GroupBy(p => p.Faction))
                if (group.Key != null && PawnsByFactionTemporary.ContainsKey(group.Key))
                    PawnsByFactionTemporary[group.Key] = group.ToList();
                else
                    PawnsNoFactionTemporary.AddRange(group);

            return (PawnsByFactionTemporary, PawnsNoFactionTemporary);
        }
    }
}