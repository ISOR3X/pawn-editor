using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PawnEditor.Utils
{
    internal static class PawnEditor_PawnsFinder
    {
        public static IEnumerable<Faction> GetAllFactionsContainingAtLeastOneHumanLike()
        {
            List<Faction> existingFactions = new List<Faction>();

            foreach (var pawn in PawnsFinder.All_AliveOrDead)
            {
                if (pawn.AnimalOrWildMan() && !pawn.IsWildMan())
                    continue;

                if (pawn.Faction != null)
                {
                    if (!existingFactions.Contains(pawn.Faction))
                    {
                        existingFactions.Add(pawn.Faction);
                    }
                }
                
            }
            return existingFactions;
        }

        public static List<Pawn> GetHumanPawnsWithoutFaction()
        {
            List<Pawn> pawnsWithoutFaction = new List<Pawn>();

            foreach (var pawn in PawnsFinder.All_AliveOrDead)
            {
                if (pawn.AnimalOrWildMan() && !pawn.IsWildMan())
                    continue;

                if ((pawn.Faction == null || pawn.Faction == default) && pawn.Faction != Find.FactionManager.ofPlayer)
                {

                    if (!pawnsWithoutFaction.Contains(pawn))
                    {
                        pawnsWithoutFaction.Add(pawn);
                    }
                }
            }

            return pawnsWithoutFaction;
        }
        
        public static List<Pawn> GetAllPawnsWithoutFaction()
        {
            List<Pawn> pawnsWithoutFaction = new List<Pawn>();

            foreach (var pawn in PawnsFinder.All_AliveOrDead)
            {
                if ((pawn.Faction == null || pawn.Faction == default) && pawn.Faction != Find.FactionManager.ofPlayer)
                {

                    if (!pawnsWithoutFaction.Contains(pawn))
                    {
                        pawnsWithoutFaction.Add(pawn);
                    }
                }
            }

            return pawnsWithoutFaction;
        }
    }
}
