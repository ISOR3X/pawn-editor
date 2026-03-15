using System;
using UnityEngine;

namespace PawnEditor.Layout;

public static class LayoutEngineUtility
{
    public const float OffscreenOffset = -99999f;
    public const float Height = 99999f;

    public static float DrawNode<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible = null)
    {
        if (node.IsLeaf)
        {
            if (PawnEditorMod.Settings.drawDebug) Verse.Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, 0.1f));
            return runLeaf(node.leaf!, rect);
        }

        if (node is FlexLayoutNode<TLeaf> flex)
            return FlexLayoutEngine.Draw(flex, rect, runLeaf, isVisible);
        if (node is GridLayoutNode<TLeaf> grid)
            return GridLayoutEngine.Draw(grid, rect, runLeaf, isVisible);

        return 0f;
    }
}