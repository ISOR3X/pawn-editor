using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_Beard(SectionDef def) : SectionWorker_StyleItemTable<BeardDef>(def)
{
    protected override string TableTitle => "Beard";
    protected override List<BeardDef> TableItems => DefDatabase<BeardDef>.AllDefsListForReading;
    protected override Action<Pawn, BeardDef> OnRowClick => (p, hd) => AppearanceUtility.TrySetBeardFor(hd, p);
    protected override Func<Pawn, BeardDef, bool> HighlightRow => (p, hd) => p.style.beardDef == hd;
    protected override Func<Pawn, bool> ShowTableForPawn => p => p.style.CanWantBeard;
}