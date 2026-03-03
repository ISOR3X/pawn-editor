using System.Collections.Generic;
using System.Linq;
using Verse;

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

    public static List<TabDef> GetTabDefsForPawn(Pawn? pawn)
    {
        if (pawn == null) return [];

        return AllTabDefs.Where(tabDef => tabDef.tabCategory.HasFlag(PawnUtility.GetPawnCategory(pawn)))
            .OrderBy(tabDef => tabDef.priority).ToList();
    }
}