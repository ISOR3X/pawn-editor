using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_TattooBody(SectionDef def) : SectionWorker_StyleItemTable<TattooDef>(def)
{
    protected override string TableTitle => "Body";
    protected override List<TattooDef> TableItems => [.. DefDatabase<TattooDef>.AllDefsListForReading.Where(t => t.tattooType == TattooType.Body)];

    protected override Action<Pawn, TattooDef> OnRowClick => (p, hd) => AppearanceUtility.TrySetTattooFor(hd, p);
    protected override Func<Pawn, TattooDef, bool> HighlightRow => (p, hd) => p.style.BodyTattoo == hd;

}