using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class TabWorker_Faction(TabDef def) : TabWorker(def)
{
    protected override void DoInnerTabContents(ref Rect inRect)
    {
        inRect = inRect.ContractedBy(16f);
        var faction = Window_Editor.GetSelectedFaction();
        if (faction == null) return;
        DoInnerTabContents(ref inRect, faction);
    }

    protected abstract void DoInnerTabContents(ref Rect inRect, Faction faction);
}