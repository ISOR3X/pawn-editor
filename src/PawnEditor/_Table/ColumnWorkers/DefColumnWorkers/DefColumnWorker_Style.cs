using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_Style : DefColumnWorker
{
    public override bool Sortable => true;

    public override int Compare(Def a, Def b)
        => string.Compare(GetText(a), GetText(b), StringComparison.CurrentCultureIgnoreCase);

    private static string? GetText(Def def)
    {
        if (def is StyleItemDef styleItemDef) return styleItemDef.StyleItemCategory.label.CapitalizeFirst();
        return null;
    }

    protected override void DrawCellContent(Rect r, Def row)
    {
        var text = GetText(row);
        if (text == null) return;
        using (new TextBlock(TextAnchor.MiddleCenter))
            Widgets.Label(r, text);
    }
}
