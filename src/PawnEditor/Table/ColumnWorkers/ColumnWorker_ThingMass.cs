using HotSwap;
using PawnEditor.Table;
using PawnEditor.Table.ColumnWorkers;
using RimWorld;
using Taffy;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ColumnWorker_ThingMass<T>(TrackSizingFunction trackSize)
    : TextColumnWorker<T>(trackSize, t => GetMass(t).ToStringMass(), "Mass", null, null) where T : Thing
{
    private static float GetMass(T thing) => thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;

    public override int Compare(T a, T b) => GetMass(a).CompareTo(GetMass(b));
}