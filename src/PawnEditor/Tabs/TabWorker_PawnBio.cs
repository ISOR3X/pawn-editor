using LudeonTK;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_PawnBio : TabWorker_Pawn
{
    public TabWorker_PawnBio(TabDef def) : base(def)
    {
    }


    [QuickAction("PawnEditor_Bio", "Teleport pawn to specific location on the current map")]
    private static void TeleportToMapSpecific()
    {
        Find.WindowStack.WindowOfType<Window_Editor>()?.Close();
        var pawn = Window_Editor.GetSelectedPawn();
        if (pawn == null) return;
        DebugTools.curTool = new("Teleport here", () =>
        {
            var cell = UI.MouseCell();
            var map = Find.CurrentMap;
            if (!cell.Standable(map) || cell.Fogged(map)) return;
            PawnUtility.TeleportTo(pawn, new PawnLocation(map), cell);
            pawn.Notify_Teleported();
            DebugTools.curTool = null;
        });
    }
}