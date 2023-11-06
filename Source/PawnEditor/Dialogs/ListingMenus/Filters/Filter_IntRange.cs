using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Filter_IntRange<T> : Filter<T>
{
    private IntRange curRange;
    private readonly Func<T, int> getValue;
    private IntRange maxRange;

    public Filter_IntRange(string label, IntRange fullRange, Func<T, int> filterValue, bool enabledByDefault = false, string description = null) : base(label,
        enabledByDefault,
        description)
    {
        maxRange = curRange = fullRange;
        getValue = filterValue;
    }

    protected override void DrawWidget(Rect rect)
    {
        Widgets.IntRange(rect, GetHashCode(), ref curRange, maxRange.TrueMin, maxRange.TrueMax);
    }

    protected override bool MatchesInt(T item) => curRange.Includes(getValue(item));
}
