using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_Style : DefColumnWorker_Text
{
    public override string? GetTextFor(Def thing)
    {
        if (thing is StyleItemDef styleItemDef) return styleItemDef.StyleItemCategory.label.CapitalizeFirst();
        return null;
    }

    public override int GetMinWidth(TableWorker<Def> def)
    {
        return Mathf.Max(base.GetMinWidth(def), 50);
    }
}