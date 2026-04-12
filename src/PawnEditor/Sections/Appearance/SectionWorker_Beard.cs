using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Beard(SectionDef def) : SectionWorker_Hair<BeardDef>(def)
{
    protected override List<BeardDef> TableItems => DefDatabase<BeardDef>.AllDefsListForReading;
    protected override string TableTitle => "Beard";
}