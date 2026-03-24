// Port of taffy/src/compute/grid/track_sizing.rs
//
// Track Sizing Algorithm (CSS Grid §11).
// Determines the final base size of each grid track.

using System;
using System.Collections.Generic;

namespace PawnEditor.TaffySharp
{
    // ── IntrinsicContributionType ─────────────────────────────────────────────

    internal enum IntrinsicContributionType : byte
    {
        Minimum,
        Maximum,
    }

    // ── ItemBatcher ───────────────────────────────────────────────────────────
    // Batches items by span and flex-ness for the intrinsic sizing loops.

    internal sealed class ItemBatcher
    {
        private readonly AbstractAxis _axis;
        private int _indexOffset;
        private bool _currentIsFlex;

        public ItemBatcher(AbstractAxis axis)
        {
            _axis = axis;
            _indexOffset = 0;
            _currentIsFlex = false;
        }

        /// <summary>
        /// Advances to the next batch. Returns false when exhausted.
        /// Sets <paramref name="batchStart"/>, <paramref name="batchEnd"/> (exclusive), <paramref name="isFlex"/>.
        /// </summary>
        public bool Next(List<GridItem> items, out int batchStart, out int batchEnd, out bool isFlex)
        {
            batchStart = batchEnd = 0;
            isFlex = false;

            if (_currentIsFlex || _indexOffset >= items.Count)
                return false;

            ushort currentSpan = items[_indexOffset].Span(_axis);
            _currentIsFlex = items[_indexOffset].CrossesFlexibleTrack(_axis);

            int nextOffset = items.Count;
            if (!_currentIsFlex)
            {
                for (int i = _indexOffset; i < items.Count; i++)
                {
                    if (items[i].CrossesFlexibleTrack(_axis) || items[i].Span(_axis) > currentSpan)
                    {
                        nextOffset = i;
                        break;
                    }
                }
            }

            batchStart = _indexOffset;
            batchEnd = nextOffset;
            isFlex = _currentIsFlex;
            _indexOffset = nextOffset;
            return true;
        }
    }

    // ── TrackSizing ───────────────────────────────────────────────────────────

    internal static class TrackSizing
    {
        // ── Pre-sizing helpers (called from GridCompute before Compute) ────────

