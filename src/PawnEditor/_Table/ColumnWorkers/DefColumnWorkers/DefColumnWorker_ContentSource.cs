using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_ContentSource : DefColumnWorker
{
    public override bool Sortable => true;

    public override int Compare(Def a, Def b)
        => string.Compare(a.modContentPack?.Name, b.modContentPack?.Name, StringComparison.CurrentCultureIgnoreCase);

    protected override void DrawCellContent(Rect r, Def row)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
            Verse.Widgets.Label(r, (row.modContentPack?.Name ?? "").Colorize(ColoredText.SubtleGrayColor));
    }
}
