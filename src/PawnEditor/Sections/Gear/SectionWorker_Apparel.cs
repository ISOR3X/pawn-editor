using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_Apparel(SectionDef def) : SectionWorker_ThingTable<Apparel>(def)
{
    protected override Func<Pawn, List<Apparel>> TableItems => p => p.apparel.WornApparel;
    protected override string TableTitle => "Apparel";
}