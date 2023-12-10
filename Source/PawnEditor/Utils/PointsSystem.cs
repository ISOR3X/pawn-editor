using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class PawnEditor
{
    public static void ResetPoints()
    {
        remainingPoints = PawnEditorMod.Settings.PointLimit;
        cachedValue = 0;
        if (!Pregame && PawnEditorMod.Settings.UseSilver)
        {
            startingSilver = ColonyInventory.AllItemsInInventory().Sum(static t => t.def == ThingDefOf.Silver ? t.stackCount : 0);
            remainingPoints = startingSilver;
        }

        Notify_PointsUsed();
    }

    public static void ApplyPoints()
    {
        var amount = remainingPoints - startingSilver;
        if (amount > 0)
        {
            var pos = ColonyInventory.AllItemsInInventory().FirstOrDefault(static t => t.def == ThingDefOf.Silver).Position;
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = Mathf.RoundToInt(amount);
            GenPlace.TryPlaceThing(silver, pos, Find.CurrentMap, ThingPlaceMode.Near);
        }
        else if (amount < 0)
        {
            amount = -amount;
            foreach (var thing in ColonyInventory.AllItemsInInventory().Where(static t => t.def == ThingDefOf.Silver))
            {
                var toRemove = Math.Min(thing.stackCount, Mathf.RoundToInt(amount));
                thing.stackCount -= toRemove;
                amount -= toRemove;

                if (thing.stackCount <= 0) thing.Destroy();
                if (amount < 1f) break;
            }
        }
    }

    public static AddResult CanUsePoints(float amount)
    {
        if (!usePointLimit) return true;
        if (remainingPoints >= amount) return true;
        return "PawnEditor.NotEnoughPoints".Translate(amount.ToStringMoney(), remainingPoints.ToStringMoney());
    }

    public static AddResult CanUsePoints(Thing thing) => CanUsePoints(GetThingValue(thing));
    public static AddResult CanUsePoints(Pawn pawn) => CanUsePoints(GetPawnValue(pawn));

    public static void Notify_PointsUsed(float? amount = null)
    {
        if (amount.HasValue)
            remainingPoints -= amount.Value;
        else
        {
            var value = 0f;
            if (Pregame)
            {
                value += ValueOfPawns(Find.GameInitData.startingAndOptionalPawns);
                value += ValueOfPawns(StartingThingsManager.GetPawns(PawnCategory.Animals));
                value += ValueOfPawns(StartingThingsManager.GetPawns(PawnCategory.Mechs));
                value += ValueOfThings(StartingThingsManager.GetStartingThingsNear());
                value += ValueOfThings(StartingThingsManager.GetStartingThingsFar());
            }
            else
            {
                AllPawns.UpdateCache(PawnEditorMod.Settings.CountNPCs ? null : Faction.OfPlayer, PawnCategory.All);
                value += ValueOfPawns(AllPawns.GetList());
                value += ValueOfThings(ColonyInventory.AllItemsInInventory());
            }


            remainingPoints -= value - cachedValue;
            cachedValue = value;
        }
    }

    private static float ValueOfPawns(IEnumerable<Pawn> pawns) => pawns.Sum(GetPawnValue);
    private static float ValueOfThings(IEnumerable<Thing> things) => things.Sum(GetThingValue);
    private static float GetThingValue(Thing thing) => thing.MarketValue * thing.stackCount;

    private static float GetPawnValue(Pawn pawn)
    {
        var num = pawn.MarketValue;
        if (pawn.apparel != null)
            num += pawn.apparel.WornApparel.Sum(t => t.MarketValue);
        if (pawn.equipment != null)
            num += pawn.equipment.AllEquipmentListForReading.Sum(t => t.MarketValue);
        return num;
    }
}
