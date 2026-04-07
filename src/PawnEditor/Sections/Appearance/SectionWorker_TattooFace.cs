using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_TattooFace(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_FaceTattoos;
    protected override string Label => "Face";
    protected override Def DefaultDef => TattooDefOf.NoTattoo_Face;

    protected override IEnumerable<Def> GetDefs() =>
        DefDatabase<TattooDef>.AllDefsListForReading.Where(t => t.tattooType == TattooType.Face);

    protected override Def GetDefaultSelectedDef(Pawn p) => p.style.FaceTattoo;

    protected override void OnSelectChanged(Def? def)
    {
        if (def is TattooDef tattoo)
            AppearanceUtility.TrySetTattooFor(tattoo, Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn()!);
    }

    protected override void OnRowHover(Rect rect, Def def)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p != null)
            UIUtility.DefIconPreview(rect, def, Color.white);
    }
}
