using System.Collections.Generic;
using Verse;

namespace PawnEditor;

public static class ColonyInventory
{
    private static readonly List<Thing> inventoryItems = new();

    public static List<Thing> AllItemsInInventory() => inventoryItems;

    public static void RecacheItems()
    {
        inventoryItems.Clear();
        foreach (var slotGroup in Find.CurrentMap.haulDestinationManager.AllGroupsListForReading) inventoryItems.AddRange(slotGroup.HeldThings);
        TabWorker_EquipmentLoot.ClearCaches();
    }

    public static void ClearCache()
    {
        inventoryItems.Clear();
    }
}
