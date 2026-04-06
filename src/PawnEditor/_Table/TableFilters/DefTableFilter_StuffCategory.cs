using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public class DefTableFilter_StuffCategory : DefTableFilter_FloatMenu<StuffCategoryDef>
{
    protected override string Label => "Stuff category";

    protected override IEnumerable<StuffCategoryDef> Options =>
        DefDatabase<StuffCategoryDef>.AllDefs
            .Where(sc => DefDatabase<ThingDef>.AllDefs
                .Any(td => td.stuffCategories?.Contains(sc) == true));

    protected override string GetLabel(StuffCategoryDef option) => option.LabelCap;

    protected override bool MatchesOption(Def thing, StuffCategoryDef option) =>
        thing is ThingDef thingDef && thingDef.stuffCategories?.Contains(option) == true;
}