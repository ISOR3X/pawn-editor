using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Mass : ColumnWorker_Text<Thing>
{
    protected override TextAnchor RowLabelAlignment => TextAnchor.MiddleCenter;
    protected override Color CellColor => ColoredText.SubtleGrayColor;

    private static float GetMass(Thing thing) => thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;

    public override int Compare(Thing a, Thing b)
    {
        return GetMass(a).CompareTo(GetMass(b));
    }

    public override string? GetTextFor(Thing thing)
    {
        return GetMass(thing).ToStringMass();
    }
}