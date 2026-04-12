using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Inventory(SectionDef def) : SectionWorker_Apparel<Thing>(def)
{
    protected override Func<Pawn, List<Thing>> TableItems => p => p.inventory.innerContainer.innerList ?? [];
    protected override string TableTitle => "Apparel";
}