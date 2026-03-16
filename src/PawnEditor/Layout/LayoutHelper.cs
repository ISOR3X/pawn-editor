using System;
using System.Collections.Generic;
using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor.Layout;

[HotSwappable]
public static class LayoutHelper
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
        int colSpan = 1,
        int colStart = 0,
        float height = UIUtility.ButtonHeight)
        => new()
        {
            flexBasis = flexBasis,
            flexGrow = flexGrow,
            colSpan = colSpan,
            colStart = colStart,
            leaf = rect =>
            {
                draw(rect);
                return height;
            }
        };

    public static LayoutNode<Func<Rect, float>> Cell(
        Func<Rect, float> draw,
        float flexBasis = 0f,
        float flexGrow = 0f,
        int colSpan = 1,
        int colStart = 0)
        => new()
        {
            flexBasis = flexBasis,
            flexGrow = flexGrow,
            colSpan = colSpan,
            colStart = colStart,
            leaf = draw
        };

    public static IEnumerable<LayoutNode<Func<Rect, float>>> LabeledWidget(
        string label,
        Action<Rect> widget,
        Func<bool>? when = null,
        int colSpan = 2
    )
    {
        yield return Cell(rect =>
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(rect, label);
        }, height: UIUtility.ButtonHeight).When(when);
        yield return Cell(widget, colSpan: colSpan - 1).When(when);
    }

    public static IEnumerable<LayoutNode<Func<Rect, float>>> LabeledWidget(
        string label,
        Func<Rect, float> widget,
        Func<bool>? when = null,
        int colSpan = 2
    )
    {
        yield return Cell(rect =>
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(rect.TakeTopPart(UIUtility.ButtonHeight), label);
        }, height: UIUtility.ButtonHeight).When(when);
        yield return Cell(widget, colSpan: colSpan - 1).When(when);
    }

    public static LayoutNode<TLeaf> When<TLeaf>(this LayoutNode<TLeaf> node, Func<bool>? condition = null)
    {
        if (condition != null) node.isActive = condition;
        return node;
    }

    public static GridLayoutNode<Func<Rect, float>> Grid(
        GridTrack[] columns,
        float gap = 4f,
        float? gapX = null,
        float? gapY = null,
        List<LayoutNode<Func<Rect, float>>>? children = null)
        => new()
        {
            columns = columns,
            gap = gap,
            gapX = gapX,
            gapY = gapY,
            children = children ?? [],
        };
}