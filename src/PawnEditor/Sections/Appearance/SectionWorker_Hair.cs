using HotSwap;
using RimWorld;
using UnityEngine;
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

    protected override void OnSelectChanged(Def? def)
    {
        if (def is HairDef hair)
            AppearanceUtility.TrySetHairFor(hair, Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn()!);
    }

    protected override void OnRowHover(Rect rect, Def def)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p != null)
            UIUtility.DefIconPreview(rect, def, p.story.HairColor);
    }
}
