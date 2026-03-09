using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor.Layout;

[HotSwappable]
public static class FlexLayoutEngine
{
    private const float OffscreenOffset = -99999f;
    
    public static float Draw<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        if (node.IsLeaf)
        {
            Widgets.DrawBoxSolidWithOutline(rect, Color.clear, Color.red, 1);
            return runLeaf(node.leaf!, rect);
        }

        if (node is FlexLayoutNode<TLeaf> flex)
            return flex.direction switch
            {
                FlexDirection.Row => DrawRow(flex, rect, runLeaf),
                FlexDirection.Col => DrawCol(flex, rect, runLeaf),
                _ => 0f
            };

        return 0f;
    }

    private static float DrawRow<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var lines = ComputeLines(node);
        var curY = rect.y;
        var gap = node.gapX ?? node.gap;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, gap);

            // Measuring pass: draw offscreen just to get heights
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight,
                    Draw(lines[i][j], new Rect(OffscreenOffset, OffscreenOffset, widths[j], 99999f), runLeaf));

            // Drawing pass: now we know the line height
            var curX = rect.x;
            for (var j = 0; j < lines[i].Count; j++)
            {
                Draw(lines[i][j], new Rect(curX, curY, widths[j], lineHeight), runLeaf);
                curX += widths[j] + gap;
            }

            curY += lineHeight;
            if (i < lines.Count - 1)
                curY += node.gapY ?? node.gap;
        }

        return curY - rect.y;
    }

    private static float DrawCol<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var active = ActiveChildren(node);
        var gapY = node.gapY ?? node.gap;

        // First pass: resolve all heights
        var heights = new float[active.Count];
        var totalGrow = active.Sum(c => c.flexGrow);
        var fixedTotal = active.Sum(c => c.flexBasis > 1f ? c.flexBasis : 0f)
                         + gapY * (active.Count - 1);
        var hasKnownHeight = rect.height < 9999f;
        var remaining = hasKnownHeight
            ? rect.height - fixedTotal
            : 0f;

        for (var i = 0; i < active.Count; i++)
        {
            var c = active[i];
            float h;
            if (c.flexBasis > 1f)
                h = c.flexBasis;
            else if (hasKnownHeight && c.flexGrow > 0f && totalGrow > 0f)
                h = (c.flexGrow / totalGrow) * remaining;
            else
                h = Draw(c, new Rect(OffscreenOffset, OffscreenOffset, rect.width, 99999f), runLeaf);
            
            heights[i] = h;
        }

        // Second pass: draw with resolved heights
        var curY = rect.y;
        for (var i = 0; i < active.Count; i++)
        {
            Draw(active[i], new Rect(rect.x, curY, rect.width, heights[i]), runLeaf);
            curY += heights[i];
            if (i < active.Count - 1)
                curY += gapY;
        }

        return curY - rect.y;
    }

    private static List<List<LayoutNode<TLeaf>>> ComputeLines<TLeaf>(FlexLayoutNode<TLeaf> node)
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

        var remaining = available;
        for (var i = 0; i < line.Count; i++)
        {
            var basis = line[i].flexBasis;
            widths[i] = basis > 1f ? basis : basis * available;
            remaining -= widths[i];
        }

        var totalGrow = line.Sum(n => n.flexGrow);
        if (totalGrow > 0f && remaining > 0f)
            for (var i = 0; i < line.Count; i++)
                widths[i] += (line[i].flexGrow / totalGrow) * remaining;

        return widths;
    }

    private static List<LayoutNode<TLeaf>> ActiveChildren<TLeaf>(FlexLayoutNode<TLeaf> node)
        => node.children.Where(c => c.IsActive && (c.IsLeaf || c is FlexLayoutNode<TLeaf>)).ToList();
}