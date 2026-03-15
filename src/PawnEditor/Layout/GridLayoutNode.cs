using System;
using UnityEngine;

namespace PawnEditor.Layout;

public class GridLayoutNode<TLeaf> : GroupLayoutNode<TLeaf>
{
    public required GridTrack[] columns;
    
    public int ColumnCount => columns.Length > 0 ? columns.Length : 2;
    
    public float Draw(Rect rect, Func<TLeaf, Rect, float> runLeaf, Func<TLeaf, bool>? isVisible = null)
        => GridLayoutEngine.Draw(this, rect, runLeaf, isVisible);
}