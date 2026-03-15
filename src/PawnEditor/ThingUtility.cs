using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class ThingUtility
{
    public static readonly HashSet<ThingStyle> ThingStyles = [];

    static ThingUtility()
    {
        foreach (var styleCategoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
        foreach (var thingDefStyle in
                 styleCategoryDef.thingDefStyles) // A list of thing defs and their style def to apply.
        {
            if (ThingStyles.Select(ts => ts.thingDef).Contains(thingDefStyle.ThingDef))
            {
                // If the def already exists in the list, add the style to the existing list.
                ThingStyles.FirstOrDefault(ts => ts.thingDef == thingDefStyle.thingDef).styleDefs
                    .TryAdd(thingDefStyle.StyleDef, styleCategoryDef);
                continue;
            }


            ThingStyles.Add(new ThingStyle
            {
                thingDef = thingDefStyle.ThingDef,
                styleDefs = new Dictionary<ThingStyleDef, StyleCategoryDef>
                {
                    { thingDefStyle.styleDef, styleCategoryDef }
                }
            });
        }
    }


    public record struct ThingStyle
    {
        public ThingDef thingDef; // The thing def that has styles
        public Dictionary<ThingStyleDef, StyleCategoryDef>
            styleDefs; // The graphic is the key, the style group is the value
    }
}