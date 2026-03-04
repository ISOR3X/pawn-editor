using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public static class PawnUtility
{
    [Flags]
    public enum PawnCategory : uint
    {
        None = 0,
        Humanlike = 1,
        Animal = 2,
        Mechanoid = 4,
        Insect = 8,
        Entity = 16,
        Other = 32,
        All = Humanlike | Insect | Mechanoid | Animal | Entity | Other
    }

    public static PawnCategory GetPawnCategory(Pawn? pawn)
    {
        if (pawn == null) return PawnCategory.Other;
        if (pawn.kindDef.RaceProps.Humanlike)
            return PawnCategory.Humanlike;
        if (pawn.kindDef.RaceProps.Insect)
            return PawnCategory.Insect;
        if (pawn.RaceProps.IsMechanoid)
            return PawnCategory.Mechanoid;
        if (pawn.RaceProps.Animal)
            return PawnCategory.Animal;
        if (pawn.IsEntity)
            return PawnCategory.Entity;
        return PawnCategory.Other;
    }

    public static SortedDictionary<string, List<Pawn>> SortByCategory(List<Pawn> pawns)
    {
        return new SortedDictionary<string, List<Pawn>>(pawns.GroupBy(GetPawnCategory)
            .ToDictionary(g => g.Key.ToString(), g => g.ToList()));
    }

    public static SortedDictionary<PawnLocation, List<Pawn>> GroupByLocation(List<Pawn> pawns,
        List<PawnLocation> locations)
    {
        var result = new Dictionary<PawnLocation, List<Pawn>>(locations.ToDictionary(l => l, _ => new List<Pawn>()));
        foreach (var pawn in pawns) result.TryAddToValueList(pawn.GetLocation(), pawn);

        return new SortedDictionary<PawnLocation, List<Pawn>>(result);
    }

    public static List<PawnLocation> GetLocations(List<Pawn> pawns)
    {
        return pawns.Select(p => p.GetLocation()).Distinct().ToList();
    }

    public static string GetFactionLabel(Pawn pawn)
    {
        return pawn.Faction != null ? pawn.Faction.Name : "Wildlife";
    }


    public static void TeleportTo(Pawn fromPawn, Pawn toPawn)
    {
        var location = toPawn.GetLocation();
        if (PawnEditorMod.Settings.SpawnNear)
        {
            TeleportTo(fromPawn, location, toPawn.Position);
            return;
        }

        TeleportTo(fromPawn, location);
    }

    public static void TeleportTo(Pawn pawn, PawnLocation pawnLoc, IntVec3 position = default)
    {
        try
        {
            var location = pawnLoc.Location;
            if (pawn.Spawned) pawn.DeSpawn();
            switch (location)
            {
                case Map map:
                {
                    var pos = position != default ? position : CellFinder.RandomEdgeCell(map);
                    GenPlace.TryPlaceThing(pawn, pos, map, ThingPlaceMode.Near);
                    break;
                }
                case Caravan caravan when pawn.Faction == Faction.OfPlayer:
                    caravan.AddPawn(pawn, false);
                    break;
                case Caravan caravan:
                    Messages.Message("Cannot add non-player pawn to caravan.", MessageTypeDefOf.RejectInput);
                    break;
                case World:
                {
                    if (pawn.IsWorldPawn())
                        Messages.Message("Pawn already exists as a world pawn", MessageTypeDefOf.RejectInput);
                    pawn.teleporting = true; // To prevent pawn from being moved to another faction.
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                    pawn.teleporting = false;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("E: " + e);
            throw;
        }
    }

    extension(Pawn pawn)
    {
        public PawnLocation GetLocation()
        {
            return new PawnLocation(pawn);
        }

        public void FullDelete()
        {
            Window_Editor.selectedPawnGroup?.Remove(pawn);
            pawn.Destroy();
            Find.WorldPawns.RemovePawn(pawn);
        }
    }


    public static void RandomizeInPlace(Pawn pawn)
    {
        if (pawn.Faction != Window_Editor.GetSelectedFaction()) return;

        // Store the old pawn's data and delete the pawn itself.
        var isSelected = Window_Editor.GetSelectedPawn() == pawn;
        var index = Window_Editor.selectedPawnGroup.IndexOf(pawn);
        var location = pawn.GetLocation();
        var position = pawn.Position;
        pawn.FullDelete();

        // Generate a new pawn.
        var req = new PawnGenerationRequest(PawnKindDefOf.Colonist, Window_Editor.GetSelectedFaction());
        var p = PawnGenerator.GeneratePawn(req);

        // Move the new pawn to the old pawn's location.
        TeleportTo(p, location, position);

        // Update list
        Window_Editor.selectedPawnGroup.Insert(index, p);
        if (isSelected) Window_Editor.TrySelect(p);
    }

    public static RenderTexture GetScaledPortrait(Pawn pawn, Rect inRect)
    {
        var rot = GetPawnCategory(pawn) is not PawnCategory.Humanlike ? Rot4.East : Rot4.South;
        var max = Mathf.Max(inRect.width, inRect.height);
        return PortraitsCache.Get(pawn, new Vector2(max, max) * 2f, rot, new Vector3(0, 0, .5f),
            0.7f, renderHeadgear: Window_Editor.showHeadgear, renderClothes: Window_Editor.showClothes);
    }
}