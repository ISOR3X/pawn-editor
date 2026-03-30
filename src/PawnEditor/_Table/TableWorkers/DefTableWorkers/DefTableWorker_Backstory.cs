using RimWorld;
using Verse;

namespace PawnEditor;

public class DefTableWorker_Backstory(DefTableDef def, Func<IEnumerable<Def>> thingsGetter, Pawn pawn, Def? defaultThing = null)
    : FilteredDefTableWorker(def, thingsGetter, defaultThing)
{
    protected override string GetTooltipFor(Def thing)
    {
        if (thing is not BackstoryDef backstoryDef) return "";

        var output = backstoryDef.FullDescriptionFor(pawn).Resolve();
        var cats = string.Join(", ", backstoryDef.spawnCategories.Select(sc => sc));
        output += "\n\n" + "PawnEditor.Categories".Translate().CapitalizeFirst() + ": \n" + cats;
        return output;

    }
}