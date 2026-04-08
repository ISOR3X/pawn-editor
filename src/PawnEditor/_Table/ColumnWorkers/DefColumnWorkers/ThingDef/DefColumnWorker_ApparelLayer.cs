using HotSwap;
using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class DefColumnWorker_ApparelLayer : DefColumnWorker
{
    public override bool Sortable => true;

    public override int Compare(Def a, Def b)
        => string.Compare(GetText(a), GetText(b), StringComparison.CurrentCultureIgnoreCase);

    private static string? GetText(Def def)
    {
        return def is not ThingDef thing || thing.apparel == null
            ? null
            : string.Join(", ", thing.apparel.bodyPartGroups.Select(b => b.LabelCap));
    }

    protected override void DrawCellContent(Rect r, Def row)
    {
        var text = GetText(row);
        if (text == null) return;
        using (new TextBlock(TextAnchor.MiddleLeft))
            Verse.Widgets.Label(r, text.Colorize(ColoredText.SubtleGrayColor));
    }
}
