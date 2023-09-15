using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PawnEditor;

public class PawnLister
{
    private static readonly List<PawnLister> allLists = new();
    private readonly List<object> locations = new();
    private readonly List<Pawn> pawns = new();
    private readonly List<string> sections = new();
    private PawnCategory category;
    private Faction faction;
    private int sectionCount;

    public PawnLister()
    {
        allLists.Add(this);
    }

    public void UpdateCache(Faction faction, PawnCategory category)
    {
        ClearCache();
        this.faction = faction;
        this.category = category;
        foreach (var map in Find.Maps) AddLocation(map, map.mapPawns.AllPawns);

        foreach (var caravan in Find.WorldObjects.Caravans) AddLocation(caravan, caravan.PawnsListForReading);

        AddLocation(Find.World, Find.WorldPawns.AllPawnsAliveOrDead);
    }

    public void ClearCache()
    {
        pawns.Clear();
        sections.Clear();
        locations.Clear();
        sectionCount = 0;
        faction = null;
        category = PawnCategory.Humans;
    }

    private void AddLocation(object location, IEnumerable<Pawn> occupants)
    {
        sections.Add(LocationLabel(location));
        sectionCount++;
        foreach (var pawn in occupants)
        {
            if (pawn.Faction != faction || !category.Includes(pawn)) continue;
            locations.Add(location);
            sections.Add(null);
            pawns.Add(pawn);
        }

        sections.RemoveLast();
    }

    public void OnReorder(Pawn pawn, int fromIndex, int toIndex)
    {
        var from = locations[fromIndex];
        var to = locations[toIndex];
        locations.Insert(toIndex, to);
        locations.RemoveAt(fromIndex < toIndex ? fromIndex : fromIndex + 1);
        if (sections[toIndex] != null) toIndex++;
        sections.Insert(toIndex, null);
        sections.RemoveLast();
        DoTeleport(pawn, from, to);
        NotifyOthers();
    }

    public (List<Pawn>, List<string>, int) GetLists() => (pawns, sections, sectionCount);

    public void OnDelete(Pawn pawn)
    {
        FullyRemove(pawn);
        pawn.Discard(true);
        RemoveFromList(pawn);
        NotifyOthers();
    }

    private void NotifyOthers()
    {
        foreach (var lister in allLists.Except(this))
            if (lister.faction == faction && lister.category == category)
                lister.UpdateCache(faction, category);
    }

    private void RemoveFromList(Pawn pawn)
    {
        var index = pawns.IndexOf(pawn);
        locations.RemoveAt(index);
        var nextIndex = index + 1;
        if (nextIndex == sections.Count) return;
        if (sections[index] != null && sections[nextIndex] == null) sections[nextIndex] = sections[index];
    }

    public void TeleportFromTo(Pawn pawn, object from, object to)
    {
        if (to == from) return;
        if (pawns.Contains(pawn))
        {
            var toIndex = locations.LastIndexOf(to);
            var fromIndex = pawns.IndexOf(pawn);
            if (toIndex >= 0)
            {
                OnReorder(pawn, fromIndex, toIndex);
                return;
            }

            RemoveFromList(pawn);
            sections.RemoveAt(fromIndex);
            pawns.Remove(pawn);
            pawns.Add(pawn);
            sections.Add(LocationLabel(to));
            locations.Add(to);
        }

        DoTeleport(pawn, from, to);
    }

    private void DoTeleport(Pawn pawn, object from, object to)
    {
        pawn.teleporting = true;
        FullyRemove(pawn);
        if (to is Map map) GenSpawn.Spawn(pawn, CellFinder.RandomCell(map), map);
        else if (to is Caravan caravan) caravan.AddPawn(pawn, true);
        else if (to is World) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        pawn.teleporting = false;
    }

    public FloatMenuOption GetTeleportOption(object location, Pawn pawn)
    {
        return new(LocationLabel(location), () => TeleportFromTo(pawn, GetLocation(pawn), location));
    }

    private object GetLocation(Pawn pawn)
    {
        if (pawns.Contains(pawn)) return locations[pawns.IndexOf(pawn)];
        if (pawn.SpawnedOrAnyParentSpawned) return pawn.MapHeld;
        if (pawn.GetCaravan() is { } caravan) return caravan;
        if (Find.WorldPawns.Contains(pawn)) return Find.World;
        return null;
    }

    private static void FullyRemove(Pawn pawn)
    {
        if (pawn.SpawnedOrAnyParentSpawned) pawn.ExitMap(false, Rot4.Invalid);
        if (pawn.GetCaravan() is { } caravan) caravan.RemovePawn(pawn);
        if (pawn.IsWorldPawn()) Find.WorldPawns.RemovePawn(pawn);
    }

    private static string LocationLabel(object location)
    {
        return location switch
        {
            Map map => map.Parent.LabelCap,
            Caravan caravan => caravan.Name,
            World => MainButtonDefOf.World.LabelCap,
            _ => location.ToString()
        };
    }
}
