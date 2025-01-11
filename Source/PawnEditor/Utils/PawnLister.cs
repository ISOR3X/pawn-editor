using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor.Utils;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PawnEditor;

public class PawnListerBase
{
    private static readonly List<PawnListerBase> allLists = new();
    private readonly List<Pawn> pawns = new();
    private PawnCategory category;
    private Faction faction;
    private bool inited;


    public PawnListerBase()
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
        if (Find.GameInitData?.startingAndOptionalPawns != null)
        {
            AddLocation(Find.World, Find.GameInitData.startingAndOptionalPawns);
        }
        inited = true;
    }
    
    public void UpdateCacheWithNullFaction()
    {
        ClearCache();
        this.faction = default;
        this.category = PawnCategory.Humans;
        foreach (var map in Find.Maps) AddLocation(map, map.mapPawns.AllPawns);

        foreach (var caravan in Find.WorldObjects.Caravans) AddLocation(caravan, caravan.PawnsListForReading);

        AddLocation(Find.World, PawnEditor_PawnsFinder.GetHumanPawnsWithoutFaction());
        if (Find.GameInitData?.startingAndOptionalPawns != null)
        {
            AddLocation(Find.World, Find.GameInitData.startingAndOptionalPawns);
        }
        inited = true;
    }

    protected virtual bool AddLocation(object location, IEnumerable<Pawn> occupants)
    {
        var hasAny = false;
        foreach (var pawn in occupants)
        {
            if (pawn == null) continue;
            if ((faction != null && pawn.Faction != faction) || !category.Includes(pawn)) continue;
            hasAny = true;
            AddPawn(location, pawn);
        }

        return hasAny;
    }


    protected virtual void AddPawn(object location, Pawn pawn)
    {
        if (pawns.Contains(pawn) is false)
        {
            pawns.Add(pawn);
        }
    }

    protected void CheckInited()
    {
        if (!inited) throw new InvalidOperationException("Cannot get lists from uninitialized PawnLister");
    }

    public List<Pawn> GetList()
    {
        CheckInited();
        return pawns;
    }

    public virtual void ClearCache()
    {
        pawns.Clear();
        faction = null;
        category = PawnCategory.All;
        inited = false;
    }

    protected void NotifyOthers()
    {
        foreach (var lister in allLists.Except(this))
            if (lister.faction == faction && lister.category == category)
                lister.UpdateCache(faction, category);

        TabWorker_FactionOverview.CheckRecache(faction);
    }
}

public class PawnLister : PawnListerBase
{
    private readonly List<object> locations = new();
    private readonly List<string> sections = new();
    private int sectionCount;

    public override void ClearCache()
    {
        base.ClearCache();
        sections.Clear();
        locations.Clear();
        sectionCount = 0;
    }

    protected override bool AddLocation(object location, IEnumerable<Pawn> occupants)
    {
        sections.Add(LocationLabel(location));
        sectionCount++;
        var hasAny = base.AddLocation(location, occupants);
        sections.RemoveLast();
        if (!hasAny) sectionCount--;
        return hasAny;
    }

    protected override void AddPawn(object location, Pawn pawn)
    {
        locations.Add(location);
        sections.Add(null);
        base.AddPawn(location, pawn);
    }

    public void OnReorder(Pawn pawn, int fromIndex, int toIndex)
    {
        if (PawnEditor.Pregame)
        {
            var startingPawnCount = Find.GameInitData.startingPawnCount;
            if (fromIndex < startingPawnCount && toIndex > startingPawnCount && startingPawnCount > 1) Find.GameInitData.startingPawnCount--;
            if (fromIndex >= startingPawnCount && toIndex < startingPawnCount) Find.GameInitData.startingPawnCount++;
            StartingPawnUtility.ReorderRequests(fromIndex, toIndex);
            NotifyOthers();
        }
        else
        {
            var from = locations[fromIndex];
            var to = locations[toIndex];
            locations.Insert(toIndex, to);
            locations.RemoveAt(fromIndex < toIndex ? fromIndex : fromIndex + 1);
            if (sections[toIndex] != null) toIndex++;
            sections.Insert(toIndex, null);
            if (sections.Pop() != null) sectionCount--;
            DoTeleport(pawn, from, to);
            NotifyOthers();
            PawnEditor.RecachePawnList();
        }
    }


    public (List<Pawn>, List<string>, int) GetLists() => (GetList(), sections, sectionCount);

    public void OnDelete(Pawn pawn)
    {
        var pawns = GetList();
        FullyRemove(pawn);
        pawn.Discard(true);
        RemoveFromList(pawn);
        if (sections[pawns.IndexOf(pawn)] != null) sectionCount--;
        NotifyOthers();
    }

    private void RemoveFromList(Pawn pawn)
    {
        var pawns = GetList();
        var index = pawns.IndexOf(pawn);
        locations.RemoveAt(index);
        var nextIndex = index + 1;
        if (nextIndex == sections.Count) return;
        if (sections[index] != null && sections[nextIndex] == null) sections[nextIndex] = sections[index];
    }

    public void TeleportFromTo(Pawn pawn, object from, object to)
    {
        if (to == from) return;
        var pawns = GetList();
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
            if (sections[fromIndex] != null) sectionCount--;
            sections.RemoveAt(fromIndex);
            pawns.Remove(pawn);
            pawns.Add(pawn);
            sections.Add(LocationLabel(to));
            sectionCount++;
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

    public object GetLocation(Pawn pawn)
    {
        var pawns = GetList();
        if (pawns.Contains(pawn)) return locations[pawns.IndexOf(pawn)];
        if (pawn.SpawnedOrAnyParentSpawned) return pawn.MapHeld;
        if (pawn.GetCaravan() is { } caravan) return caravan;
        if (Find.WorldPawns.Contains(pawn)) return Find.World;
        return null;
    }

    public static void FullyRemove(Pawn pawn)
    {
        if (pawn.SpawnedOrAnyParentSpawned) pawn.ExitMap(false, Rot4.Invalid);
        if (pawn.GetCaravan() is { } caravan) caravan.RemovePawn(pawn);
        if (pawn.IsWorldPawn()) Find.WorldPawns.RemovePawn(pawn);
    }

    public static string LocationLabel(object location)
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