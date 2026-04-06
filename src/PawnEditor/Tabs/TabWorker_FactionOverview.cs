using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_FactionOverview(TabDef def) : TabWorker_Faction(def)
{
    private readonly PawnTable _pawnTable = (PawnTable)Activator.CreateInstance(
        PawnTableDefOf.PawnEditor_ColonyOverview.workerClass, PawnTableDefOf.PawnEditor_ColonyOverview,
        (Func<IEnumerable<Pawn>>)(() =>
            PawnLister.Pawns_ByFaction[Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedFaction()!]), 0, 0);

    protected override void DoInnerTabContents(TaffyBuilder col, Faction faction)
    {
        col.Item(grow: 1f, draw: r =>
        {
            if (!_pawnTable.hasFixedSize) _pawnTable.SetFixedSize(r.size);
            if (_pawnTable.PawnsListForReading.First().Faction != faction) _pawnTable.SetDirty();
            _pawnTable.PawnTableOnGUI(r.position);
        });
    }
}
