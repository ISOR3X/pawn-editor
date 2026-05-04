using Verse;
using Void;

namespace PawnEditor;

public static class TabUtility
{
    private static List<TabDef> AllTabDefs
    {
        get
        {
            if (field.NullOrEmpty()) field = DefDatabase<TabDef>.AllDefsListForReading;

            return field;
        }
    } = [];

    public static List<TabDef> GetTabDefsFor(IContext? context)
    {
        return [.. AllTabDefs
            .Where(def =>
            {
                var required = def.Worker.RequiredContextType;
                // Context-free workers always show.
                if (required == null) return true;
                // Typed workers only show when the context matches their declared type.
                return context != null && required.IsInstanceOfType(context);
            })
            // Pawn workers additionally filter by pawn category (Humanlike, Animal, etc.).
            .Where(def => context is not PawnContext pc
                          || def.tabCategory.HasFlag(PawnUtility.GetPawnCategory(pc.Value)))
            .OrderBy(def => def.priority)];
    }
}