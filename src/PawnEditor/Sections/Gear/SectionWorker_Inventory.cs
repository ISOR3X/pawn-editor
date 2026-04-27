
using Verse;

namespace PawnEditor;

public class SectionWorker_Inventory(SectionDef def) : SectionWorker_ThingTable<Thing>(def)
{
    protected override Func<Pawn, List<Thing>> TableItems => p => p.inventory.innerContainer.innerList ?? [];
    protected override string TableTitle => "Inventory";
}