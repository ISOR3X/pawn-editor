using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_FactionOverview : TabWorker_Faction
{
    private PawnTable pawnTable;

    public TabWorker_FactionOverview(TabDef def) : base(def) 
    {
        pawnTable = (PawnTable)Activator.CreateInstance(PawnTableDefOf.PawnEditor_ColonyOverview.workerClass, PawnTableDefOf.PawnEditor_ColonyOverview,
            (Func<IEnumerable<Pawn>>)(() =>
                Window_Editor.GetSelectedFaction() != null ? PawnLister.Pawns_ByFaction.Item1[Window_Editor.GetSelectedFaction()!] : PawnLister.Pawns_ByFaction.Item2), 0, 0);
    }

    protected override void DoInnerTabContents(ref Rect inRect, Faction faction)
    {
        if (!pawnTable.hasFixedSize) pawnTable.SetFixedSize(inRect.size);

        if (pawnTable.PawnsListForReading.First().Faction != faction)
        {
            pawnTable.SetDirty();
        }

        pawnTable.PawnTableOnGUI(inRect.position);
    }
}