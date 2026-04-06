using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class DefColumnWorker_ApparelLayer : ColumnWorker_Text<Def>
{
    protected override Color CellColor => ColoredText.SubtleGrayColor;
    protected override TextAnchor HeaderLabelAlignment => TextAnchor.MiddleLeft;

    public override string? GetTextFor(Def def)
    {
        return def is not ThingDef thing || thing.apparel == null 
            ? null 
            : string.Join(", ", thing.apparel.bodyPartGroups.Select(b => b.LabelCap));
    }
}