using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using UnityEngine;

namespace PawnEditor.Layout;

[HotSwappable]
public static class LayoutEngine
{
    
    public static float Measure<TLeaf>(
        LayoutNode<TLeaf> node,
        float width,
        Func<TLeaf, Rect, float> runLeaf)
    {
        if (node._cachedMeasure.HasValue)
            return node._cachedMeasure.Value;

        var result = MeasureNode(node, new Rect(0, 0, width, 99999f), runLeaf);
        node._cachedMeasure = result;
        return result;
    }

    private static float MeasureNode<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        if (node.IsLeaf)
            return runLeaf(node.leaf!, rect);

        return node.direction switch
        {
            FlexDirection.Row => MeasureRow(node, rect, runLeaf),
            FlexDirection.Col => MeasureCol(node, rect, runLeaf),
            _ => 0f
        };
    }

    public static void Draw<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        if (node.IsLeaf)
        {
            runLeaf(node.leaf!, rect);
            return;
        }

        switch (node.direction)
        {
            case FlexDirection.Row: DrawRow(node, rect, runLeaf); break;
            case FlexDirection.Col: DrawCol(node, rect, runLeaf); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static float MeasureRow<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var lines = ComputeLines(node);
        var totalHeight = 0f;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, node.gap);
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight, Measure(lines[i][j], widths[j], runLeaf));
            totalHeight += lineHeight;
            if (i < lines.Count - 1)
                totalHeight += node.gap;
        }

        return totalHeight;
    }
    
    private static void DrawRow<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var lines = ComputeLines(node);
        var curY = rect.y;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, node.gap);
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight, Measure(lines[i][j], widths[j], runLeaf));

            var curX = rect.x;
            for (var j = 0; j < lines[i].Count; j++)
            {
                Draw(lines[i][j], new Rect(curX, curY, widths[j], lineHeight), runLeaf);
                curX += widths[j] + node.gap;
            }

            curY += lineHeight;
            if (i < lines.Count - 1)
                curY += node.gap;
        }
    }

    private static float MeasureCol<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var active = ActiveChildren(node);
        var totalHeight = 0f;
        for (var i = 0; i < active.Count; i++)
        {
            totalHeight += Measure(active[i], rect.width, runLeaf);
            if (i < active.Count - 1)
                totalHeight += node.gap;
        }

        return totalHeight;
    }



    private static void DrawCol<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var active = ActiveChildren(node);
        var curY = rect.y;
        for (var i = 0; i < active.Count; i++)
        {
            var childHeight = Measure(active[i], rect.width, runLeaf);
            Draw(active[i], new Rect(rect.x, curY, rect.width, childHeight), runLeaf);
            curY += childHeight;
            if (i < active.Count - 1)
                curY += node.gap;
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