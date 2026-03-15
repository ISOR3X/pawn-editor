using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_Style : ColumnWorker_Text<Def>
{
    protected override TextAnchor RowLabelAlignment => TextAnchor.MiddleCenter;

    public override string? GetTextFor(Def thing)
    {
        if (thing is StyleItemDef styleItemDef) return styleItemDef.StyleItemCategory.label.CapitalizeFirst();
        return null;
    }
}