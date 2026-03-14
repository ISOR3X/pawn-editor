using System.Collections.Generic;
using System.Linq;
using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_TattooBody(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_Beards;
    protected override string Label => "Body";
    protected override Def? DefaultDef => TattooDefOf.NoTattoo_Body;

    protected override IEnumerable<Def> GetDefs() =>
        DefDatabase<TattooDef>.AllDefsListForReading.Where(t => t.tattooType == TattooType.Body);
}