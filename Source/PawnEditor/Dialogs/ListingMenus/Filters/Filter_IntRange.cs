using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Filter_IntRange<T> : Filter<T>
{
    private readonly Func<T, int> getValue;
    private readonly Func<T, IntRange> getValueRange;
    private IntRange curRange;
    private IntRange maxRange;


    public Filter_IntRange(string label, IntRange fullRange, Func<T, int> filterValue, bool enabledByDefault = false, string description = null) : this(label,
        fullRange, enabledByDefault, description) =>
        getValue = filterValue;

    public Filter_IntRange(string label, IntRange fullRange, Func<T, IntRange> filterRange, bool enabledByDefault = false, string description = null) : this(
        label,
        fullRange, enabledByDefault, description) =>
        getValueRange = filterRange;

    protected Filter_IntRange(string label, IntRange fullRange, bool enabledByDefault = false, string description = null) : base(label,
        enabledByDefault,
        description) =>
        maxRange = curRange = fullRange;

    protected override void DrawWidget(Rect rect)
    {
        Widgets.IntRange(rect, GetHashCode(), ref curRange, maxRange.TrueMin, maxRange.TrueMax);
    }

    protected override bool MatchesInt(T item)
    {
        if (getValue != null) return curRange.Includes(getValue(item));
        if (getValueRange != null)
        {
            var itemRange = getValueRange(item);
            return curRange.Includes(itemRange.TrueMin) && curRange.Includes(itemRange.TrueMax);
        }

        throw new InvalidOperationException("Checking IntRange filter with no way to get value");
    }
}
