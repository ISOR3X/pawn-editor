// Port of taffy/src/compute/grid/implicit_grid.rs
//
// Estimates the number of rows and columns in the grid by scanning child placements.
// This is a performance optimisation — reduces allocations by pre-sizing vectors.
// The explicit and negative-implicit estimates are exact; positive-implicit is a lower bound
// (auto-placement may expand it further).

namespace Taffy;

internal static class ImplicitGrid
{
    /// <summary>
    ///     Estimates the total TrackCounts (columns, rows) required by the grid by scanning all children.
    /// </summary>
    public static (TrackCounts cols, TrackCounts rows) ComputeGridSizeEstimate(
        ushort explicitColCount,
        ushort explicitRowCount,
        TaffyTree tree,
        NodeId node)
    {
        var colMin = new OriginZeroLine(0);
        var colMax = new OriginZeroLine(0);
        ushort colMaxSpan = 0;
        var rowMin = new OriginZeroLine(0);
        var rowMax = new OriginZeroLine(0);
        ushort rowMaxSpan = 0;

        var childCount = tree.ChildCount(node);
        for (var i = 0; i < childCount; i++)
        {
            var child = tree.ChildAt(node, i);
            var style = tree.GetStyle(child);

            var (childColMin, childColMax, childColSpan) =
                ChildMinLineMaxLineSpan(style.gridColumn, explicitColCount);
            var (childRowMin, childRowMax, childRowSpan) =
                ChildMinLineMaxLineSpan(style.gridRow, explicitRowCount);

            if (childColMin < colMin) colMin = childColMin;
            if (childColMax > colMax) colMax = childColMax;
            if (childColSpan > colMaxSpan) colMaxSpan = childColSpan;
            if (childRowMin < rowMin) rowMin = childRowMin;
            if (childRowMax > rowMax) rowMax = childRowMax;
            if (childRowSpan > rowMaxSpan) rowMaxSpan = childRowSpan;
        }

        var negColTracks = colMin.ImpliedNegativeImplicitTracks();
        var explColTracks = explicitColCount;
        var posColTracks = colMax.ImpliedPositiveImplicitTracks(explicitColCount);
        var negRowTracks = rowMin.ImpliedNegativeImplicitTracks();
        var explRowTracks = explicitRowCount;
        var posRowTracks = rowMax.ImpliedPositiveImplicitTracks(explicitRowCount);

        // Ensure total inline tracks are at least as large as the largest item span
        var totColTracks = (ushort)(negColTracks + explColTracks + posColTracks);
        if (totColTracks < colMaxSpan)
            posColTracks = (ushort)(colMaxSpan - explColTracks - negColTracks);

        var totRowTracks = (ushort)(negRowTracks + explRowTracks + posRowTracks);
        if (totRowTracks < rowMaxSpan)
            posRowTracks = (ushort)(rowMaxSpan - explRowTracks - negRowTracks);

        return (
            new TrackCounts(negColTracks, explColTracks, posColTracks),
            new TrackCounts(negRowTracks, explRowTracks, posRowTracks)
        );
    }

    /// <summary>
    ///     For a single item, produce a conservative estimate of its min/max grid lines (in OriginZero
    ///     coordinates) and the number of tracks it spans (for indefinitely-placed items only).
    /// </summary>
    private static (OriginZeroLine min, OriginZeroLine max, ushort span) ChildMinLineMaxLineSpan(
        Line<GridPlacement> line, ushort explicitTrackCount)
    {
        // Convert to OriginZero coordinates (ignoring named lines — not supported)
        var oz = line.IntoOriginZeroIgnoringNamed(explicitTrackCount);
        var s = oz.Start;
        var e = oz.End;

        OriginZeroLine min;
        OriginZeroLine max;

        if (s.IsLine && e.IsLine)
        {
            var sl = s.AsLine();
            var el = e.AsLine();
            if (sl.Value == el.Value)
            {
                min = sl;
                max = new OriginZeroLine((short)(sl.Value + 1));
            }
            else
            {
                min = sl < el ? sl : el;
                max = sl > el ? sl : el;
            }
        }
        else if (s.IsLine)
        {
            var sl = s.AsLine();
            min = sl;
            max = e.IsSpan
                ? sl + e.AsSpan()
                : new OriginZeroLine((short)(sl.Value + 1));
        }
        else if (e.IsLine)
        {
            var el = e.AsLine();
            min = s.IsSpan ? el - s.AsSpan() : el;
            max = el;
        }
        else
        {
            // Both Auto/Span — don't affect min/max estimates
            min = new OriginZeroLine(0);
            max = new OriginZeroLine(0);
        }

        // Span is only meaningful for indefinitely-placed items (both ends are Auto or Span)
        ushort span;
        if (!s.IsLine && !e.IsLine)
        {
            if (s.IsSpan) span = s.AsSpan();
            else if (e.IsSpan) span = e.AsSpan();
            else span = 1;
        }
        else
        {
            span = 1;
        }

        return (min, max, span);
    }
}