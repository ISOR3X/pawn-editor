using System.Collections.Generic;
using System.Linq;
using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_TattooFace(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_FaceTattoos;
    protected override string Label => "Face";
    protected override Def? DefaultDef => TattooDefOf.NoTattoo_Face;

    protected override IEnumerable<Def> GetDefs() =>
        DefDatabase<TattooDef>.AllDefsListForReading.Where(t => t.tattooType == TattooType.Face);
}