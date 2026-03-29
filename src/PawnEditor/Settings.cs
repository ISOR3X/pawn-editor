using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Settings : ModSettings
{
    public enum RestrictionMode
    {
        None,
        Severe
    }
    
    /// <summary> When enabled, dead pawns in the world are hidden from the left panel. (Destroyed pawns are moved to the world for compatibility reasons.) </summary>
    public bool hideDeadWorldPawns = true;

    /// <summary> Decides the flexibility of the mod. None has no restrictions but is less stable. </summary>
    public RestrictionMode restriction = RestrictionMode.Severe;

    /// <summary> When enabled, a pawn is spawned near the pawn it is teleported to. </summary>
    public bool spawnNear = true;

    public bool drawDebug;


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref restriction, nameof(restriction), RestrictionMode.Severe);
        Scribe_Values.Look(ref spawnNear, nameof(spawnNear), true);
        Scribe_Values.Look(ref drawDebug, nameof(drawDebug), defaultValue: false);
        Scribe_Values.Look(ref hideDeadWorldPawns, nameof(hideDeadWorldPawns), true);
        Scribe_Values.Look(ref Window_Editor.SavedWindowRect, nameof(Window_Editor.SavedWindowRect), Window_Editor.DefaultWindowRect);
    }
}