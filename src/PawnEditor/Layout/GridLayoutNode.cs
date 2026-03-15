using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawnEditor.Layout;

public class GridLayoutNode<TLeaf> : LayoutNode<TLeaf>
{
    public required GridTrack[] Columns;

    public float GapX;
    public float GapY;

    /// <summary>
    /// Children are <see cref="LayoutNode{TLeaf}"/> so .When() and isActive work
    /// consistently with the flex engine.
    /// </summary>
    public List<LayoutNode<TLeaf>> Children = [];

    public int ColumnCount => Columns.Length;

    public float Draw(
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible = null)
        => GridLayoutEngine.Draw(this, rect, runLeaf, isVisible);
}