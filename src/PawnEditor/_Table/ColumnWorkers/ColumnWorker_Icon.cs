using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class ColumnWorker_Icon<T> : ColumnWorker<T> where T : class
{
    public override void DrawCell(TaffyBuilder grid, T row)
    {
        var iconFor = GetIconFor(row);
        if (!(iconFor != null))
            return;
        grid.Icon(iconFor, GetIconColor(row));
    }

    public override int Compare(T a, T b)
    {
        return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
    }

    private int GetValueToCompare(T thing)
    {
        var iconFor = GetIconFor(thing);
        return !(iconFor != null) ? int.MinValue : iconFor.GetInstanceID();
    }

    protected virtual Texture2D? GetIconFor(T thing) => null;

    protected virtual string? GetIconTip(T thing) => null;

    protected virtual Color GetIconColor(T thing) => Color.white;
}