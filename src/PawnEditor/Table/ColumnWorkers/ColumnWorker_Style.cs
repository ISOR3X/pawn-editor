using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ColumnWorker_Style : ColumnWorker_Text
{
    public override string? GetTextFor(Def thing)
    {
        if (thing is StyleItemDef styleItemDef) return styleItemDef.StyleItemCategory.label.CapitalizeFirst();
        return null;
    }

    public override int GetMinWidth(DefTable defTable)
    {
        return Mathf.Max(base.GetMinWidth(defTable), 50);
    }
}