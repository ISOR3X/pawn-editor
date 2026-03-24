// Port of taffy/src/compute/grid/mod.rs
//
// CSS Grid layout algorithm — main orchestrator.
// Phases: resolve grid → place items → size tracks → align tracks → position items.

using System;
using System.Collections.Generic;

namespace PawnEditor.TaffySharp
{
    internal static class GridCompute
    {
        // ── Public entry point ────────────────────────────────────────────────

        internal static LayoutOutput Compute(TaffyTree tree, NodeId node, LayoutInput input)
        {
            var style = tree.GetStyle(node);

            var direction      = style.direction;
            var alignContent   = style.alignContent   ?? AlignContent.Stretch;
            var justifyContent = style.justifyContent ?? AlignContent.Stretch;
            var alignItems     = style.alignItems;
            var justifyItems   = style.justifyItems;

            // 1. Compute available grid space
            var parentWidth  = input.ParentSize.Width;
            var parentHeight = input.ParentSize.Height;

            var padding = style.padding.ResolveOrZero(parentWidth);
            var border  = style.border.ResolveOrZero(parentWidth);
            var paddingBorder     = RectF.Add(padding, border);
            var paddingBorderSize = RectF.SumAxes(paddingBorder);

            var ar     = style.aspectRatio;
            var boxAdj = style.boxSizing == BoxSizing.ContentBox ? paddingBorderSize : SizeF.ZERO;

            var minSize = SizeF.MaybeApplyAspectRatio(
                style.minSize.MaybeResolve(input.ParentSize).MaybeAdd(boxAdj), ar);
            var maxSize = SizeF.MaybeApplyAspectRatio(
                style.maxSize.MaybeResolve(input.ParentSize).MaybeAdd(boxAdj), ar);

            Size<float?> preferredSize;
            if (input.SizingMode == SizingMode.InherentSize)
                preferredSize = SizeF.MaybeApplyAspectRatio(
                    style.size.MaybeResolve(input.ParentSize).MaybeAdd(boxAdj), ar);
            else
                preferredSize = SizeF.NONE;

            // Scrollbar gutters (transposed because horizontal scroll → vertical space reserved)
            var scrollbarGutter = new Size<float>(
                style.overflow.Y == Overflow.Scroll ? style.scrollbarWidth : 0f,
                style.overflow.X == Overflow.Scroll ? style.scrollbarWidth : 0f);

            var contentBoxInset = paddingBorder;
            contentBoxInset.Bottom += scrollbarGutter.Height;
            if (direction == Direction.Rtl)
                contentBoxInset.Left  += scrollbarGutter.Width;
            else
                contentBoxInset.Right += scrollbarGutter.Width;

            float contentBoxInsetW = contentBoxInset.Left + contentBoxInset.Right;
            float contentBoxInsetH = contentBoxInset.Top  + contentBoxInset.Bottom;

            // constrained_available_space
            var constrainedBase = input.KnownDimensions.Or(preferredSize);
            var constrainedAvailableSpace = new Size<AvailableSpace>(
                constrainedBase.Width.HasValue
                    ? AvailableSpace.Definite(constrainedBase.Width.Value)
                    : input.availableSpace.Width,
                constrainedBase.Height.HasValue
                    ? AvailableSpace.Definite(constrainedBase.Height.Value)
                    : input.availableSpace.Height);

            constrainedAvailableSpace = new Size<AvailableSpace>(
                constrainedAvailableSpace.Width.MaybeClamp(minSize.Width, maxSize.Width)
                    .MaybeMax(paddingBorderSize.Width),
                constrainedAvailableSpace.Height.MaybeClamp(minSize.Height, maxSize.Height)
                    .MaybeMax(paddingBorderSize.Height));

            // available_grid_space (subtract content box insets)
            var availableGridSpace = new Size<AvailableSpace>(
                constrainedAvailableSpace.Width.IsDefinite
                    ? AvailableSpace.Definite(constrainedAvailableSpace.Width.Unwrap() - contentBoxInsetW)
                    : constrainedAvailableSpace.Width,
                constrainedAvailableSpace.Height.IsDefinite
                    ? AvailableSpace.Definite(constrainedAvailableSpace.Height.Unwrap() - contentBoxInsetH)
                    : constrainedAvailableSpace.Height);

            var outerNodeSize = input.KnownDimensions.Or(preferredSize)
                .MaybeClamp(minSize, maxSize).MaybeMax(paddingBorderSize);
            var innerNodeSize = new Size<float?>(
                outerNodeSize.Width.MaybeSub(contentBoxInsetW),
                outerNodeSize.Height.MaybeSub(contentBoxInsetH));

            // Early-exit for ComputeSize when both dimensions are known
            if (input.RunMode == RunMode.ComputeSize
                && outerNodeSize.Width.HasValue && outerNodeSize.Height.HasValue)
            {
                return LayoutOutput.FromOuterSize(
                    new Size<float>(outerNodeSize.Width.Value, outerNodeSize.Height.Value));
            }

            // 2. Resolve explicit grid sizes
            ushort explicitColCount = ExplicitGrid.ComputeExplicitGridSizeInAxis(style, AbsoluteAxis.Horizontal);
            ushort explicitRowCount = ExplicitGrid.ComputeExplicitGridSizeInAxis(style, AbsoluteAxis.Vertical);

            // 3. Estimate implicit track counts
            var (estColCounts, estRowCounts) = ImplicitGrid.ComputeGridSizeEstimate(
                explicitColCount, explicitRowCount, tree, node);

            // 4. Place grid items
            var items = new List<GridItem>(tree.ChildCount(node));
            var cellOccupancy = CellOccupancyMatrix.WithTrackCounts(estColCounts, estRowCounts);
            Placement.PlaceGridItems(
                cellOccupancy, items, tree, node, direction,
                style.gridAutoFlow,
                alignItems  ?? AlignItems.Stretch,
                justifyItems ?? AlignItems.Stretch,
                style);

            var finalColCounts = cellOccupancy.TrackCounts(AbsoluteAxis.Horizontal);
            var finalRowCounts = cellOccupancy.TrackCounts(AbsoluteAxis.Vertical);

            // 5. Initialize tracks
            var columns = new List<GridTrack>();
            var rows    = new List<GridTrack>();

            var colCountsForInit = finalColCounts;
            if (direction == Direction.Rtl && finalColCounts.Explicit <= 1)
            {
                colCountsForInit.NegativeImplicit = finalColCounts.PositiveImplicit;
                colCountsForInit.PositiveImplicit = finalColCounts.NegativeImplicit;
            }

            ExplicitGrid.InitializeGridTracks(columns, colCountsForInit, style, AbsoluteAxis.Horizontal,
                colIdx =>
                {
                    int occupancyIdx = direction == Direction.Rtl
                        ? RtlColumnOccupancyIndex(colIdx, finalColCounts)
                        : colIdx;
                    return cellOccupancy.ColumnIsOccupied(occupancyIdx);
                });
            ExplicitGrid.InitializeGridTracks(rows, finalRowCounts, style, AbsoluteAxis.Vertical,
                rowIdx => cellOccupancy.RowIsOccupied(rowIdx));

            if (direction == Direction.Rtl)
                ReverseNonGutterTracks(columns, finalColCounts);

            // 6. Track sizing

            TrackSizing.ResolveItemTrackIndexes(items, finalColCounts, finalRowCounts);
            TrackSizing.DetermineIfItemCrossesFlexibleOrIntrinsicTracks(items, columns, rows);

            bool hasBaselineAlignedItem = false;
            for (int i = 0; i < items.Count; i++)
                if (items[i].AlignSelf == AlignItems.Baseline) { hasBaselineAlignedItem = true; break; }

            // Inline axis (columns)
            Func<GridTrack, float?, float?> maxDefiniteEstimate =
                (t, parentSize) => t.MaxTrackSizingFunction.DefiniteValue(parentSize);
            Func<GridTrack, float?, float?> baseSizeEstimate =
                (t, _) => (float?)t.BaseSize;

            TrackSizing.Compute(tree,
                AbstractAxis.Inline,
                minSize.Width, maxSize.Width,
                justifyContent, alignContent,
                availableGridSpace, innerNodeSize,
                columns, rows, items,
                maxDefiniteEstimate,
                hasBaselineAlignedItem);

            float initialColumnSum = 0f;
            for (int i = 0; i < columns.Count; i++) initialColumnSum += columns[i].BaseSize;
            if (!innerNodeSize.Width.HasValue)
                innerNodeSize = innerNodeSize.WithAxis(AbstractAxis.Inline, initialColumnSum);

            // Clear available-space cache between the two axis passes
            for (int i = 0; i < items.Count; i++) items[i].AvailableSpaceCache = null;

            // Block axis (rows)
            TrackSizing.Compute(tree,
                AbstractAxis.Block,
                minSize.Height, maxSize.Height,
                alignContent, justifyContent,
                availableGridSpace, innerNodeSize,
                rows, columns, items,
                baseSizeEstimate,
                false);

            float initialRowSum = 0f;
            for (int i = 0; i < rows.Count; i++) initialRowSum += rows[i].BaseSize;
            if (!innerNodeSize.Height.HasValue)
                innerNodeSize = innerNodeSize.WithAxis(AbstractAxis.Block, initialRowSum);

            // 6b. Compute container border-box size
            var resolvedStyleSize = input.KnownDimensions.Or(preferredSize);
            float containerWidth = (resolvedStyleSize.Width ?? (initialColumnSum + contentBoxInsetW))
                .MaybeClamp(minSize.Width, maxSize.Width);
            containerWidth = MathF.Max(containerWidth, paddingBorderSize.Width);

            float containerHeight = (resolvedStyleSize.Height ?? (initialRowSum + contentBoxInsetH))
                .MaybeClamp(minSize.Height, maxSize.Height);
            containerHeight = MathF.Max(containerHeight, paddingBorderSize.Height);

            var containerBorderBox = new Size<float>(containerWidth, containerHeight);
            var containerContentBox = new Size<float>(
                MathF.Max(containerWidth  - contentBoxInsetW, 0f),
                MathF.Max(containerHeight - contentBoxInsetH, 0f));

            if (input.RunMode == RunMode.ComputeSize)
                return LayoutOutput.FromOuterSize(containerBorderBox);

            // 7. Resolve percentage track base sizes when container was initially indefinite
            if (!availableGridSpace.Width.IsDefinite)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    float? mn = col.MinTrackSizingFunction.ResolvedPercentageSize(containerContentBox.Width);
                    float? mx = col.MaxTrackSizingFunction.ResolvedPercentageSize(containerContentBox.Width);
                    col.BaseSize = col.BaseSize.MaybeClamp(mn, mx);
                }
            }
            if (!availableGridSpace.Height.IsDefinite)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    float? mn = row.MinTrackSizingFunction.ResolvedPercentageSize(containerContentBox.Height);
                    float? mx = row.MaxTrackSizingFunction.ResolvedPercentageSize(containerContentBox.Height);
                    row.BaseSize = row.BaseSize.MaybeClamp(mn, mx);
                }
            }

            // Re-run column sizing if percentage columns existed or min-content contributions changed
            bool rerunColumnSizing;
            bool hasPercentageColumn = false;
            for (int i = 0; i < columns.Count; i++)
                if (columns[i].UsesPercentage()) { hasPercentageColumn = true; break; }

            bool parentWidthIndefinite = !input.availableSpace.Width.IsDefinite;
            rerunColumnSizing = parentWidthIndefinite && hasPercentageColumn;

            if (!rerunColumnSizing)
            {
                bool changed = false;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (!item.CrossesIntrinsicColumn) continue;
                    var avail = item.AvailableSpace(AbstractAxis.Inline, rows, innerNodeSize.Height, baseSizeEstimate);
                    float newMin = item.MinContentContribution(AbstractAxis.Inline, tree, avail, innerNodeSize);
                    bool hasChanged = newMin != item.MinContentContributionCache.Width;

                    item.AvailableSpaceCache = avail;
                    item.MinContentContributionCache =
                        item.MinContentContributionCache.WithAxis(AbstractAxis.Inline, newMin);
                    item.MaxContentContributionCache =
                        item.MaxContentContributionCache.WithAxis(AbstractAxis.Inline, null);
                    item.MinimumContributionCache =
                        item.MinimumContributionCache.WithAxis(AbstractAxis.Inline, null);
                    changed |= hasChanged;
                }
                rerunColumnSizing = changed;
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    item.AvailableSpaceCache = null;
                    item.MinContentContributionCache =
                        item.MinContentContributionCache.WithAxis(AbstractAxis.Inline, null);
                    item.MaxContentContributionCache =
                        item.MaxContentContributionCache.WithAxis(AbstractAxis.Inline, null);
                    item.MinimumContributionCache =
                        item.MinimumContributionCache.WithAxis(AbstractAxis.Inline, null);
                }
            }

            if (rerunColumnSizing)
            {
                TrackSizing.Compute(tree,
                    AbstractAxis.Inline,
                    minSize.Width, maxSize.Width,
                    justifyContent, alignContent,
                    availableGridSpace, innerNodeSize,
                    columns, rows, items,
                    baseSizeEstimate,
                    hasBaselineAlignedItem);

                // Check whether row sizing also needs re-running
                bool rerunRowSizing;
                bool hasPercentageRow = false;
                for (int i = 0; i < rows.Count; i++)
                    if (rows[i].UsesPercentage()) { hasPercentageRow = true; break; }

                bool parentHeightIndefinite = !input.availableSpace.Height.IsDefinite;
                rerunRowSizing = parentHeightIndefinite && hasPercentageRow;

                if (!rerunRowSizing)
                {
                    bool changed = false;
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (!item.CrossesIntrinsicColumn) continue;
                        var avail = item.AvailableSpace(AbstractAxis.Block, columns, innerNodeSize.Width, baseSizeEstimate);
                        float newMin = item.MinContentContribution(AbstractAxis.Block, tree, avail, innerNodeSize);
                        bool hasChanged = newMin != item.MinContentContributionCache.Height;

                        item.AvailableSpaceCache = avail;
                        item.MinContentContributionCache =
                            item.MinContentContributionCache.WithAxis(AbstractAxis.Block, newMin);
                        item.MaxContentContributionCache =
                            item.MaxContentContributionCache.WithAxis(AbstractAxis.Block, null);
                        item.MinimumContributionCache =
                            item.MinimumContributionCache.WithAxis(AbstractAxis.Block, null);
                        changed |= hasChanged;
                    }
                    rerunRowSizing = changed;
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        item.AvailableSpaceCache = null;
                        item.MinContentContributionCache =
                            item.MinContentContributionCache.WithAxis(AbstractAxis.Block, null);
                        item.MaxContentContributionCache =
                            item.MaxContentContributionCache.WithAxis(AbstractAxis.Block, null);
                        item.MinimumContributionCache =
                            item.MinimumContributionCache.WithAxis(AbstractAxis.Block, null);
                    }
                }

                if (rerunRowSizing)
                {
                    TrackSizing.Compute(tree,
                        AbstractAxis.Block,
                        minSize.Height, maxSize.Height,
                        alignContent, justifyContent,
                        availableGridSpace, innerNodeSize,
                        rows, columns, items,
                        baseSizeEstimate,
                        false);
                }
            }

            // 8. Track alignment (assign Offset to each track)

            float inlineSizeWithoutScrollbar = MathF.Max(containerWidth - paddingBorderSize.Width, 0f);
            float inlineScrollbarForAlignment = MathF.Min(scrollbarGutter.Width, inlineSizeWithoutScrollbar);

            GridAlignment.AlignTracks(
                containerContentBox.Width,
                new Line<float>(
                    padding.Left  + (direction == Direction.Rtl ? inlineScrollbarForAlignment : 0f),
                    padding.Right + (direction == Direction.Rtl ? 0f : inlineScrollbarForAlignment)),
                new Line<float>(border.Left, border.Right),
                columns,
                justifyContent,
                direction == Direction.Rtl);

            GridAlignment.AlignTracks(
                containerContentBox.Height,
                new Line<float>(padding.Top, padding.Bottom),
                new Line<float>(border.Top, border.Bottom),
                rows,
                alignContent,
                false);

            // 9. Size, align, and position grid items

            // Sort by source order for in-flow item positioning
            items.Sort((a, b) => a.SourceOrder.CompareTo(b.SourceOrder));

            var containerAlignStyles = new InBothAbsAxis<AlignItems?>(justifyItems, alignItems);

            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                var gridArea = new Rect<float>(
                    top:    rows[item.RowIndexes.Start    + 1].Offset,
                    bottom: rows[item.RowIndexes.End         ].Offset,
                    left:   columns[item.ColumnIndexes.Start + 1].Offset,
                    right:  columns[item.ColumnIndexes.End      ].Offset);

                var (y, h) = GridAlignment.AlignAndPositionItem(
                    tree, item.Node, (uint)idx, gridArea,
                    containerAlignStyles, item.BaselineShim, direction);
                item.YPosition = y;
                item.Height    = h;
            }

            // Position hidden and absolutely positioned children
            uint order = (uint)items.Count;
            int childCount = tree.ChildCount(node);
            for (int ci = 0; ci < childCount; ci++)
            {
                var child      = tree.ChildAt(node, ci);
                var childStyle = tree.GetStyle(child);

                if (childStyle.boxGenerationMode == BoxGenerationMode.None)
                {
                    // Process the hidden subtree first (sets all descendants to zero layout),
                    // then override this node's order with the correct value.
                    tree.PerformLayout(child, LayoutInput.Hidden);
                    tree.SetNodeLayout(child, TaffySharp.Layout.WithOrder(order));
                    order++;
                    continue;
                }

                if (childStyle.position == Position.Absolute)
                {
                    var colPl = childStyle.gridColumn.IntoOriginZeroIgnoringNamed(finalColCounts.Explicit);
                    var rowPl = childStyle.gridRow.IntoOriginZeroIgnoringNamed(finalRowCounts.Explicit);

                    // RTL: flip column indexes
                    if (direction == Direction.Rtl)
                        colPl = new Line<OriginZeroGridPlacement>(colPl.End, colPl.Start);

                    int? colStartIdx = colPl.Start.IsLine
                        ? colPl.Start.AsLine().TryIntoTrackVecIndex(finalColCounts) : null;
                    int? colEndIdx   = colPl.End.IsLine
                        ? colPl.End.AsLine().TryIntoTrackVecIndex(finalColCounts)   : null;
                    int? rowStartIdx = rowPl.Start.IsLine
                        ? rowPl.Start.AsLine().TryIntoTrackVecIndex(finalRowCounts) : null;
                    int? rowEndIdx   = rowPl.End.IsLine
                        ? rowPl.End.AsLine().TryIntoTrackVecIndex(finalRowCounts)   : null;

                    float absLeft  = colStartIdx.HasValue
                        ? columns[colStartIdx.Value].Offset
                        : (direction == Direction.Rtl
                            ? border.Left + scrollbarGutter.Width
                            : border.Left);
                    float absRight = colEndIdx.HasValue
                        ? columns[colEndIdx.Value].Offset
                        : (direction == Direction.Rtl
                            ? containerWidth - border.Right
                            : containerWidth - border.Right - scrollbarGutter.Width);
                    float absTop    = rowStartIdx.HasValue
                        ? rows[rowStartIdx.Value].Offset
                        : border.Top;
                    float absBottom = rowEndIdx.HasValue
                        ? rows[rowEndIdx.Value].Offset
                        : containerHeight - border.Bottom - scrollbarGutter.Height;

                    var gridArea = new Rect<float>(absLeft, absRight, absTop, absBottom);

                    GridAlignment.AlignAndPositionItem(
                        tree, child, order, gridArea,
                        containerAlignStyles, 0f, direction);
                    order++;
                }
            }

            // Compute grid container baseline
            if (items.Count == 0)
                return LayoutOutput.FromOuterSize(containerBorderBox);

            // Sort items by row start for baseline computation
            items.Sort((a, b) => a.RowIndexes.Start.CompareTo(b.RowIndexes.Start));
            ushort firstRow = items[0].RowIndexes.Start;

            // Find the last item in the first row
            int firstRowEnd = 0;
            while (firstRowEnd < items.Count && items[firstRowEnd].RowIndexes.Start == firstRow)
                firstRowEnd++;

            // Pick baseline item (prefer Baseline-aligned, else first)
            GridItem? baselineItem = null;
            for (int i = 0; i < firstRowEnd; i++)
                if (items[i].AlignSelf == AlignItems.Baseline) { baselineItem = items[i]; break; }
            baselineItem ??= items[0];

            float gridContainerBaseline = baselineItem.YPosition
                + (baselineItem.Baseline ?? baselineItem.Height);

            return LayoutOutput.FromSizesAndBaselines(
                containerBorderBox,
                SizeF.ZERO,
                new Point<float?>(null, gridContainerBaseline));
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Reverses the non-gutter column tracks in-place to support RTL layout.
        /// </summary>
        private static void ReverseNonGutterTracks(List<GridTrack> tracks, TrackCounts trackCounts)
        {
            if (trackCounts.Explicit <= 1)
            {
                const int MinLen = 5;
                if (tracks.Count < MinLen) return;
                int left = 1, right = tracks.Count - 2;
                while (left < right)
                {
                    var tmp = tracks[left]; tracks[left] = tracks[right]; tracks[right] = tmp;
                    left += 2;
                    right = right >= 2 ? right - 2 : 0;
                }
                return;
            }

            int explicitCount = trackCounts.Explicit;
            if (explicitCount < 2) return;

            int lo = trackCounts.NegativeImplicit;
            int hi = lo + explicitCount - 1;
            while (lo < hi)
            {
                int li = (2 * lo) + 1, ri = (2 * hi) + 1;
                var tmp = tracks[li]; tracks[li] = tracks[ri]; tracks[ri] = tmp;
                lo++;
                hi--;
            }
        }

        /// <summary>
        /// Maps a column index to its occupancy-matrix index for RTL auto-fit collapsing.
        /// </summary>
        private static int RtlColumnOccupancyIndex(int columnIndex, TrackCounts trackCounts)
        {
            if (trackCounts.Explicit <= 1)
                return trackCounts.Len() - columnIndex - 1;

            int explicitStart = trackCounts.NegativeImplicit;
            int explicitEnd   = explicitStart + trackCounts.Explicit;
            if (columnIndex >= explicitStart && columnIndex < explicitEnd)
                return explicitStart + (explicitEnd - columnIndex - 1);
            return columnIndex;
        }
    }
}
