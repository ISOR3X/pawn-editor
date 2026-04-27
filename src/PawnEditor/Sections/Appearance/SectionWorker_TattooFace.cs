using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_TattooFace(SectionDef def) : SectionWorker_StyleItemTable<TattooDef>(def)
{
    protected override string TableTitle => "Face";

    protected override List<TattooDef> TableItems =>
    [
        .. DefDatabase<TattooDef>.AllDefsListForReading.Where(t => t.tattooType == TattooType.Face)
    ];

    protected override Action<Pawn, TattooDef> OnRowClick => (p, hd) => AppearanceUtility.TrySetTattooFor(hd, p);
    protected override Func<Pawn, TattooDef, bool> HighlightRow => (p, hd) => p.style.FaceTattoo == hd;
}