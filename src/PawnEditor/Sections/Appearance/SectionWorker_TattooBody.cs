using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_TattooBody(SectionDef def) : SectionWorker_Hair<TattooDef>(def)
{
    protected override List<TattooDef> TableItems => DefDatabase<TattooDef>.AllDefsListForReading
        .Where(t => t.tattooType == TattooType.Body).ToList();

    protected override string TableTitle => "Tattoo, body";
}