using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Beard(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_DefTable_Beard;
    protected override string Label => "Beard";
    protected override Def DefaultDef => BeardDefOf.NoBeard;

    protected override IEnumerable<Def> GetDefs() => DefDatabase<BeardDef>.AllDefs;
    protected override Def GetDefaultSelectedDef(Pawn p) => p.style.beardDef;

    protected override bool ShowTableForPawn(Pawn pawn) =>
        pawn.style.CanWantBeard || PawnEditorMod.Settings.restriction == Settings.RestrictionMode.None;

    protected override void OnSelectChanged(Def? def)
    {
        if (def is BeardDef beard)
            AppearanceUtility.TrySetBeardFor(beard, Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn()!);
    }

    protected override void OnRowHover(Rect rect, Def def)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p != null)
            UIUtility.DefIconPreview(rect, def, p.story.HairColor);
    }
}
