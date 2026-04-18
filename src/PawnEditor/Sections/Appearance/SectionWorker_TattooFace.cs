using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_TattooFace(SectionDef def) : SectionWorker_Hair<TattooDef>(def)
{
    protected override List<TattooDef> TableItems => DefDatabase<TattooDef>.AllDefsListForReading
        .Where(t => t.tattooType == TattooType.Face).ToList();

    protected override string TableTitle => "Tattoo, face";
}