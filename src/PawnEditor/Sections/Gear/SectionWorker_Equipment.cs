
using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_Equipment(SectionDef def) : SectionWorker_Apparel<Thing>(def)
{
    protected override Func<Pawn, List<Thing>> TableItems => GetEquipment;
    protected override string TableTitle => "Equipment";

    private static List<Thing> GetEquipment(Pawn pawn)
    {
        List<Thing> gear = [];
        gear.AddRange(pawn.equipment?.AllEquipmentListForReading ?? []);
        gear.AddRange(pawn.apparel?.WornApparel.Where<Apparel>(IsBelt) ?? []);

        return gear;

        static bool IsBelt(Apparel apparel)
        {
            return apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Belt);
        }
    }
}