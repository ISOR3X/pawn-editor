// Port of taffy/src/compute/grid/placement.rs
//
// Implements the CSS Grid Item Placement Algorithm (§8.5).
// Places items into the grid, potentially expanding the implicit grid as required.
// https://www.w3.org/TR/css-grid-2/#auto-placement-algo

using System.Collections.Generic;

namespace PawnEditor.TaffySharp
{
    internal static class Placement
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool AxisIsReversed(Direction direction, AbsoluteAxis axis) =>
            direction == Direction.Rtl && axis == AbsoluteAxis.Horizontal;

        private static OriginZeroLine AdvancePosition(OriginZeroLine pos, bool reversed) =>
            reversed
                ? new OriginZeroLine((short)(pos.Value - 1))
                : new OriginZeroLine((short)(pos.Value + 1));

        private static OriginZeroLine SearchStartLine(
            OriginZeroLine gridStart, OriginZeroLine gridEnd, bool reversed) =>
            reversed ? new OriginZeroLine((short)(gridEnd.Value - 1)) : gridStart;

        private static Line<OriginZeroLine> ResolveIndefiniteSpan(
            OriginZeroLine pos, ushort span, bool reversed) =>
            reversed
                ? new Line<OriginZeroLine>(
                    new OriginZeroLine((short)(pos.Value - span + 1)),
                    new OriginZeroLine((short)(pos.Value + 1)))
                : new Line<OriginZeroLine>(pos, pos + span);

