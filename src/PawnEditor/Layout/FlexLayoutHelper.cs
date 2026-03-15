using System;
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
            wrap = wrap,
            gapX = gapX,
            gapY = gapY,
            children = [..children]
        };

        if (childFlexBasis.HasValue || childFlexGrow.HasValue)
            foreach (var child in node.children)
            {
                if (childFlexBasis.HasValue && child.flexBasis == 0f)
                    child.flexBasis = childFlexBasis.Value;
                if (childFlexGrow.HasValue && child.flexGrow == 0f)
                    child.flexGrow = childFlexGrow.Value;
            }

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
    {
        return new LayoutNode<Func<Rect, float>>
        {
            flexBasis = flexBasis,
            flexGrow = flexGrow,
            leaf = rect =>
            {
                draw(rect);
                return height;
            }
        };
    }

    public static LayoutNode<TLeaf> When<TLeaf>(this LayoutNode<TLeaf> node, bool condition)
    {
        node.isActive = condition;
        return node;
    }
}