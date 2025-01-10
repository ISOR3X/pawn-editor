using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static List<Pawn> GetPawnsWithoutFaction()
        {
            List<Pawn> pawnsWithoutFaction = new List<Pawn>();

            foreach (var pawn in PawnsFinder.All_AliveOrDead)
            {
                if (pawn.AnimalOrWildMan() && !pawn.IsWildMan())
                    continue;

                if (pawn.Faction == null || pawn.Faction == default)
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
