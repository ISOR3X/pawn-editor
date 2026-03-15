using System.Collections.Generic;
using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Beard(SectionDef def) : SectionWorker_DefTable(def)
{
    protected override DefTableDef TableDef => TableDefOf.PawnEditor_Beards;
    protected override string Label => "Beard";
    protected override Def? DefaultDef => BeardDefOf.NoBeard;

    protected override IEnumerable<Def> GetDefs() => DefDatabase<BeardDef>.AllDefs;

    protected override bool CanShowTable(Pawn pawn) =>
        pawn.style.CanWantBeard || PawnEditorMod.Settings.restriction == Settings.RestrictionMode.None;
}