        private static Line<OriginZeroLine> MaybeMirrorSpan(
            Line<OriginZeroLine> span, AbsoluteAxis axis, Direction direction, ushort explicitColCount)
        {
            if (axis == AbsoluteAxis.Horizontal && direction == Direction.Rtl)
            {
                short n = (short)explicitColCount;
                return new Line<OriginZeroLine>(
                    new OriginZeroLine((short)(n - span.End.Value)),
                    new OriginZeroLine((short)(n - span.Start.Value)));
            }
            return span;
        }

        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>
        /// CSS Grid Item Placement Algorithm (§8.5).
        /// Assigns each child a definite grid-row and grid-column span, expanding the implicit grid as needed.
        /// Results are stored in <paramref name="items"/> and <paramref name="occupancy"/>.
        /// </summary>
        public static void PlaceGridItems(
            CellOccupancyMatrix occupancy,
            List<GridItem> items,
            TaffyTree tree,
            NodeId node,
            Direction direction,
            GridAutoFlow autoFlow,
            AlignItems alignItems,
            AlignItems justifyItems,
            Style containerStyle)
        {
            var primaryAxis   = autoFlow.PrimaryAxis();
            var secondaryAxis = primaryAxis.OtherAxis();
            ushort explicitColCount = occupancy.TrackCounts(AbsoluteAxis.Horizontal).Explicit;
            ushort explicitRowCount = occupancy.TrackCounts(AbsoluteAxis.Vertical).Explicit;

            int childCount = tree.ChildCount(node);

            // ── Step 1: Place children with definite positions in BOTH axes ───
            for (int i = 0; i < childCount; i++)
            {
                var child = tree.ChildAt(node, i);
                var style = tree.GetStyle(child);

                if (style.display == Display.None) continue;
                if (style.position == Position.Absolute) continue;

                var colPlacement = style.gridColumn.IntoOriginZeroIgnoringNamed(explicitColCount);
                var rowPlacement = style.gridRow.IntoOriginZeroIgnoringNamed(explicitRowCount);
                var placement = new InBothAbsAxis<Line<OriginZeroGridPlacement>>(colPlacement, rowPlacement);

                if (!placement.Horizontal.IsDefinite() || !placement.Vertical.IsDefinite()) continue;

                var (rowSpan, colSpan) = PlaceDefiniteGridItem(placement, primaryAxis, direction, explicitColCount);
                RecordGridPlacement(occupancy, items, child, i, style, alignItems, justifyItems,
                    primaryAxis, rowSpan, colSpan, CellOccupancyState.DefinitelyPlaced);
            }

            // ── Step 2: Place children with definite secondary axis, indefinite primary ──
            for (int i = 0; i < childCount; i++)
            {
                var child = tree.ChildAt(node, i);
                var style = tree.GetStyle(child);

                if (style.display == Display.None) continue;
                if (style.position == Position.Absolute) continue;

                var colPlacement = style.gridColumn.IntoOriginZeroIgnoringNamed(explicitColCount);
                var rowPlacement = style.gridRow.IntoOriginZeroIgnoringNamed(explicitRowCount);
                var placement = new InBothAbsAxis<Line<OriginZeroGridPlacement>>(colPlacement, rowPlacement);

                if (!placement.Get(secondaryAxis).IsDefinite() || placement.Get(primaryAxis).IsDefinite()) continue;

                var (primarySpan, secondarySpan) = PlaceDefiniteSecondaryAxisItem(
                    occupancy, placement, autoFlow, direction, explicitColCount);
                RecordGridPlacement(occupancy, items, child, i, style, alignItems, justifyItems,
                    primaryAxis, primarySpan, secondarySpan, CellOccupancyState.AutoPlaced);
            }

            // ── Step 4: Place remaining children ─────────────────────────────
            // (children with indefinite secondary axis, may or may not have definite primary)
            var primaryAxisIsReversed   = AxisIsReversed(direction, primaryAxis);
            var primaryGridStart   = occupancy.TrackCounts(primaryAxis).ImplicitStartLine();
            var primaryGridEnd     = occupancy.TrackCounts(primaryAxis).ImplicitEndLine();
            var secondaryGridStart = occupancy.TrackCounts(secondaryAxis).ImplicitStartLine();
            var secondaryGridEnd   = occupancy.TrackCounts(secondaryAxis).ImplicitEndLine();

            var gridStartPrimary   = SearchStartLine(primaryGridStart, primaryGridEnd, primaryAxisIsReversed);
            var gridStartSecondary = SearchStartLine(secondaryGridStart, secondaryGridEnd,
                AxisIsReversed(direction, secondaryAxis));

            var gridPos = (primary: gridStartPrimary, secondary: gridStartSecondary);

            for (int i = 0; i < childCount; i++)
            {
                var child = tree.ChildAt(node, i);
                var style = tree.GetStyle(child);

                if (style.display == Display.None) continue;
                if (style.position == Position.Absolute) continue;

                var colPlacement = style.gridColumn.IntoOriginZeroIgnoringNamed(explicitColCount);
                var rowPlacement = style.gridRow.IntoOriginZeroIgnoringNamed(explicitRowCount);
                var placement = new InBothAbsAxis<Line<OriginZeroGridPlacement>>(colPlacement, rowPlacement);

                // Only process items that are NOT definitively placed on the secondary axis
                if (placement.Get(secondaryAxis).IsDefinite()) continue;

                var (primarySpan, secondarySpan) = PlaceIndefinitelyPositionedItem(
                    occupancy, placement, autoFlow, gridPos, direction, explicitColCount);

                RecordGridPlacement(occupancy, items, child, i, style, alignItems, justifyItems,
                    primaryAxis, primarySpan, secondarySpan, CellOccupancyState.AutoPlaced);

                // Update cursor: dense resets to start; sparse advances
                if (autoFlow.IsDense())
                {
                    gridPos = (gridStartPrimary, gridStartSecondary);
                }
                else if (!primaryAxisIsReversed)
                {
                    gridPos = (primarySpan.End, secondarySpan.Start);
                }
                else
                {
                    gridPos = (primarySpan.Start, secondarySpan.Start);
                }
            }
        }

        // ── Placement sub-functions ───────────────────────────────────────────

