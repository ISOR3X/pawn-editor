using System;
using UnityEngine;

namespace PawnEditor.Layout;

public static class FlexLayoutHelper
{
    public static FlexLayoutNode<Func<Rect, float>> Row(
        LayoutNode<Func<Rect, float>>[] children,
        float gap = 4f, bool wrap = false, float flexBasis = 0f, float flexGrow = 1f, float? gapX = null,
        float? gapY = null) => new()
    {
        direction = FlexDirection.Row,
        gap = gap,
        gapX = gapX,
        gapY = gapY,
        wrap = wrap,
        flexBasis = flexBasis,
        flexGrow = flexGrow,
        children = [..children]
    };

    public static FlexLayoutNode<Func<Rect, float>> Col(
        LayoutNode<Func<Rect, float>>[] children,
        float gap = 4f, float flexBasis = 0f, float flexGrow = 1f, float? gapX = null,
        float? gapY = null) => new()
    {
        direction = FlexDirection.Col,
        gap = gap,
        gapX = gapX,
        gapY = gapY,
        flexBasis = flexBasis,
        flexGrow = flexGrow,
        children = [..children]
    };

    public static LayoutNode<Func<Rect, float>> Cell(
        Action<Rect> draw,
        float flexBasis = 0.5f,
        float flexGrow = 0f,
        float height = UIUtility.ButtonHeight) => new()
    {
        flexBasis = flexBasis,
        flexGrow = flexGrow,
        leaf = rect =>
        {
            draw(rect);
            return height;
        }
    };

    public static LayoutNode<TLeaf> When<TLeaf>(this LayoutNode<TLeaf> node, bool condition)
    {
        node.IsActive = condition;
        return node;
    }
}