using HotSwap;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Mass : ThingColumnWorker
{
    public override bool Sortable => true;

    private static float GetMass(Thing thing) => thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;

    public override int Compare(Thing a, Thing b) => GetMass(a).CompareTo(GetMass(b));

    protected override void DrawCellContent(Rect r, Thing row)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(r, GetMass(row).ToStringMass().Colorize(ColoredText.SubtleGrayColor));
    }
}