        private static (Line<OriginZeroLine> primarySpan, Line<OriginZeroLine> secondarySpan)
            PlaceDefiniteGridItem(
                InBothAbsAxis<Line<OriginZeroGridPlacement>> placement,
                AbsoluteAxis primaryAxis,
                Direction direction,
                ushort explicitColCount)
        {
            var primarySpan = MaybeMirrorSpan(
                placement.Get(primaryAxis).ResolveDefiniteGridLines(),
                primaryAxis, direction, explicitColCount);
            var secondarySpan = MaybeMirrorSpan(
                placement.Get(primaryAxis.OtherAxis()).ResolveDefiniteGridLines(),
                primaryAxis.OtherAxis(), direction, explicitColCount);
            return (primarySpan, secondarySpan);
        }

        private static (Line<OriginZeroLine> primarySpan, Line<OriginZeroLine> secondarySpan)
            PlaceDefiniteSecondaryAxisItem(
                CellOccupancyMatrix occupancy,
                InBothAbsAxis<Line<OriginZeroGridPlacement>> placement,
                GridAutoFlow autoFlow,
                Direction direction,
                ushort explicitColCount)
        {
            var primaryAxis          = autoFlow.PrimaryAxis();
            var secondaryAxis        = primaryAxis.OtherAxis();
            var primaryAxisIsReversed = AxisIsReversed(direction, primaryAxis);
            var primaryGridStart     = occupancy.TrackCounts(primaryAxis).ImplicitStartLine();
            var primaryGridEnd       = occupancy.TrackCounts(primaryAxis).ImplicitEndLine();

            var secondarySpan = MaybeMirrorSpan(
                placement.Get(secondaryAxis).ResolveDefiniteGridLines(),
                secondaryAxis, direction, explicitColCount);

            OriginZeroLine startingPos;
            if (autoFlow.IsDense())
            {
                startingPos = SearchStartLine(primaryGridStart, primaryGridEnd, primaryAxisIsReversed);
            }
            else
            {
                OriginZeroLine? lookupResult;
                if (primaryAxisIsReversed)
                    lookupResult = occupancy.FirstOfType(primaryAxis, secondarySpan.Start, CellOccupancyState.AutoPlaced);
                else
                    lookupResult = occupancy.LastOfType(primaryAxis, secondarySpan.Start, CellOccupancyState.AutoPlaced);
                startingPos = lookupResult ?? SearchStartLine(primaryGridStart, primaryGridEnd, primaryAxisIsReversed);
            }

            ushort primarySpanCount = placement.Get(primaryAxis).IndefiniteSpan();
            var pos = startingPos;
            while (true)
            {
                var primarySpan = ResolveIndefiniteSpan(pos, primarySpanCount, primaryAxisIsReversed);
                if (occupancy.LineAreaIsUnoccupied(primaryAxis, primarySpan, secondarySpan))
                    return (primarySpan, secondarySpan);
                pos = AdvancePosition(pos, primaryAxisIsReversed);
            }
        }

