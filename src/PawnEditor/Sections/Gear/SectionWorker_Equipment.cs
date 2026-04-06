using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Equipment(SectionDef def) : SectionWorker_ThingTable(def)
{
    protected override ThingTableDef TableDef => TableDefOf.PawnEditor_ThingTable_Equipment;
    protected override string Label => "Equipment";

    protected override IEnumerable<Thing> GetThings()
    {
        var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        List<Thing> gear = [];
        gear.AddRange(pawn?.equipment?.AllEquipmentListForReading ?? []);
        gear.AddRange(pawn?.apparel.WornApparel.Where<Apparel>((Func<Apparel, bool>)(x =>
            x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt))) ?? []);
        return gear;
    }
}