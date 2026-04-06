using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Hair(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_DefTable_Hair;
    protected override string Label => "Hair";
    protected override Def DefaultDef => HairDefOf.Bald;

    protected override IEnumerable<Def> GetDefs() => DefDatabase<HairDef>.AllDefs;
    protected override Def GetDefaultSelectedDef(Pawn p) => p.story.hairDef;
}