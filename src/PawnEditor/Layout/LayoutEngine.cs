using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawnEditor.Layout;

public static class LayoutEngine
{
    public static float Measure<TLeaf>(
        LayoutNode<TLeaf> node,
        float width,
        Func<TLeaf, float, float> measureLeaf)
    {
        if (node.IsLeaf)
            return measureLeaf(node.leaf!, width);

        return node.direction switch
        {
            FlexDirection.Row => MeasureRow(node, width, measureLeaf),
            FlexDirection.Col => MeasureCol(node, width, measureLeaf),
            _ => 0f
        };
    }

    public static void Draw<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, float, float> measureLeaf,
        Action<TLeaf, Rect> drawLeaf)
    {
        if (node.IsLeaf)
        {
            drawLeaf(node.leaf!, rect);
            return;
        }

        switch (node.direction)
        {
            case FlexDirection.Row: DrawRow(node, rect, measureLeaf, drawLeaf); break;
            case FlexDirection.Col: DrawCol(node, rect, measureLeaf, drawLeaf); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static List<List<LayoutNode<TLeaf>>> ComputeLines<TLeaf>(LayoutNode<TLeaf> node)
    {
        var lines = new List<List<LayoutNode<TLeaf>>>();
        var currentLine = new List<LayoutNode<TLeaf>>();
        var currentBasis = 0f;

        foreach (var child in ActiveChildren(node))
        {
            var basis = child.flexBasis;

            if (node.wrap && currentLine.Count > 0 && currentBasis + basis > 1f + float.Epsilon)
            {
                lines.Add(currentLine);
                currentLine = [];
                currentBasis = 0f;
            }

            currentLine.Add(child);
            currentBasis += basis;
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    private static float MeasureRow<TLeaf>(
        LayoutNode<TLeaf> node,
        float width,
        Func<TLeaf, float, float> measureLeaf)
    {
        var lines = ComputeLines(node);
        var totalHeight = 0f;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], width, node.gap);
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight, Measure(lines[i][j], widths[j], measureLeaf));
            totalHeight += lineHeight;
            if (i < lines.Count - 1)
                totalHeight += node.gap;
        }

        return totalHeight;
    }

    private static float MeasureCol<TLeaf>(
        LayoutNode<TLeaf> node,
        float width,
        Func<TLeaf, float, float> measureLeaf)
    {
        var active = ActiveChildren(node);
        var totalHeight = 0f;
        for (var i = 0; i < active.Count; i++)
        {
            totalHeight += Measure(active[i], width, measureLeaf);
            if (i < active.Count - 1)
                totalHeight += node.gap;
        }

        return totalHeight;
    }

    private static void DrawRow<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, float, float> measureLeaf,
        Action<TLeaf, Rect> drawLeaf)
    {
        var lines = ComputeLines(node);
        var curY = rect.y;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, node.gap);
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight, Measure(lines[i][j], widths[j], measureLeaf));

            var curX = rect.x;
            for (var j = 0; j < lines[i].Count; j++)
            {
                Draw(lines[i][j], new Rect(curX, curY, widths[j], lineHeight), measureLeaf, drawLeaf);
                curX += widths[j] + node.gap;
            }

            curY += lineHeight;
            if (i < lines.Count - 1)
                curY += node.gap;
        }
    }

    private static void DrawCol<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, float, float> measureLeaf,
        Action<TLeaf, Rect> drawLeaf)
    {
        var active = ActiveChildren(node);
        var curY = rect.y;
        for (var i = 0; i < active.Count; i++)
        {
            var childHeight = Measure(active[i], rect.width, measureLeaf);
            Draw(active[i], new Rect(rect.x, curY, rect.width, childHeight), measureLeaf, drawLeaf);
            curY += childHeight;
            if (i < active.Count - 1)
                curY += node.gap;
        }
    }

    private static float[] ResolveWidths<TLeaf>(
        List<LayoutNode<TLeaf>> line,
        float totalWidth,
        float gap)
    {
        var widths = new float[line.Count];
        var totalGap = gap * (line.Count - 1);
        var available = totalWidth - totalGap;

        // First pass: assign a flex basis as absolute widths
        var remaining = available;
        for (var i = 0; i < line.Count; i++)
        {
            widths[i] = line[i].flexBasis * available;
            remaining -= widths[i];
        }

        // Second pass: distribute remaining space by flexGrow
        var totalGrow = line.Sum(n => n.flexGrow);
        if (totalGrow > 0f && remaining > 0f)
            for (var i = 0; i < line.Count; i++)
                widths[i] += (line[i].flexGrow / totalGrow) * remaining;

        return widths;
    }

    private static List<LayoutNode<TLeaf>> ActiveChildren<TLeaf>(LayoutNode<TLeaf> node)
        => node.children.Where(c => c.IsActive && (c.IsLeaf || c.children.Count > 0)).ToList();
}