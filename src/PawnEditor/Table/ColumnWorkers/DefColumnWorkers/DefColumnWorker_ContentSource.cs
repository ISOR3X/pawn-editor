using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_ContentSource : ColumnWorker_Text<Def>
{
    protected override Color CellColor => ColoredText.SubtleGrayColor;

    public override string GetTextFor(Def thing)
    {
        return thing.modContentPack.Name;
    }
}