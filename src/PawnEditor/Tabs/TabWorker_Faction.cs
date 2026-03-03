using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class TabWorker_Faction : TabWorker
{
    public TabWorker_Faction(TabDef def) : base(def)
    {
    }

    protected override void DoInnerTabContents(ref Rect inRect)
    {
        inRect = inRect.ContractedBy(16f);
        DoInnerTabContents(ref inRect, Window_Editor.GetSelectedFaction());
    }

    protected abstract void DoInnerTabContents(ref Rect inRect, Faction faction);
}