using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_Hair(SectionDef def) : SectionWorker_StyleItemTable<HairDef>(def)
{
    protected override string TableTitle => "Hair";
    protected override List<HairDef> TableItems => DefDatabase<HairDef>.AllDefsListForReading;
    protected override Action<Pawn, HairDef> OnRowClick => (p, hd) => AppearanceUtility.TrySetHairFor(hd, p);
    protected override Func<Pawn, HairDef, bool> HighlightRow => (p, hd) => p.story.hairDef == hd;
}