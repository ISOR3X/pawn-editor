using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_TattooBody(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_BodyTattoos;
    protected override string Label => "Body";
    protected override Def DefaultDef => TattooDefOf.NoTattoo_Body;

    protected override IEnumerable<Def> GetDefs() =>
        DefDatabase<TattooDef>.AllDefsListForReading.Where(t => t.tattooType == TattooType.Body);

    protected override Def GetDefaultSelectedDef(Pawn p) => p.style.BodyTattoo;

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
