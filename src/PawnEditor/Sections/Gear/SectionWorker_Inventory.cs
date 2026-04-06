using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Inventory(SectionDef def) : SectionWorker_ThingTable(def)
{
    protected override ThingTableDef TableDef => TableDefOf.PawnEditor_ThingTable_Inventory;
    protected override string Label => "Inventory";

    protected override IEnumerable<Thing> GetThings()
    {
        var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        return pawn?.inventory.innerContainer ?? [];
    }
}