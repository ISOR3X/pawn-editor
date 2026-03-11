using Verse;

namespace PawnEditor;

public class Settings : ModSettings
{
    public enum RestrictionMode
    {
        None,
        Severe
    }

    public enum WindowSize
    {
        Small,
        Medium,
        Large
    }

    // When enabled, dead pawns in the world are hidden from the left panel. (Destroyed pawns are moved to the world for compatibility reasons.)
    public bool HideDeadWorldPawns = true;

    public RestrictionMode
        Restriction =
            RestrictionMode.Severe; // Decided the flexibility of the mod. None has no restrictions but is less stable.

    public WindowSize Size = WindowSize.Small; // The size of the pawn editor window.

    public bool SpawnNear = true; // When enabled, a pawn is spawned near the pawn it is teleported to.


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Restriction, nameof(Restriction), RestrictionMode.Severe);
        Scribe_Values.Look(ref Size, nameof(Size));
        Scribe_Values.Look(ref SpawnNear, nameof(SpawnNear), true);
        Scribe_Values.Look(ref HideDeadWorldPawns, nameof(HideDeadWorldPawns), true);
    }
}