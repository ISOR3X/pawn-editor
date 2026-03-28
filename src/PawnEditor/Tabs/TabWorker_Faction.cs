using RimWorld;
using Verse;

namespace PawnEditor;

public abstract class TabWorker_Faction(TabDef def) : TabWorker(def)
{
    protected override void DoInnerTabContents(TaffyBuilder col)
    {
        var faction = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedFaction();
        if (faction == null) return;
        DoInnerTabContents(col, faction);
    }

    protected abstract void DoInnerTabContents(TaffyBuilder col, Faction faction);
}
