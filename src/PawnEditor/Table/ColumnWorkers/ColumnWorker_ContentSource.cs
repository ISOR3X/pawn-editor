using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ColumnWorker_ContentSource : ColumnWorker_Text
{
    protected override TextAnchor Anchor => TextAnchor.MiddleLeft;
    protected override Color CellColor => ColoredText.SubtleGrayColor;

    public override int GetMinWidth(DefTable defTable)
    {
        return Mathf.Max(base.GetMinWidth(defTable), 50);
    }

    public override int Compare(Def a, Def b)
    {
        return string.Compare(a.modContentPack.Name, b.modContentPack.Name, StringComparison.Ordinal);
    }

    public override string GetTextFor(Def thing)
    {
        return thing.modContentPack.Name;
    }
}