        /// <summary>Converts OriginZero placements to interleaved track-vector indexes.</summary>
        internal static void ResolveItemTrackIndexes(
            List<GridItem> items, TrackCounts columnCounts, TrackCounts rowCounts)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.ColumnIndexes = new Line<ushort>(
                    (ushort)item.Column.Start.IntoTrackVecIndex(columnCounts),
                    (ushort)item.Column.End.IntoTrackVecIndex(columnCounts));
                item.RowIndexes = new Line<ushort>(
                    (ushort)item.Row.Start.IntoTrackVecIndex(rowCounts),
                    (ushort)item.Row.End.IntoTrackVecIndex(rowCounts));
            }
        }

        /// <summary>Flags each item with whether it crosses flexible or intrinsic tracks in each axis.</summary>
        internal static void DetermineIfItemCrossesFlexibleOrIntrinsicTracks(
            List<GridItem> items, List<GridTrack> columns, List<GridTrack> rows)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var (cS, cE) = item.TrackRangeExcludingLines(AbstractAxis.Inline);
                var (rS, rE) = item.TrackRangeExcludingLines(AbstractAxis.Block);

                item.CrossesFlexibleColumn = item.CrossesIntrinsicColumn = false;
                for (int j = cS; j < cE; j++)
                {
                    if (columns[j].IsFlexible()) item.CrossesFlexibleColumn = true;
                    if (columns[j].HasIntrinsicSizingFunction()) item.CrossesIntrinsicColumn = true;
                }

                item.CrossesFlexibleRow = item.CrossesIntrinsicRow = false;
                for (int j = rS; j < rE; j++)
                {
                    if (rows[j].IsFlexible()) item.CrossesFlexibleRow = true;
                    if (rows[j].HasIntrinsicSizingFunction()) item.CrossesIntrinsicRow = true;
                }
            }
        }

        // ── Gutter alignment adjustment ───────────────────────────────────────

        internal static float ComputeAlignmentGutterAdjustment(
            AlignContent alignment,
            float? axisInnerNodeSize,
            Func<GridTrack, float?, float?> getTrackSizeEstimate,
            List<GridTrack> tracks)
        {
            if (tracks.Count <= 1) return 0f;

            int outerGutterWeight = alignment switch
            {
                AlignContent.Stretch or AlignContent.SpaceBetween => 0,
                _ => 1,
            };

            int innerGutterWeight = alignment switch
            {
                AlignContent.SpaceBetween => 1,
                AlignContent.SpaceAround  => 2,
                AlignContent.SpaceEvenly  => 1,
                _ => 0,
            };

            if (innerGutterWeight == 0 || !axisInnerNodeSize.HasValue) return 0f;

            float nodeSize = axisInnerNodeSize.Value;
            float trackSizeSum = 0f;
            for (int i = 0; i < tracks.Count; i++)
            {
                var est = getTrackSizeEstimate(tracks[i], nodeSize);
                if (!est.HasValue) return 0f;
                trackSizeSum += est.Value;
            }

            float freeSpace = MathF.Max(0f, nodeSize - trackSizeSum);
            int weightedCount = ((tracks.Count - 3) / 2) * innerGutterWeight + 2 * outerGutterWeight;
            if (weightedCount == 0) return 0f;
            return (freeSpace / weightedCount) * innerGutterWeight;
        }

        // ── Main entry point ──────────────────────────────────────────────────

        /// <summary>
        /// CSS Grid Track Sizing Algorithm (§11).
        /// Mutates <paramref name="axisTracks"/> base sizes in-place.
        /// </summary>
        internal static void Compute(
            TaffyTree tree,
            AbstractAxis axis,
            float? axisMinSize,
            float? axisMaxSize,
            AlignContent axisAlignment,
            AlignContent otherAxisAlignment,
            Size<AvailableSpace> availableGridSpace,
            Size<float?> innerNodeSize,
            List<GridTrack> axisTracks,
            List<GridTrack> otherAxisTracks,
            List<GridItem> items,
            Func<GridTrack, float?, float?> getTrackSizeEstimate,
            bool hasBaselineAlignedItem)
        {
            // 11.4 Initialize track sizes
            float? percentageBasis = innerNodeSize.Get(axis) ?? axisMinSize;
            InitializeTrackSizes(axisTracks, percentageBasis);

            // 11.5.1 Shim item baselines
            if (hasBaselineAlignedItem)
                ResolveItemBaselines(tree, axis, items, innerNodeSize);

            // Early exit: if all tracks already have a fixed size
            bool allFixed = true;
            for (int i = 0; i < axisTracks.Count; i++)
                if (axisTracks[i].BaseSize != axisTracks[i].GrowthLimit) { allFixed = false; break; }
            if (allFixed) return;

            // Apply content-alignment gutter adjustment to the other axis
            float gutterAdj = ComputeAlignmentGutterAdjustment(
                otherAxisAlignment,
                innerNodeSize.Get(axis.OtherAxis()),
                getTrackSizeEstimate,
                otherAxisTracks);
            if (otherAxisTracks.Count > 3)
            {
                // inner gutters are at even indices [2, 4, 6 … len-3]
                for (int i = 2; i < otherAxisTracks.Count - 1; i += 2)
                    otherAxisTracks[i].ContentAlignmentAdjustment = gutterAdj;
            }

            // 11.5 Resolve intrinsic track sizes
            float? axisInnerNodeSize = innerNodeSize.Get(axis);
            float flexFactorSum = 0f;
            for (int i = 0; i < axisTracks.Count; i++)
                flexFactorSum += axisTracks[i].FlexFactor();

            ResolveIntrinsicTrackSizes(
                tree, axis, axisTracks, otherAxisTracks, items,
                availableGridSpace.Get(axis), innerNodeSize,
                getTrackSizeEstimate, axisInnerNodeSize, flexFactorSum);

            // 11.6 Maximize tracks
            MaximiseTracks(axisTracks, innerNodeSize.Get(axis), availableGridSpace.Get(axis));

            // Determine available space for flex/stretch expansion
            AvailableSpace axisAvailForExpansion;
            float? axisInnerSize = innerNodeSize.Get(axis);
            if (axisInnerSize.HasValue)
                axisAvailForExpansion = AvailableSpace.Definite(axisInnerSize.Value);
            else
                axisAvailForExpansion = availableGridSpace.Get(axis).IsMinContent
                    ? AvailableSpace.MinContent
                    : AvailableSpace.MaxContent;

            // 11.7 Expand flexible tracks
            ExpandFlexibleTracks(
                tree, axis, axisTracks, items,
                axisMinSize, axisMaxSize, axisAvailForExpansion, innerNodeSize);

            // 11.8 Stretch auto tracks
            if (axisAlignment == AlignContent.Stretch)
                StretchAutoTracks(axisTracks, axisMinSize, axisAvailForExpansion);
        }

        // ── 11.4 Initialize track sizes ───────────────────────────────────────

        static void InitializeTrackSizes(List<GridTrack> axisTracks, float? percentageBasis)
        {
            for (int i = 0; i < axisTracks.Count; i++)
            {
                var t = axisTracks[i];
                t.BaseSize   = t.MinTrackSizingFunction.DefiniteValue(percentageBasis) ?? 0f;
                t.GrowthLimit = t.MaxTrackSizingFunction.DefiniteValue(percentageBasis) ?? float.PositiveInfinity;
                if (t.GrowthLimit < t.BaseSize)
                    t.GrowthLimit = t.BaseSize;
            }
        }

        // ── 11.5.1 Resolve item baselines ─────────────────────────────────────

        static void ResolveItemBaselines(
            TaffyTree tree, AbstractAxis axis, List<GridItem> items, Size<float?> innerNodeSize)
        {
            var otherAxis = axis.OtherAxis();
            items.Sort((a, b) => a.Placement(otherAxis).Start.CompareTo(b.Placement(otherAxis).Start));

            int offset = 0;
            while (offset < items.Count)
            {
                var currentRow = items[offset].Placement(otherAxis).Start;

                // Find end of current row group
                int rowEnd = items.Count;
                for (int i = offset; i < items.Count; i++)
                {
                    if (!items[i].Placement(otherAxis).Start.Equals(currentRow))
                    {
                        rowEnd = i;
                        break;
                    }
                }

                // Only process rows with ≥2 baseline-aligned items
                int baselineCount = 0;
                for (int i = offset; i < rowEnd; i++)
                    if (items[i].AlignSelf == AlignItems.Baseline) baselineCount++;

                if (baselineCount > 1)
                {
                    // Compute baseline for every item in the row
                    for (int i = offset; i < rowEnd; i++)
                    {
                        var item = items[i];
                        var input = new LayoutInput
                        {
                            KnownDimensions = SizeF.NONE,
                            ParentSize       = innerNodeSize,
                            availableSpace   = new Size<AvailableSpace>(AvailableSpace.MinContent, AvailableSpace.MinContent),
                            SizingMode       = SizingMode.InherentSize,
                            Axis             = RequestedAxis.Both,
                            RunMode          = RunMode.PerformLayout,
                            VerticalMarginsAreCollapsible = new Line<bool>(false, false),
                        };
                        var output = tree.PerformLayout(item.Node, input);
                        float marginTop = item.Margin.Top.IsAuto()
                            ? 0f
                            : item.Margin.Top.ResolveOrZero(innerNodeSize.Width);
                        item.Baseline = (output.FirstBaselines.Y ?? output.Size.Height) + marginTop;
                    }

                    // Find row max baseline
                    float maxBaseline = 0f;
                    for (int i = offset; i < rowEnd; i++)
                    {
                        var b = items[i].Baseline;
                        if (b.HasValue && b.Value > maxBaseline) maxBaseline = b.Value;
                    }

                    // Set shims
                    for (int i = offset; i < rowEnd; i++)
                        items[i].BaselineShim = maxBaseline - (items[i].Baseline ?? 0f);
                }

                offset = rowEnd;
            }
        }

        // ── 11.5 Resolve intrinsic track sizes ────────────────────────────────

        static void ResolveIntrinsicTrackSizes(
            TaffyTree tree,
            AbstractAxis axis,
            List<GridTrack> axisTracks,
            List<GridTrack> otherAxisTracks,
            List<GridItem> items,
            AvailableSpace axisAvailGridSpace,
            Size<float?> innerNodeSize,
            Func<GridTrack, float?, float?> getTrackSizeEstimate,
            float? axisInnerNodeSize,
            float flexFactorSum)
        {
            items.Sort(CmpByCrossFlexThenSpanThenStart(axis));

            // ── Local size-contribution helpers ──────────────────────────────
            float? otherAxisSize = innerNodeSize.Get(axis.OtherAxis());

            Size<float?> AvailSpace(GridItem item) =>
                item.AvailableSpaceCached(axis, otherAxisTracks, otherAxisSize, getTrackSizeEstimate);

            float MinContent(GridItem item)
            {
                var ms = item.MarginsAxisSumsWithBaselineShims(innerNodeSize.Width);
                return item.MinContentContributionCached(axis, tree, AvailSpace(item), innerNodeSize)
                     + ms.Get(axis);
            }

            float MaxContent(GridItem item)
            {
                var ms = item.MarginsAxisSumsWithBaselineShims(innerNodeSize.Width);
                return item.MaxContentContributionCached(axis, tree, AvailSpace(item), innerNodeSize)
                     + ms.Get(axis);
            }

            float MinimumContrib(GridItem item)
            {
                var ms = item.MarginsAxisSumsWithBaselineShims(innerNodeSize.Width);
                return item.MinimumContributionCached(tree, axis, axisTracks, AvailSpace(item), innerNodeSize)
                     + ms.Get(axis);
            }
            // ─────────────────────────────────────────────────────────────────

            bool useFlexFactor = flexFactorSum != 0f;
            var batcher = new ItemBatcher(axis);

            while (batcher.Next(items, out int bStart, out int bEnd, out bool isFlex))
            {
                ushort batchSpan = items[bStart].Span(axis);

                // ── Span-1 non-flex: fast path ────────────────────────────────
                if (!isFlex && batchSpan == 1)
                {
                    for (int i = bStart; i < bEnd; i++)
                    {
                        var item = items[i];
                        int trackIdx = item.PlacementIndexes(axis).Start + 1;
                        var track = axisTracks[trackIdx];

                        // Base size
                        float newBase;
                        switch (track.MinTrackSizingFunction._cl.Tag)
                        {
                            case CompactLength.MIN_CONTENT_TAG:
                                newBase = MathF.Max(track.BaseSize, MinContent(item));
                                break;
                            case CompactLength.PERCENT_TAG:
                                // If container size is indefinite, treat as min-content
                                newBase = axisInnerNodeSize.HasValue
                                    ? track.BaseSize
                                    : MathF.Max(track.BaseSize, MinContent(item));
                                break;
                            case CompactLength.MAX_CONTENT_TAG:
                                newBase = MathF.Max(track.BaseSize, MaxContent(item));
                                break;
                            case CompactLength.AUTO_TAG:
                            {
                                float space;
                                if ((axisAvailGridSpace.IsMinContent || axisAvailGridSpace.IsMaxContent)
                                    && !item.Overflow.Get(axis).IsScrollContainer())
                                {
                                    float minSz      = MinimumContrib(item);
                                    float minContent = MinContent(item);
                                    float? limit     = track.DefiniteLimit(axisInnerNodeSize);
                                    space = minContent.MaybeMin(limit);
                                    if (space < minSz) space = minSz;
                                }
                                else
                                {
                                    space = MinimumContrib(item);
                                }
                                newBase = MathF.Max(track.BaseSize, space);
                                break;
                            }
                            default: // LENGTH_TAG or other fixed
                                newBase = track.BaseSize;
                                break;
                        }
                        track.BaseSize = newBase;

                        // Growth limit
                        if (track.MaxTrackSizingFunction.IsFitContent())
                        {
                            if (!item.Overflow.Get(axis).IsScrollContainer())
                                track.GrowthLimitPlannedIncrease = MathF.Max(
                                    track.GrowthLimitPlannedIncrease, MinContent(item));
                            float fcLimit = track.FitContentLimit(axisInnerNodeSize);
                            float mcCapped = MathF.Min(MaxContent(item), fcLimit);
                            track.GrowthLimitPlannedIncrease = MathF.Max(
                                track.GrowthLimitPlannedIncrease, mcCapped);
                        }
                        else if (track.MaxTrackSizingFunction.IsMaxContentAlike()
                            || (track.MaxTrackSizingFunction.UsesPercentage() && !axisInnerNodeSize.HasValue))
                        {
                            track.GrowthLimitPlannedIncrease = MathF.Max(
                                track.GrowthLimitPlannedIncrease, MaxContent(item));
                        }
                        else if (track.MaxTrackSizingFunction.IsIntrinsic())
                        {
                            track.GrowthLimitPlannedIncrease = MathF.Max(
                                track.GrowthLimitPlannedIncrease, MinContent(item));
                        }
                    }

                    // Span-1 growth-limit flush (distinct from multi-span flush)
                    for (int i = 0; i < axisTracks.Count; i++)
                    {
                        var t = axisTracks[i];
                        if (t.GrowthLimitPlannedIncrease > 0f)
                        {
                            t.GrowthLimit = t.GrowthLimit == float.PositiveInfinity
                                ? t.GrowthLimitPlannedIncrease
                                : MathF.Max(t.GrowthLimit, t.GrowthLimitPlannedIncrease);
                        }
                        t.InfinitelyGrowable = false;
                        t.GrowthLimitPlannedIncrease = 0f;
                        if (t.GrowthLimit < t.BaseSize) t.GrowthLimit = t.BaseSize;
                    }

                    continue;
                }

                // ── Multi-span (and flex) batches ─────────────────────────────
                bool useFlexFactorForDist = isFlex && useFlexFactor;

                // Step 1: For intrinsic minimums
                for (int i = bStart; i < bEnd; i++)
                {
                    var item = items[i];
                    if (!item.CrossesIntrinsicTrack(axis)) continue;

                    float space;
                    if ((axisAvailGridSpace.IsMinContent || axisAvailGridSpace.IsMaxContent)
                        && !item.Overflow.Get(axis).IsScrollContainer())
                    {
                        float minSz      = MinimumContrib(item);
                        float minContent = MinContent(item);
                        float? limit     = item.SpannedTrackLimit(axis, axisTracks, axisInnerNodeSize);
                        space = minContent.MaybeMin(limit);
                        if (space < minSz) space = minSz;
                    }
                    else
                    {
                        space = MinimumContrib(item);
                    }

                    if (space > 0f)
                    {
                        var (ts, te) = item.TrackRangeExcludingLines(axis);
                        if (item.Overflow.Get(axis).IsScrollContainer())
                            DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                                t => !t.MinTrackSizingFunction.DefiniteValue(axisInnerNodeSize).HasValue,
                                t => t.FitContentLimitedGrowthLimit(axisInnerNodeSize),
                                IntrinsicContributionType.Minimum);
                        else
                            DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                                t => !t.MinTrackSizingFunction.DefiniteValue(axisInnerNodeSize).HasValue,
                                t => t.GrowthLimit,
                                IntrinsicContributionType.Minimum);
                    }
                }
                FlushPlannedBaseSizeIncreases(axisTracks);

                // Step 2: For content-based minimums
                for (int i = bStart; i < bEnd; i++)
                {
                    var item = items[i];
                    float space = MinContent(item);
                    if (space > 0f)
                    {
                        var (ts, te) = item.TrackRangeExcludingLines(axis);
                        if (item.Overflow.Get(axis).IsScrollContainer())
                            DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                                t => t.MinTrackSizingFunction.IsMinOrMaxContent(),
                                t => t.FitContentLimitedGrowthLimit(axisInnerNodeSize),
                                IntrinsicContributionType.Minimum);
                        else
                            DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                                t => t.MinTrackSizingFunction.IsMinOrMaxContent(),
                                t => t.GrowthLimit,
                                IntrinsicContributionType.Minimum);
                    }
                }
                FlushPlannedBaseSizeIncreases(axisTracks);

                // Step 3: For max-content minimums (only under MaxContent constraint)
                if (axisAvailGridSpace.IsMaxContent)
                {
                    for (int i = bStart; i < bEnd; i++)
                    {
                        var item = items[i];
                        float? limit = item.SpannedTrackLimit(axis, axisTracks, axisInnerNodeSize);
                        float space  = MaxContent(item).MaybeMin(limit);
                        if (space > 0f)
                        {
                            var (ts, te) = item.TrackRangeExcludingLines(axis);
                            // Prefer MaxContent min tracks; fall back to Auto min tracks
                            bool anyMaxContentMin = false;
                            for (int j = ts; j < te; j++)
                                if (axisTracks[j].MinTrackSizingFunction._cl.IsMaxContent())
                                { anyMaxContentMin = true; break; }

                            if (anyMaxContentMin)
                                DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                                    t => t.MinTrackSizingFunction._cl.IsMaxContent(),
                                    _ => float.PositiveInfinity,
                                    IntrinsicContributionType.Maximum);
                            else
                                DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                                    t => t.MinTrackSizingFunction._cl.IsAuto()
                                        && !t.MaxTrackSizingFunction._cl.IsMinContent(),
                                    t => t.FitContentLimitedGrowthLimit(axisInnerNodeSize),
                                    IntrinsicContributionType.Maximum);
                        }
                    }
                    FlushPlannedBaseSizeIncreases(axisTracks);
                }

                // Step 3 (cont.): MaxContent min tracks always get max-content contributions
                for (int i = bStart; i < bEnd; i++)
                {
                    var item = items[i];
                    float space = MaxContent(item);
                    if (space > 0f)
                    {
                        var (ts, te) = item.TrackRangeExcludingLines(axis);
                        DistributeItemSpaceToBaseSize(isFlex, useFlexFactorForDist, space, axisTracks, ts, te,
                            t => t.MinTrackSizingFunction._cl.IsMaxContent(),
                            t => t.GrowthLimit,
                            IntrinsicContributionType.Maximum);
                    }
                }
                FlushPlannedBaseSizeIncreases(axisTracks);

                // Step 4: Ensure growth_limit >= base_size
                for (int i = 0; i < axisTracks.Count; i++)
                    if (axisTracks[i].GrowthLimit < axisTracks[i].BaseSize)
                        axisTracks[i].GrowthLimit = axisTracks[i].BaseSize;

                if (!isFlex)
                {
                    // Step 5: For intrinsic maximums (min-content contributions → growth limits)
                    for (int i = bStart; i < bEnd; i++)
                    {
                        var item = items[i];
                        float space = MinContent(item);
                        if (space > 0f)
                        {
                            var (ts, te) = item.TrackRangeExcludingLines(axis);
                            DistributeItemSpaceToGrowthLimit(space, axisTracks, ts, te,
                                t => !t.MaxTrackSizingFunction.DefiniteValue(axisInnerNodeSize).HasValue,
                                axisInnerNodeSize);
                        }
                    }
                    FlushPlannedGrowthLimitIncreases(axisTracks, true);

                    // Step 6: For max-content maximums
                    for (int i = bStart; i < bEnd; i++)
                    {
                        var item = items[i];
                        float space = MaxContent(item);
                        if (space > 0f)
                        {
                            var (ts, te) = item.TrackRangeExcludingLines(axis);
                            DistributeItemSpaceToGrowthLimit(space, axisTracks, ts, te,
                                t => t.MaxTrackSizingFunction.IsMaxContentAlike()
                                  || (t.MaxTrackSizingFunction.UsesPercentage() && !axisInnerNodeSize.HasValue),
                                axisInnerNodeSize);
                        }
                    }
                    FlushPlannedGrowthLimitIncreases(axisTracks, false);
                }
            }

            // Step 5 (final): Set infinite growth limits to base_size
            for (int i = 0; i < axisTracks.Count; i++)
                if (axisTracks[i].GrowthLimit == float.PositiveInfinity)
                    axisTracks[i].GrowthLimit = axisTracks[i].BaseSize;
        }

        // ── 11.6 Maximize tracks ──────────────────────────────────────────────

        static void MaximiseTracks(
            List<GridTrack> axisTracks,
            float? axisInnerNodeSize,
            AvailableSpace axisAvailGridSpace)
        {
            float usedSpace = 0f;
            for (int i = 0; i < axisTracks.Count; i++) usedSpace += axisTracks[i].BaseSize;
            float freeSpace = axisAvailGridSpace.ComputeFreeSpace(usedSpace);

            if (freeSpace == float.PositiveInfinity)
            {
                for (int i = 0; i < axisTracks.Count; i++)
                    axisTracks[i].BaseSize = axisTracks[i].GrowthLimit;
            }
            else if (freeSpace > 0f)
            {
                DistributeSpaceUpToLimits(freeSpace, axisTracks, 0, axisTracks.Count,
                    _ => true, _ => 1f,
                    t => t.BaseSize,
                    t => t.FitContentLimitedGrowthLimit(axisInnerNodeSize));

                for (int i = 0; i < axisTracks.Count; i++)
                {
                    axisTracks[i].BaseSize += axisTracks[i].ItemIncurredIncrease;
                    axisTracks[i].ItemIncurredIncrease = 0f;
                }
            }
        }

        // ── 11.7 Expand flexible tracks ───────────────────────────────────────

        static void ExpandFlexibleTracks(
            TaffyTree tree,
            AbstractAxis axis,
            List<GridTrack> axisTracks,
            List<GridItem> items,
            float? axisMinSize,
            float? axisMaxSize,
            AvailableSpace axisAvailForExpansion,
            Size<float?> innerNodeSize)
        {
            float flexFraction;

            if (axisAvailForExpansion.IsDefinite)
            {
                float available = axisAvailForExpansion.Unwrap();
                float usedSpace = 0f;
                for (int i = 0; i < axisTracks.Count; i++) usedSpace += axisTracks[i].BaseSize;
                float freeSpace = available - usedSpace;
                flexFraction = freeSpace <= 0f ? 0f : FindSizeOfFr(axisTracks, 0, axisTracks.Count, available);
            }
            else if (axisAvailForExpansion.IsMinContent)
            {
                flexFraction = 0f;
            }
            else // MaxContent
            {
                // Maximum of base_size/ff or base_size for flexible tracks
                float fromTracks = 0f;
                for (int i = 0; i < axisTracks.Count; i++)
                {
                    var t = axisTracks[i];
                    if (!t.MaxTrackSizingFunction.IsFr()) continue;
                    float ff = t.FlexFactor();
                    float candidate = ff > 1f ? t.BaseSize / ff : t.BaseSize;
                    if (candidate > fromTracks) fromTracks = candidate;
                }

                // Maximum fr size from items crossing flexible tracks
                float fromItems = 0f;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (!item.CrossesFlexibleTrack(axis)) continue;
                    var (ts, te) = item.TrackRangeExcludingLines(axis);
                    float maxContent = item.MaxContentContributionCached(axis, tree, SizeF.NONE, innerNodeSize);
                    float candidate = FindSizeOfFr(axisTracks, ts, te, maxContent);
                    if (candidate > fromItems) fromItems = candidate;
                }

                flexFraction = MathF.Max(fromTracks, fromItems);

                // Clamp to container min/max
                float hypothetical = 0f;
                for (int i = 0; i < axisTracks.Count; i++)
                {
                    var t = axisTracks[i];
                    hypothetical += t.MaxTrackSizingFunction.IsFr()
                        ? MathF.Max(t.BaseSize, t.MaxTrackSizingFunction._cl.Value * flexFraction)
                        : t.BaseSize;
                }
                float minSz = axisMinSize ?? 0f;
                float maxSz = axisMaxSize ?? float.PositiveInfinity;
                if (hypothetical < minSz)
                    flexFraction = FindSizeOfFr(axisTracks, 0, axisTracks.Count, minSz);
                else if (hypothetical > maxSz)
                    flexFraction = FindSizeOfFr(axisTracks, 0, axisTracks.Count, maxSz);
            }

            // Apply flex fraction to flexible tracks
            for (int i = 0; i < axisTracks.Count; i++)
            {
                var t = axisTracks[i];
                if (!t.MaxTrackSizingFunction.IsFr()) continue;
                t.BaseSize = MathF.Max(t.BaseSize, t.MaxTrackSizingFunction._cl.Value * flexFraction);
            }
        }

        // ── 11.7.1 Find size of an fr ─────────────────────────────────────────

        static float FindSizeOfFr(List<GridTrack> tracks, int start, int end, float spaceToFill)
        {
            if (spaceToFill == 0f) return 0f;

            float hypotheticalFrSize = float.PositiveInfinity;
            float prevHypotheticalFrSize;

            while (true)
            {
                float usedSpace = 0f;
                float naiveFlexSum = 0f;
                for (int i = start; i < end; i++)
                {
                    var t = tracks[i];
                    if (t.MaxTrackSizingFunction.IsFr()
                        && t.MaxTrackSizingFunction._cl.Value * hypotheticalFrSize >= t.BaseSize)
                        naiveFlexSum += t.MaxTrackSizingFunction._cl.Value;
                    else
                        usedSpace += t.BaseSize;
                }
                float leftover = spaceToFill - usedSpace;
                float flexFactor = MathF.Max(naiveFlexSum, 1f);

                prevHypotheticalFrSize = hypotheticalFrSize;
                hypotheticalFrSize = leftover / flexFactor;

                // Validate: check all flexible tracks
                bool valid = true;
                for (int i = start; i < end; i++)
                {
                    var t = tracks[i];
                    if (!t.MaxTrackSizingFunction.IsFr()) continue;
                    float ff = t.MaxTrackSizingFunction._cl.Value;
                    if (ff * hypotheticalFrSize < t.BaseSize
                        && ff * prevHypotheticalFrSize >= t.BaseSize)
                    { valid = false; break; }
                }
                if (valid) break;
            }

            return hypotheticalFrSize;
        }

        // ── 11.8 Stretch auto tracks ──────────────────────────────────────────

        static void StretchAutoTracks(
            List<GridTrack> axisTracks,
            float? axisMinSize,
            AvailableSpace axisAvailForExpansion)
        {
            int numAuto = 0;
            for (int i = 0; i < axisTracks.Count; i++)
                if (axisTracks[i].MaxTrackSizingFunction._cl.IsAuto()) numAuto++;
            if (numAuto == 0) return;

            float usedSpace = 0f;
            for (int i = 0; i < axisTracks.Count; i++) usedSpace += axisTracks[i].BaseSize;

            float freeSpace = axisAvailForExpansion.IsDefinite
                ? axisAvailForExpansion.ComputeFreeSpace(usedSpace)
                : (axisMinSize.HasValue ? axisMinSize.Value - usedSpace : 0f);

            if (freeSpace > 0f)
            {
                float extra = freeSpace / numAuto;
                for (int i = 0; i < axisTracks.Count; i++)
                    if (axisTracks[i].MaxTrackSizingFunction._cl.IsAuto())
                        axisTracks[i].BaseSize += extra;
            }
        }

        // ── Distribution helpers ──────────────────────────────────────────────

        static void DistributeItemSpaceToBaseSize(
            bool isFlex, bool useFlexFactorForDist, float space,
            List<GridTrack> tracks, int start, int end,
            Func<GridTrack, bool> trackIsAffected,
            Func<GridTrack, float> trackLimit,
            IntrinsicContributionType type)
        {
            if (isFlex)
            {
                Func<GridTrack, bool> flexFilter = t => t.IsFlexible() && trackIsAffected(t);
                Func<GridTrack, float> proportion = useFlexFactorForDist
                    ? (Func<GridTrack, float>)(t => t.FlexFactor()) : _ => 1f;
                DistributeItemSpaceToBaseSizeInner(
                    space, tracks, start, end, flexFilter, proportion, trackLimit, type);
            }
            else
            {
                DistributeItemSpaceToBaseSizeInner(
                    space, tracks, start, end, trackIsAffected, _ => 1f, trackLimit, type);
            }
        }

        static void DistributeItemSpaceToBaseSizeInner(
            float space,
            List<GridTrack> tracks, int start, int end,
            Func<GridTrack, bool> trackIsAffected,
            Func<GridTrack, float> trackDistributionProportion,
            Func<GridTrack, float> trackLimit,
            IntrinsicContributionType type)
        {
            if (space == 0f) return;
            bool anyAffected = false;
            for (int i = start; i < end; i++)
                if (trackIsAffected(tracks[i])) { anyAffected = true; break; }
            if (!anyAffected) return;

            float trackSizes = 0f;
            for (int i = start; i < end; i++) trackSizes += tracks[i].BaseSize;
            float extraSpace = MathF.Max(0f, space - trackSizes);

            const float THRESHOLD = 0.000001f;

            extraSpace = DistributeSpaceUpToLimits(
                extraSpace, tracks, start, end,
                trackIsAffected, trackDistributionProportion,
                t => t.BaseSize, trackLimit);

            if (extraSpace > THRESHOLD)
            {
                // "Distributing space beyond limits"
                Func<GridTrack, bool> beyondFilter = type == IntrinsicContributionType.Minimum
                    ? (Func<GridTrack, bool>)(t => t.MaxTrackSizingFunction.IsIntrinsic())
                    : t => t.MinTrackSizingFunction._cl.IsMaxContent()
                        || t.MaxTrackSizingFunction._cl.IsMaxOrFitContent();

                // If no affected tracks match, fall back to all tracks
                int count = 0;
                for (int i = start; i < end; i++)
                    if (trackIsAffected(tracks[i]) && beyondFilter(tracks[i])) count++;
                if (count == 0) beyondFilter = _ => true;

                DistributeSpaceUpToLimits(
                    extraSpace, tracks, start, end,
                    beyondFilter, trackDistributionProportion,
                    t => t.BaseSize, trackLimit);
            }

            // Commit to planned increase
            for (int i = start; i < end; i++)
            {
                var t = tracks[i];
                if (t.ItemIncurredIncrease > t.BaseSizePlannedIncrease)
                    t.BaseSizePlannedIncrease = t.ItemIncurredIncrease;
                t.ItemIncurredIncrease = 0f;
            }
        }

        static void DistributeItemSpaceToGrowthLimit(
            float space,
            List<GridTrack> tracks, int start, int end,
            Func<GridTrack, bool> trackIsAffected,
            float? axisInnerNodeSize)
        {
            if (space == 0f) return;
            bool anyAffected = false;
            for (int i = start; i < end; i++)
                if (trackIsAffected(tracks[i])) { anyAffected = true; break; }
            if (!anyAffected) return;

            float trackSizes = 0f;
            for (int i = start; i < end; i++)
            {
                var t = tracks[i];
                trackSizes += t.GrowthLimit == float.PositiveInfinity ? t.BaseSize : t.GrowthLimit;
            }
            float extraSpace = MathF.Max(0f, space - trackSizes);

            // Try to distribute to infinitely growable tracks first
            int growableCount = 0;
            for (int i = start; i < end; i++)
            {
                var t = tracks[i];
                if (!trackIsAffected(t)) continue;
                if (t.InfinitelyGrowable || t.FitContentLimitedGrowthLimit(axisInnerNodeSize) == float.PositiveInfinity)
                    growableCount++;
            }

            if (growableCount > 0)
            {
                float inc = extraSpace / growableCount;
                for (int i = start; i < end; i++)
                {
                    var t = tracks[i];
                    if (!trackIsAffected(t)) continue;
                    if (t.InfinitelyGrowable || t.FitContentLimitedGrowthLimit(axisInnerNodeSize) == float.PositiveInfinity)
                        t.ItemIncurredIncrease = inc;
                }
            }
            else
            {
                DistributeSpaceUpToLimits(
                    extraSpace, tracks, start, end,
                    trackIsAffected, _ => 1f,
                    t => t.GrowthLimit == float.PositiveInfinity ? t.BaseSize : t.GrowthLimit,
                    t => t.FitContentLimit(axisInnerNodeSize));
            }

            // Commit to growth limit planned increase
            for (int i = start; i < end; i++)
            {
                var t = tracks[i];
                if (t.ItemIncurredIncrease > t.GrowthLimitPlannedIncrease)
                    t.GrowthLimitPlannedIncrease = t.ItemIncurredIncrease;
                t.ItemIncurredIncrease = 0f;
            }
        }

        static float DistributeSpaceUpToLimits(
            float spaceToDistribute,
            List<GridTrack> tracks, int start, int end,
            Func<GridTrack, bool> trackIsAffected,
            Func<GridTrack, float> trackDistributionProportion,
            Func<GridTrack, float> trackAffectedProperty,
            Func<GridTrack, float> trackLimit)
        {
            const float THRESHOLD = 0.01f;

            while (spaceToDistribute > THRESHOLD)
            {
                float proportionSum = 0f;
                for (int i = start; i < end; i++)
                {
                    var t = tracks[i];
                    if (!trackIsAffected(t)) continue;
                    if (trackAffectedProperty(t) + t.ItemIncurredIncrease < trackLimit(t))
                        proportionSum += trackDistributionProportion(t);
                }
                if (proportionSum == 0f) break;

                float minIncreaseLimit = float.PositiveInfinity;
                for (int i = start; i < end; i++)
                {
                    var t = tracks[i];
                    if (!trackIsAffected(t)) continue;
                    float prop = trackAffectedProperty(t) + t.ItemIncurredIncrease;
                    float lim  = trackLimit(t);
                    if (prop >= lim) continue;
                    float candidate = (lim - trackAffectedProperty(t)) / trackDistributionProportion(t);
                    if (candidate < minIncreaseLimit) minIncreaseLimit = candidate;
                }

                float iterIncrease = MathF.Min(minIncreaseLimit, spaceToDistribute / proportionSum);

                for (int i = start; i < end; i++)
                {
                    var t = tracks[i];
                    if (!trackIsAffected(t)) continue;
                    float increase = iterIncrease * trackDistributionProportion(t);
                    if (increase > 0f
                        && trackAffectedProperty(t) + increase <= trackLimit(t) + THRESHOLD)
                    {
                        t.ItemIncurredIncrease += increase;
                        spaceToDistribute -= increase;
                    }
                }
            }

            return spaceToDistribute;
        }

        // ── Flush helpers ─────────────────────────────────────────────────────

        static void FlushPlannedBaseSizeIncreases(List<GridTrack> axisTracks)
        {
            for (int i = 0; i < axisTracks.Count; i++)
            {
                var t = axisTracks[i];
                t.BaseSize += t.BaseSizePlannedIncrease;
                t.BaseSizePlannedIncrease = 0f;
            }
        }

        static void FlushPlannedGrowthLimitIncreases(List<GridTrack> axisTracks, bool setInfinitelyGrowable)
        {
            for (int i = 0; i < axisTracks.Count; i++)
            {
                var t = axisTracks[i];
                if (t.GrowthLimitPlannedIncrease > 0f)
                {
                    t.GrowthLimit = t.GrowthLimit == float.PositiveInfinity
                        ? t.BaseSize + t.GrowthLimitPlannedIncrease
                        : t.GrowthLimit + t.GrowthLimitPlannedIncrease;
                    t.InfinitelyGrowable = setInfinitelyGrowable;
                }
                else
                {
                    t.InfinitelyGrowable = false;
                }
                t.GrowthLimitPlannedIncrease = 0f;
            }
        }

        // ── Sort comparator ───────────────────────────────────────────────────

        static Comparison<GridItem> CmpByCrossFlexThenSpanThenStart(AbstractAxis axis) =>
            (a, b) =>
            {
                bool aFlex = a.CrossesFlexibleTrack(axis);
                bool bFlex = b.CrossesFlexibleTrack(axis);
                if (!aFlex && bFlex) return -1;
                if (aFlex && !bFlex) return 1;
                var pA = a.Placement(axis);
                var pB = b.Placement(axis);
                int spanCmp = pA.Span().CompareTo(pB.Span());
                return spanCmp != 0 ? spanCmp : pA.Start.CompareTo(pB.Start);
            };
    }
}
