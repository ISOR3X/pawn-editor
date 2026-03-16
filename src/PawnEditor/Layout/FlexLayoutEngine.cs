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
    public static float Draw<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible = null)
    {
        return node.direction switch
        {
            FlexDirection.Row => DrawRow(node, rect, runLeaf, isVisible),
            FlexDirection.Col => DrawCol(node, rect, runLeaf, isVisible),
            _ => 0f
        };
    }

    private static float DrawRow<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible)
    {
        var lines = ComputeLines(node, isVisible);
        var curY = rect.y;
        var gap = node.gapX ?? node.gap;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, gap);
            
            // Measuring pass: draw offscreen just to get heights.
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight,
                    LayoutEngineUtility.DrawNode(lines[i][j],
                        new Rect(LayoutEngineUtility.OffscreenOffset, LayoutEngineUtility.OffscreenOffset,
                            widths[j], LayoutEngineUtility.Height),
                        runLeaf, isVisible));

            // Drawing pass: now we know the line height.
            var curX = rect.x;
            for (var j = 0; j < lines[i].Count; j++)
            {
                LayoutEngineUtility.DrawNode(lines[i][j], new Rect(curX, curY, widths[j], lineHeight), runLeaf, isVisible);
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
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible)
    {
        var active = ActiveChildren(node, isVisible);
        var gapY = node.gapY ?? node.gap;

        var heights = new float[active.Count];
        var totalGrow = active.Sum(c => c.flexGrow);
        var fixedTotal = active.Sum(c => c.flexBasis > 1f ? c.flexBasis : 0f)
                         + gapY * (active.Count - 1);
        var hasKnownHeight = rect.height < LayoutEngineUtility.Height;
        var remaining = hasKnownHeight ? rect.height - fixedTotal : 0f;

        // Measuring pass: draw offscreen just to get heights.
        for (var i = 0; i < active.Count; i++)
        {
            var c = active[i];
            float h;
            if (c.flexBasis > 1f)
                h = c.flexBasis;
            else if (hasKnownHeight && c.flexGrow > 0f && totalGrow > 0f)
                h = c.flexGrow / totalGrow * remaining;
            else
                h = LayoutEngineUtility.DrawNode(c,
                    new Rect(LayoutEngineUtility.OffscreenOffset, LayoutEngineUtility.OffscreenOffset,
                        rect.width, LayoutEngineUtility.Height),
                    runLeaf, isVisible);

            heights[i] = h;
        }

        // Drawing pass: now we know the line height.
        var curY = rect.y;
        for (var i = 0; i < active.Count; i++)
        {
            LayoutEngineUtility.DrawNode(active[i], new Rect(rect.x, curY, rect.width, heights[i]), runLeaf, isVisible);
            curY += heights[i];
            if (i < active.Count - 1)
                curY += gapY;
        }

        return curY - rect.y;
    }

    private static List<List<LayoutNode<TLeaf>>> ComputeLines<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Func<TLeaf, bool>? isVisible)
    {
        var lines = new List<List<LayoutNode<TLeaf>>>();
        var currentLine = new List<LayoutNode<TLeaf>>();
        var currentBasis = 0f;

        foreach (var child in ActiveChildren(node, isVisible))
        {
            var basis = child.flexBasis;

            if (node.wrap && currentLine.Count > 0 && currentBasis + basis > 1.001f)
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

    internal static float[] ResolveWidths<TLeaf>(
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
                widths[i] += line[i].flexGrow / totalGrow * remaining;

        for (var i = 0; i < line.Count; i++)
        {
            var max = line[i].maxWidth > 1f ? line[i].maxWidth : line[i].maxWidth * totalWidth;
            widths[i] = Mathf.Min(widths[i], max);
        }

        return widths;
    }

    private static bool HasVisibleContent<TLeaf>(LayoutNode<TLeaf> node, Func<TLeaf, bool>? isVisible)
    {
        if (node.IsLeaf)
            return node.leaf == null || isVisible == null || isVisible(node.leaf);
        if (node is GroupLayoutNode<TLeaf> group)
            return group.children.Any(c => c.isActive() && HasVisibleContent(c, isVisible));
        return false;
    }
    
    internal static List<LayoutNode<TLeaf>> ActiveChildren<TLeaf>(
        GroupLayoutNode<TLeaf> node,
        Func<TLeaf, bool>? isVisible)
        => node.children
            .Where(c => c.isActive() && HasVisibleContent(c, isVisible))
            .ToList();
}