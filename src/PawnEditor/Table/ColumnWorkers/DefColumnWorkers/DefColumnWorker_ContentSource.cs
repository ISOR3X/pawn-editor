using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_ContentSource : ColumnWorker_Text<Def>
{
    protected override TextAnchor RowLabelAlignment => TextAnchor.MiddleLeft;
    protected override Color CellColor => ColoredText.SubtleGrayColor;

    public override int GetMinWidth(TableWorker<Def> table)
    {
        return Mathf.Max(base.GetMinWidth(table), 50);
    }

    public override string GetTextFor(Def thing)
    {
        return thing.modContentPack.Name;
    }
}