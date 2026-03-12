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
    private const float Height = 99999f;

    public static float Draw<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible = null)
    {
        if (node.IsLeaf)
        {
            Verse.Widgets.DrawBoxSolidWithOutline(rect, Color.clear, new Color(1f, 1f, 1f, 0.1f));
            return runLeaf(node.leaf!, rect);
        }

        if (node is FlexLayoutNode<TLeaf> flex)
            return flex.direction switch
            {
                FlexDirection.Row => DrawRow(flex, rect, runLeaf, isVisible),
                FlexDirection.Col => DrawCol(flex, rect, runLeaf, isVisible),
                _ => 0f
            };

        return 0f;
    }

    private static float DrawRow<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible
    )
    {
        var lines = ComputeLines(node, isVisible);
        var curY = rect.y;
        var gap = node.gapX ?? node.gap;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, gap);

            // Measuring pass: draw offscreen just to get heights
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight,
                    Draw(lines[i][j], new Rect(OffscreenOffset, OffscreenOffset, widths[j], Height), runLeaf,
                        isVisible));

            // Drawing pass: now we know the line height
            var curX = rect.x;
            for (var j = 0; j < lines[i].Count; j++)
            {
                Draw(lines[i][j], new Rect(curX, curY, widths[j], lineHeight), runLeaf, isVisible);
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
        Func<TLeaf, bool>? isVisible
    )
    {
        var active = ActiveChildren(node, isVisible);
        var gapY = node.gapY ?? node.gap;

        // First pass: resolve all heights
        var heights = new float[active.Count];
        var totalGrow = active.Sum(c => c.flexGrow);
        var fixedTotal = active.Sum(c => c.flexBasis > 1f ? c.flexBasis : 0f)
                         + gapY * (active.Count - 1);
        var hasKnownHeight = rect.height < Height;
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
                h = c.flexGrow / totalGrow * remaining;
            else
                h = Draw(c, new Rect(OffscreenOffset, OffscreenOffset, rect.width, Height), runLeaf, isVisible);

            heights[i] = h;
        }

        // Second pass: draw with resolved heights
        var curY = rect.y;
        for (var i = 0; i < active.Count; i++)
        {
            Draw(active[i], new Rect(rect.x, curY, rect.width, heights[i]), runLeaf, isVisible);
            curY += heights[i];
            if (i < active.Count - 1)
                curY += gapY;
        }

        return curY - rect.y;
    }

    private static List<List<LayoutNode<TLeaf>>> ComputeLines<TLeaf>(FlexLayoutNode<TLeaf> node,
        Func<TLeaf, bool>? isVisible)
    {
        var lines = new List<List<LayoutNode<TLeaf>>>();
        var currentLine = new List<LayoutNode<TLeaf>>();
        var currentBasis = 0f;

        foreach (var child in ActiveChildren(node, isVisible))
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
                widths[i] += line[i].flexGrow / totalGrow * remaining;

        return widths;
    }

    private static bool HasVisibleContent<TLeaf>(LayoutNode<TLeaf> node, Func<TLeaf, bool>? isVisible)
    {
        if (node.IsLeaf)
            return node.leaf == null || isVisible == null || isVisible(node.leaf);
        if (node is FlexLayoutNode<TLeaf> flex)
            return flex.children.Any(c => c.IsActive && HasVisibleContent(c, isVisible));
        return false;
    }

    private static List<LayoutNode<TLeaf>> ActiveChildren<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Func<TLeaf, bool>? isVisible)
    {
        return node.children.Where(c => c.IsActive && HasVisibleContent(c, isVisible)).ToList();
    }
}