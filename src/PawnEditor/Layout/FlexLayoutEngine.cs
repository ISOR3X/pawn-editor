using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using UnityEngine;

namespace PawnEditor.Layout;

[HotSwappable]
public static class FlexLayoutEngine
{
    private const float OffscreenOffset = 99999f;

    /// <summary>
    /// Renders a layout node and its children within the specified rectangle, using the provided function to draw leaf nodes.
    /// The layout is organized based on the direction of the node and may include rows or columns with optional gaps.
    /// </summary>
    /// <typeparam name="TLeaf">The type of the leaf node elements.</typeparam>
    /// <param name="node">The layout node to be drawn, which may consist of child nodes or a single leaf node.</param>
    /// <param name="rect">The rectangular area where the layout will be rendered, specifying position and size constraints.</param>
    /// <param name="runLeaf">A function that renders a leaf node within the given rectangle and returns the rendered height.</param>
    /// <returns>The total height occupied by the layout, including all child nodes, gaps, and line heights.</returns>
    public static float Draw<TLeaf>(
        LayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        if (node.IsLeaf)
            return runLeaf(node.leaf!, rect);

        if (node is FlexLayoutNode<TLeaf> flex)
            return flex.direction switch
            {
                FlexDirection.Row => DrawRow(flex, rect, runLeaf),
                FlexDirection.Col => DrawCol(flex, rect, runLeaf),
                _ => 0f
            };

        return 0f;
    }

    /// <summary>
    /// Draws a row layout by rendering active child nodes horizontally within the specified rectangle,
    /// arranging them from left to right while respecting the specified gap between children.
    /// </summary>
    /// <typeparam name="TLeaf">The type of the leaf node elements.</typeparam>
    /// <param name="node">The layout node containing the children to be drawn in a horizontal row format.</param>
    /// <param name="rect">The rectangle within which the row layout will be drawn, defining the starting position and size constraints.</param>
    /// <param name="runLeaf">A function that renders a leaf node within the given rectangle and returns its height.</param>
    /// <returns>The total height occupied by the row layout, which includes all child nodes, gaps, and line heights.</returns>
    private static float DrawRow<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var lines = ComputeLines(node);
        var curY = rect.y;

        for (var i = 0; i < lines.Count; i++)
        {
            var widths = ResolveWidths(lines[i], rect.width, node.gap);

            // Measuring pass: draw offscreen just to get heights
            var lineHeight = 0f;
            for (var j = 0; j < lines[i].Count; j++)
                lineHeight = Mathf.Max(lineHeight,
                    Draw(lines[i][j], new Rect(OffscreenOffset, OffscreenOffset, widths[j], OffscreenOffset), runLeaf));

            // Drawing pass: now we know the line height
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

        return curY - rect.y;
    }

    /// <summary>
    /// Draws a column layout by rendering active child nodes vertically within the specified rectangle,
    /// stacking them from top to bottom while respecting the specified gap between children.
    /// </summary>
    /// <typeparam name="TLeaf">The type of the leaf node elements.</typeparam>
    /// <param name="node">The layout node containing the children to be drawn in a vertical column format.</param>
    /// <param name="rect">The rectangle within which the column layout will be drawn, defining the starting position and size constraints.</param>
    /// <param name="runLeaf">A function that renders a leaf node and returns its height.</param>
    /// <returns>The total height occupied by the column layout, including all child nodes and gaps.</returns>
    private static float DrawCol<TLeaf>(
        FlexLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf)
    {
        var active = ActiveChildren(node);
        var curY = rect.y;

        for (var i = 0; i < active.Count; i++)
        {
            // Vertical stacked items don't need an initial Draw call to determine their height as they are independent of each other.
            var childHeight = Draw(active[i], new Rect(rect.x, curY, rect.width, 99999f), runLeaf);
            curY += childHeight;
            if (i < active.Count - 1)
                curY += node.gap;
        }

        return curY - rect.y;
    }

    /// <summary>
    /// Computes a list of lines for a given layout node, organizing its children into rows or columns
    /// based on their flex basis and the wrapping behavior of the node.
    /// </summary>
    /// <typeparam name="TLeaf">The type of the leaf node elements.</typeparam>
    /// <param name="node">The root node for which lines are to be computed. This node contains children
    /// that are organized into lines.</param>
    /// <returns>A list of lines where each line is a list of layout nodes grouped together
    /// according to the layout configuration.</returns>
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
            widths[i] = line[i].flexBasis * available;
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