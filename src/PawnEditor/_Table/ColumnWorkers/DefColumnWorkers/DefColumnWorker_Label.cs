using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_Label : DefColumnWorker
{
    public override bool Sortable => true;

    public override int Compare(Def a, Def b)
        => string.Compare(GetText(a), GetText(b), StringComparison.CurrentCultureIgnoreCase);

    private static string GetText(Def def) => def.label.CapitalizeFirst() ?? def.ReadableDefName();

    public override string? GetSearchText(Def row) => GetText(row);

    protected override void DrawCellContent(Rect r, Def row)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
            Verse.Widgets.Label(r, GetText(row));
    }
}
