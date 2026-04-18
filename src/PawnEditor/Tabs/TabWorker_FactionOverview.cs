using HotSwap;
using RimWorld;
using Verse;
using Void;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_FactionOverview(TabDef def) : TabWorker<FactionContext>(def)
{
    private readonly PawnTable _pawnTable = (PawnTable)Activator.CreateInstance(
        PawnTableDefOf.PawnEditor_ColonyOverview.workerClass, PawnTableDefOf.PawnEditor_ColonyOverview,
        (Func<IEnumerable<Pawn>>)(() =>
            PawnLister.Pawns_ByFaction[Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedFaction()!]), 0, 0);

    protected override void DoInnerTabContents(TaffyBuilder col, FactionContext ctx)
    {
        col.Item(r =>
        {
            if (!_pawnTable.hasFixedSize) _pawnTable.SetFixedSize(r.size);
            if (_pawnTable.PawnsListForReading.First().Faction != ctx.Value) _pawnTable.SetDirty();
            _pawnTable.PawnTableOnGUI(r.position);
        }, new StyleOverride { flexGrow = 1f });
    }
}
