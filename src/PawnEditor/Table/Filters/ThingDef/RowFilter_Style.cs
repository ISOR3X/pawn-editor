using Verse;

namespace PawnEditor.Table;

[StaticConstructorOnStartup]
public class RowFilter_Style() : RowFilter_ToggleableList<ThingDef, StyleCategoryDef>("Style", s => s.LabelCap, [])
{
    private static readonly Dictionary<ThingDef, List<StyleCategoryDef>> ThingDefByCategory;

    static RowFilter_Style()
    {
        ThingDefByCategory = [];
        foreach (var styleCategoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
        foreach (var thingDefStyle in styleCategoryDef.thingDefStyles)
        {
            if (thingDefStyle.ThingDef == null) continue;
            if (!ThingDefByCategory.TryGetValue(thingDefStyle.ThingDef, out var list))
                ThingDefByCategory[thingDefStyle.ThingDef] = list = [];
            list.Add(styleCategoryDef);
        }
    }

    protected override void Initialize(IReadOnlyList<ThingDef> allRows)
    {
        allOptions =
        [
            .. allRows
                .Where(t => ThingDefByCategory.ContainsKey(t) && ThingDefByCategory[t].Count > 0)
                .SelectMany(t => ThingDefByCategory[t])
                .Distinct()
                .OrderBy(c => c.label)
        ];
    }

    protected override IReadOnlyList<StyleCategoryDef>? GetOptions(ThingDef row)
    {
        return ThingDefByCategory.GetValueOrDefault(row);
    }
}