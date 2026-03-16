using System.Collections.Generic;
using System.Linq;
using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Apparel(SectionDef def) : SectionWorker_ThingTable(def)
{
    protected override ThingTableDef TableDef => TableDefOf.PawnEditor_Apparel;
    protected override string Label => "Apparel";

    protected override IEnumerable<Thing> GetThings()
    {
        var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        List<Thing> app = [];
        for (var i = 0; i < 10; i++)
            app.AddRange(pawn?.apparel.WornApparel ?? []);
        return app;
    }
}