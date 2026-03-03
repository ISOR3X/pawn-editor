using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnEditor;

public static class TabUtility
{
    private static List<TabDef> allTabDefs;

    private static List<TabDef> AllTabDefs
    {
        get
        {
            if (allTabDefs.NullOrEmpty())
            {
                allTabDefs = DefDatabase<TabDef>.AllDefsListForReading;
            }

            return allTabDefs;
        }
    }

    public static List<TabDef> GetTabDefsForPawn(Pawn pawn)
    {
        return AllTabDefs.Where(tabDef => tabDef.tabCategory.HasFlag(PawnUtility.GetPawnCategory(pawn))).OrderBy(tabDef => tabDef.priority).ToList();
    }
}