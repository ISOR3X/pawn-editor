using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class PawnLocation : IEquatable<PawnLocation>, IComparable<PawnLocation>
{
    public object Location { get; }
    public string Label { get; }

    public PawnLocation(object location)
    {
        Location = location;
        Label = GetLocationLabel(location);
    }

    public PawnLocation(Pawn pawn)
    {
        Location = GetDirectLocation(pawn);
        Label = GetLocationLabel(pawn);
    }
    
    
    private static string GetLocationLabel(object obj)
    {
        return obj switch
        {
            Map map => map.Parent.Label,
            Caravan caravan => caravan.Label,
            World => "World",
            _ => "Unknown"
        };
    }

    public static string GetLocationLabel(Pawn pawn)
    {
        if (Window_Editor.Playing || pawn.Faction != Faction.OfPlayer) return GetLocationLabel(GetDirectLocation(pawn));
        return Find.GameInitData.startingPawnCount >= StartingPawnUtility.PawnIndex(pawn) ? "StartingPawnsSelected".Translate() : "StartingPawnsLeftBehind".Translate();

    }

    private static object GetDirectLocation(Pawn pawn)
    {
        return pawn.MapHeld ?? pawn.Map ?? pawn.GetCaravan() ?? (object)Find.World;
    }

    public bool Equals(PawnLocation other)
    {
        if (other == null)
            return false;

        return Label == other.Label;
    }

    public int CompareTo(PawnLocation other)
    {
        return String.Compare(Label, other.Label, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return Label.GetHashCode();
    }
}