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
        var resolvedWidths = ResolveTrackWidths(node.Columns, rect.width, node.GapX);
        var activeChildren = ActiveChildren(node, isVisible);
        var columnCount = node.ColumnCount;
        var curY = rect.y;

        for (var i = 0; i < activeChildren.Count; i += columnCount)
        {
            var rowItems = activeChildren.Skip(i).Take(columnCount).ToList();

            // Measuring pass: draw offscreen to determine row height.
            var rowHeight = 0f;
            for (var j = 0; j < rowItems.Count; j++)
                rowHeight = Mathf.Max(rowHeight,
                    MeasureHeight(rowItems[j], resolvedWidths[j], runLeaf));

            // Drawing pass: now we know the row height.
            var curX = rect.x;
            for (var j = 0; j < rowItems.Count; j++)
            {
                // Use FlexLayoutEngine.Draw so nested flex nodes inside grid cells work.
                FlexLayoutEngine.Draw(rowItems[j], new Rect(curX, curY, resolvedWidths[j], rowHeight), runLeaf, isVisible);
                curX += resolvedWidths[j];
                if (j < rowItems.Count - 1)
                    curX += node.GapX;
            }

            curY += rowHeight;
            if (i + columnCount < activeChildren.Count)
                curY += node.GapY;
        }

        return curY - rect.y;
    }

    internal static float[] ResolveTrackWidths(GridTrack[] columns, float availableWidth, float gapX)
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

    private static float MeasureHeight<TLeaf>(LayoutNode<TLeaf> node, float width, Func<TLeaf, Rect, float> runLeaf)
        => FlexLayoutEngine.Draw(node,
            new Rect(LayoutEngineUtils.OffscreenOffset, LayoutEngineUtils.OffscreenOffset,
                width, LayoutEngineUtils.Height),
            runLeaf);

    private static List<LayoutNode<TLeaf>> ActiveChildren<TLeaf>(
        GridLayoutNode<TLeaf> node,
        Func<TLeaf, bool>? isVisible)
        => node.Children
            .Where(c => c.isActive && HasVisibleContent(c, isVisible))
            .ToList();

    private static bool HasVisibleContent<TLeaf>(LayoutNode<TLeaf> node, Func<TLeaf, bool>? isVisible)
    {
        if (node.IsLeaf)
            return node.leaf == null || isVisible == null || isVisible(node.leaf);
        if (node is FlexLayoutNode<TLeaf> flex)
            return flex.children.Any(c => c.isActive && HasVisibleContent(c, isVisible));
        return false;
    }
}