        private static (Line<OriginZeroLine> primarySpan, Line<OriginZeroLine> secondarySpan)
            PlaceIndefinitelyPositionedItem(
                CellOccupancyMatrix occupancy,
                InBothAbsAxis<Line<OriginZeroGridPlacement>> placement,
                GridAutoFlow autoFlow,
                (OriginZeroLine primary, OriginZeroLine secondary) gridPosition,
                Direction direction,
                ushort explicitColCount)
        {
            var primaryAxis          = autoFlow.PrimaryAxis();
            var secondaryAxis        = primaryAxis.OtherAxis();
            var primaryAxisIsReversed   = AxisIsReversed(direction, primaryAxis);
            var secondaryAxisIsReversed = AxisIsReversed(direction, secondaryAxis);

            var primaryPlacement   = placement.Get(primaryAxis);
            var secondaryPlacement = placement.Get(secondaryAxis);

            var secondarySpanCount = secondaryPlacement.IndefiniteSpan();
            bool hasDefinitePrimary = primaryPlacement.IsDefinite();

            var primaryGridStart   = occupancy.TrackCounts(primaryAxis).ImplicitStartLine();
            var primaryGridEnd     = occupancy.TrackCounts(primaryAxis).ImplicitEndLine();
            var secondaryGridStart = occupancy.TrackCounts(secondaryAxis).ImplicitStartLine();
            var secondaryGridEnd   = occupancy.TrackCounts(secondaryAxis).ImplicitEndLine();
            var primaryStart   = SearchStartLine(primaryGridStart, primaryGridEnd, primaryAxisIsReversed);
            var secondaryStart = SearchStartLine(secondaryGridStart, secondaryGridEnd, secondaryAxisIsReversed);

            var (primaryIdx, secondaryIdx) = gridPosition;

            if (hasDefinitePrimary)
            {
                var primarySpan = MaybeMirrorSpan(
                    primaryPlacement.ResolveDefiniteGridLines(),
                    primaryAxis, direction, explicitColCount);

                // Compute secondary starting position
                if (autoFlow.IsDense())
                {
                    secondaryIdx = secondaryStart;
                }
                else
                {
                    bool shouldAdvance = primaryAxisIsReversed
                        ? primarySpan.Start > primaryIdx
                        : primarySpan.Start < primaryIdx;
                    if (shouldAdvance)
                        secondaryIdx = AdvancePosition(secondaryIdx, secondaryAxisIsReversed);
                }

                // Search secondary axis for a free slot
                while (true)
                {
                    var secondarySpan = ResolveIndefiniteSpan(secondaryIdx, secondarySpanCount, secondaryAxisIsReversed);
                    if (occupancy.LineAreaIsUnoccupied(primaryAxis, primarySpan, secondarySpan))
                        return (primarySpan, secondarySpan);
                    secondaryIdx = AdvancePosition(secondaryIdx, secondaryAxisIsReversed);
                }
            }
            else
            {
                var primarySpanCount = primaryPlacement.IndefiniteSpan();

                // Search primary then secondary axes for a free slot
                while (true)
                {
                    var primarySpan   = ResolveIndefiniteSpan(primaryIdx, primarySpanCount, primaryAxisIsReversed);
                    var secondarySpan = ResolveIndefiniteSpan(secondaryIdx, secondarySpanCount, secondaryAxisIsReversed);

                    // If primary is out of bounds, advance secondary and reset primary
                    bool primaryOutOfBounds = primaryAxisIsReversed
                        ? primarySpan.Start < primaryGridStart
                        : primarySpan.End > primaryGridEnd;
                    if (primaryOutOfBounds)
                    {
                        secondaryIdx = AdvancePosition(secondaryIdx, secondaryAxisIsReversed);
                        primaryIdx   = primaryStart;
                        continue;
                    }

                    if (occupancy.LineAreaIsUnoccupied(primaryAxis, primarySpan, secondarySpan))
                        return (primarySpan, secondarySpan);

                    primaryIdx = AdvancePosition(primaryIdx, primaryAxisIsReversed);
                }
            }
        }

        private static void RecordGridPlacement(
            CellOccupancyMatrix occupancy,
            List<GridItem> items,
            NodeId node,
            int sourceIndex,
            Style style,
            AlignItems parentAlignItems,
            AlignItems parentJustifyItems,
            AbsoluteAxis primaryAxis,
            Line<OriginZeroLine> primarySpan,
            Line<OriginZeroLine> secondarySpan,
            CellOccupancyState state)
        {
            occupancy.MarkAreaAs(primaryAxis, primarySpan, secondarySpan, state);

            var (colSpan, rowSpan) = primaryAxis == AbsoluteAxis.Horizontal
                ? (primarySpan, secondarySpan)
                : (secondarySpan, primarySpan);

            items.Add(new GridItem(
                node, colSpan, rowSpan, style, parentAlignItems, parentJustifyItems, (ushort)sourceIndex));
        }
    }
}
