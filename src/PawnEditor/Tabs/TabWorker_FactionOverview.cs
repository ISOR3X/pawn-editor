using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public class TabWorker_FactionOverview(TabDef def) : TabWorker_Faction(def)
{
    private readonly PawnTable _pawnTable = (PawnTable)Activator.CreateInstance(
        PawnTableDefOf.PawnEditor_ColonyOverview.workerClass, PawnTableDefOf.PawnEditor_ColonyOverview,
        (Func<IEnumerable<Pawn>>)(() =>
            Window_Editor.GetSelectedFaction() != null
                ? PawnLister.Pawns_ByFaction.Item1[Window_Editor.GetSelectedFaction()!]
                : PawnLister.Pawns_ByFaction.Item2), 0, 0);

    protected override void DoInnerTabContents(ref Rect inRect, Faction faction)
    {
        if (!_pawnTable.hasFixedSize) _pawnTable.SetFixedSize(inRect.size);

        if (_pawnTable.PawnsListForReading.First().Faction != faction) _pawnTable.SetDirty();

        _pawnTable.PawnTableOnGUI(inRect.position);
    }
}