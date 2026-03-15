using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using UnityEngine;

namespace PawnEditor.Layout;

[HotSwappable]
public static class GridLayoutEngine
{
    public static float Draw<TLeaf>(
        GridLayoutNode<TLeaf> node,
        Rect rect,
        Func<TLeaf, Rect, float> runLeaf,
        Func<TLeaf, bool>? isVisible = null)
    {
        if (node.columns.Length == 0) return 0f;

        var gap = node.gapX ?? node.gap;
        var resolvedWidths = ResolveTrackWidths(node.columns, rect.width, gap);
        var active = FlexLayoutEngine.ActiveChildren(node, isVisible);
        var columnCount = node.ColumnCount;
        var curY = rect.y;

        var i = 0;
        while (i < active.Count)
        {
            // Build row by accumulating colSpans until columnCount is filled
            var rowItems = new List<(LayoutNode<TLeaf> node, int col, int span)>();
            var col = 0;
            while (col < columnCount && i < active.Count)
            {
                var childNode = active[i];
                var start = childNode.colStart > 0 ? childNode.colStart - 1 : col; // convert to 0-indexed

                if (start < col)
                    break; // can't fit on this row, push to the next

                col = start; // jump to the explicit start column
                var span = Math.Min(childNode.colSpan, columnCount - col);
                rowItems.Add((childNode, col, span));
                col += span;
                i++;
            }

            // Measuring pass: draw offscreen to determine row height
            var rowHeight = 0f;
            foreach (var (child, itemCol, span) in rowItems)
            {
                var spanWidth = resolvedWidths.Skip(itemCol).Take(span).Sum() + gap * (span - 1);
                rowHeight = Mathf.Max(rowHeight,
                    LayoutEngineUtility.DrawNode(child,
                        new Rect(LayoutEngineUtility.OffscreenOffset, LayoutEngineUtility.OffscreenOffset,
                            spanWidth, LayoutEngineUtility.Height),
                        runLeaf, isVisible));
            }

            // Drawing pass: now we know the row height
            foreach (var (child, itemCol, span) in rowItems)
            {
                var spanWidth = resolvedWidths.Skip(itemCol).Take(span).Sum() + gap * (span - 1);
                var curX = rect.x + resolvedWidths.Take(itemCol).Sum() + gap * itemCol;
                LayoutEngineUtility.DrawNode(child, new Rect(curX, curY, spanWidth, rowHeight), runLeaf, isVisible);
            }

            curY += rowHeight;
            if (i < active.Count)
                curY += node.gapY ?? node.gap;
        }

        return curY - rect.y;
    }

    private static float[] ResolveTrackWidths(GridTrack[] columns, float availableWidth, float gapX)
    {
        var columnCount = columns.Length;
        var totalGap = gapX * (columnCount - 1);
        var remaining = availableWidth - totalGap;

        var totalFr = 0f;
        foreach (var track in columns)
        {
            if (!track.isFractional)
                remaining -= track.value;
            else
                totalFr += track.value;
        }

        remaining = Mathf.Max(remaining, 0f);

        var widths = new float[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            var track = columns[i];
            widths[i] = track.isFractional
                ? (totalFr > 0f ? track.value / totalFr * remaining : 0f)
                : track.value;
        }

        return widths;
    }
}