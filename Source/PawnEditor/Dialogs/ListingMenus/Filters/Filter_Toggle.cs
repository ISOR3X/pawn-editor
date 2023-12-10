using System;
using UnityEngine;

namespace PawnEditor;

public class Filter_Toggle<T> : Filter<T>
{
    private readonly Func<T, bool> predicate;

    public Filter_Toggle(string label, Func<T, bool> predicate, bool enabledByDefault = false, string description = null) : base(label, enabledByDefault,
        description) =>
        this.predicate = predicate;

    protected override float Height => base.Height - (UIUtility.RegularButtonHeight + 4);

    protected override void DrawWidget(Rect rect) { }
    protected override bool MatchesInt(T item) => predicate(item);
}
