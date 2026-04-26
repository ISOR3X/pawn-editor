using LudeonTK;
using Verse;

namespace PawnEditor;

public static class QuickActions
{
    [QuickAction("PawnEditor_Bio", "Teleport pawn to specific location on the current map")]
    private static void TeleportToMapSpecific()
    {
        // TODO: Expose pawn through function params instead.
        // Find.WindowStack.WindowOfType<Window_Editor>()?.Close();
        // var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        // if (pawn == null) return;
        DebugTools.curTool = new DebugTool("Teleport here", () =>
        {
            var cell = UI.MouseCell();
            var map = Find.CurrentMap;
            if (!cell.Standable(map) || cell.Fogged(map)) return;
            // PawnUtility.TeleportTo(pawn, new PawnLocation(map), cell);
            // pawn.Notify_Teleported();
            DebugTools.curTool = null;
        });
    }
}