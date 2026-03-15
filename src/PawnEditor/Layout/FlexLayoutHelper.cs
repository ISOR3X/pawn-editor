using System;
using System.Collections.Generic;
using HotSwap;
using UnityEngine;

namespace PawnEditor.Layout;

[HotSwappable]
public static class FlexLayoutHelper
{
    public static FlexLayoutNode<Func<Rect, float>> Row(
        LayoutNode<Func<Rect, float>>[] children,
        float gap = 4f, bool wrap = false, float flexBasis = 0f, float flexGrow = 0f, float? gapX = null,
        float? gapY = null, float? childFlexBasis = null, float? childFlexGrow = null)
    {
        var node = new FlexLayoutNode<Func<Rect, float>>
        {
            direction = FlexDirection.Row,
            gap = gap,
            flexBasis = flexBasis,
            flexGrow = flexGrow,
            childFlexGrow = childFlexGrow,
            childFlexBasis = childFlexBasis,
            wrap = wrap,
            gapX = gapX,
            gapY = gapY,
            children = [..children]
        };

        node.ApplyChildDefaults();
        return node;
    }

    public static FlexLayoutNode<Func<Rect, float>> Col(
        LayoutNode<Func<Rect, float>>[] children,
        float gap = 4f, float flexBasis = 0f, float flexGrow = 0f, float? gapX = null,
        float? gapY = null, float? childFlexBasis = null, float? childFlexGrow = null)
    {
        var node = new FlexLayoutNode<Func<Rect, float>>
        {
            direction = FlexDirection.Col,
            gap = gap,
            gapX = gapX,
            gapY = gapY,
            flexBasis = flexBasis,
            flexGrow = flexGrow,
            childFlexGrow = childFlexGrow,
            childFlexBasis = childFlexBasis,
            children = [..children]
        };

        node.ApplyChildDefaults();
        return node;
    }

    public static LayoutNode<Func<Rect, float>> Cell(
        Action<Rect> draw,
        float flexBasis = 0f,
        float flexGrow = 0f,
        float height = UIUtility.ButtonHeight)
        => new()
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
        node.isActive = condition;
        return node;
    }
    
    public static GridLayoutNode<Func<Rect, float>> Grid(
        GridTrack[] columns,
        float gapX = 0f,
        float gapY = 0f,
        List<LayoutNode<Func<Rect, float>>>? children = null)
        => new()
        {
            Columns = columns,
            GapX = gapX,
            GapY = gapY,
            Children = children ?? [],
        };
}