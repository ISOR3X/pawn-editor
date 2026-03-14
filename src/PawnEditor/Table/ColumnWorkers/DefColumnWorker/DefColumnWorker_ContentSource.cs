using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class DefColumnWorker_ContentSource : ColumnWorker_Text<Def>
{
    protected override TextAnchor Anchor => TextAnchor.MiddleLeft;
    protected override Color CellColor => ColoredText.SubtleGrayColor;

    public override int GetMinWidth(TableWorker<Def> table)
    {
        return Mathf.Max(base.GetMinWidth(table), 50);
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