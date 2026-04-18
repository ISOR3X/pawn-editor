// Port of taffy/src/compute/grid/alignment.rs
//       and taffy/src/compute/common/alignment.rs
//
// Track alignment and item final positioning for CSS Grid layout.

namespace Taffy
{
    internal static class GridAlignment
    {
        // ── From common/alignment.rs ──────────────────────────────────────────

        /// <summary>Applies the CSS alignment fallback rules.</summary>
        internal static AlignContent ApplyAlignmentFallback(
            float freeSpace, int numItems, AlignContent alignment, bool isSafe)
        {
            if (numItems <= 1 || freeSpace <= 0f)
            {
                bool safe;
                (alignment, safe) = alignment switch
                {
                    AlignContent.Stretch      => (AlignContent.FlexStart, true),
                    AlignContent.SpaceBetween => (AlignContent.FlexStart, true),
                    AlignContent.SpaceAround  => (AlignContent.Center,    true),
                    AlignContent.SpaceEvenly  => (AlignContent.Center,    true),
                    _                         => (alignment,               isSafe),
                };
                isSafe = safe;
            }

            if (freeSpace <= 0f && isSafe)
                alignment = AlignContent.Start;

            return alignment;
        }

        internal static float ComputeAlignmentOffset(
            float freeSpace, int numItems, float gap,
            AlignContent alignmentMode, bool layoutIsFlexReversed, bool isFirst)
        {
            if (isFirst)
            {
                return alignmentMode switch
                {
                    AlignContent.Start        => 0f,
                    AlignContent.FlexStart    => layoutIsFlexReversed ? freeSpace : 0f,
                    AlignContent.End          => freeSpace,
                    AlignContent.FlexEnd      => layoutIsFlexReversed ? 0f : freeSpace,
                    AlignContent.Center       => freeSpace / 2f,
                    AlignContent.Stretch      => 0f,
                    AlignContent.SpaceBetween => 0f,
                    AlignContent.SpaceAround  => freeSpace >= 0f
                        ? (freeSpace / numItems) / 2f
                        : freeSpace / 2f,
                    AlignContent.SpaceEvenly  => freeSpace >= 0f
                        ? freeSpace / (numItems + 1)
                        : freeSpace / 2f,
                    _ => 0f,
                };
            }
            else
            {
                float fs = MathF.Max(freeSpace, 0f);
                return gap + alignmentMode switch
                {
                    AlignContent.SpaceBetween => numItems > 1 ? fs / (numItems - 1) : 0f,
                    AlignContent.SpaceAround  => fs / numItems,
                    AlignContent.SpaceEvenly  => fs / (numItems + 1),
                    _                         => 0f,
                };
            }
        }

        // ── align_tracks ──────────────────────────────────────────────────────

        /// <summary>
        /// Assigns <see cref="GridTrack.Offset"/> for each track based on container size and alignment.
        /// </summary>
        internal static void AlignTracks(
            float gridContainerContentBoxSize,
            Line<float> padding,
            Line<float> border,
            List<GridTrack> tracks,
            AlignContent trackAlignmentStyle,
            bool axisIsReversed)
        {
            float usedSize = 0f;
            for (int i = 0; i < tracks.Count; i++) usedSize += tracks[i].BaseSize;

            float freeSpace = gridContainerContentBoxSize - usedSize;
            float origin = padding.Start + border.Start;

            // Count non-collapsed actual tracks (odd indices, since even = gutters)
            int numTracks = 0;
            for (int i = 1; i < tracks.Count; i += 2)
                if (!tracks[i].IsCollapsed) numTracks++;

            var trackAlignment = ApplyAlignmentFallback(freeSpace, numTracks, trackAlignmentStyle, false);
            if (axisIsReversed) trackAlignment = trackAlignment.Reversed();

            float totalOffset = origin;
            bool seenNonCollapsed = false;

            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                bool isGutter           = (i % 2 == 0);
                bool isNonCollapsed     = !isGutter && !track.IsCollapsed;
                bool isFirst            = isNonCollapsed && !seenNonCollapsed;

                float offset = isNonCollapsed
                    ? ComputeAlignmentOffset(freeSpace, numTracks, 0f, trackAlignment, false, isFirst)
                    : 0f;

                track.Offset = totalOffset + offset;
                totalOffset += offset + track.BaseSize;
                if (isNonCollapsed) seenNonCollapsed = true;
            }
        }

        // ── align_and_position_item ───────────────────────────────────────────

        /// <summary>
        /// Computes final size and position for a grid item and stores the result via
        /// <see cref="TaffyTree.SetNodeLayout"/>. Returns <c>(y, height)</c> for baseline computation.
        /// </summary>
        internal static (float y, float height) AlignAndPositionItem(
            TaffyTree tree,
            NodeId node,
            uint order,
            Rect<float> gridArea,
            InBothAbsAxis<AlignItems?> containerAlignmentStyles,
            float baselineShim,
            Direction direction)
        {
            var gridAreaSize = new Size<float>(
                gridArea.Right - gridArea.Left,
                gridArea.Bottom - gridArea.Top);
            var gridAreaSizeOpt = new Size<float?>(gridAreaSize.Width, gridAreaSize.Height);

            var style = tree.GetStyle(node);

            var overflow       = style.overflow;
            var scrollbarWidth = style.scrollbarWidth;
            var aspectRatio    = style.aspectRatio;
            var justifySelf    = style.justifySelf;
            var alignSelf      = style.alignSelf;
            var position       = style.position;

            var insetHorizontal = style.inset.HorizontalComponents().Map(
                lpa => lpa.MaybeResolve(gridAreaSize.Width));
            var insetVertical   = style.inset.VerticalComponents().Map(
                lpa => lpa.MaybeResolve(gridAreaSize.Height));

            var padding = style.padding.ResolveOrZero((float?)gridAreaSize.Width);
            var border  = style.border.ResolveOrZero((float?)gridAreaSize.Width);
            var pbSum   = SizeF.Add(RectF.SumAxes(padding), RectF.SumAxes(border));
            var boxAdj  = style.boxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

            var inherentSize = SizeF.MaybeApplyAspectRatio(
                style.size.MaybeResolve(gridAreaSizeOpt).MaybeAdd(boxAdj), aspectRatio);

            var minSize = SizeF.MaybeApplyAspectRatio(
                style.minSize.MaybeResolve(gridAreaSizeOpt)
                    .MaybeAdd(boxAdj)
                    .Or(new Size<float?>(pbSum.Width, pbSum.Height))
                    .MaybeMax(pbSum),
                aspectRatio);

            var maxSize = SizeF.MaybeApplyAspectRatio(
                style.maxSize.MaybeResolve(gridAreaSizeOpt).MaybeAdd(boxAdj), aspectRatio);

            // Resolve alignment styles with fallbacks
            var alignH = justifySelf ?? containerAlignmentStyles.Horizontal ??
                (inherentSize.Width.HasValue ? AlignItems.Start : AlignItems.Stretch);
            var alignV = alignSelf ?? containerAlignmentStyles.Vertical ??
                (inherentSize.Height.HasValue || aspectRatio.HasValue ? AlignItems.Start : AlignItems.Stretch);

            // Margins (all sides resolve against width per spec)
            var margin = style.margin.MaybeResolve(gridAreaSize.Width);

            var gridAreaMinusMargins = new Size<float>(
                gridAreaSize.Width.MaybeSub(margin.Left).MaybeSub(margin.Right),
                gridAreaSize.Height.MaybeSub(margin.Top).MaybeSub(margin.Bottom) - baselineShim);

            // Resolve width
            float? width = inherentSize.Width;
            if (!width.HasValue)
            {
                if (position == Position.Absolute
                    && insetHorizontal.Start.HasValue && insetHorizontal.End.HasValue)
                {
                    width = MathF.Max(
                        gridAreaMinusMargins.Width - insetHorizontal.Start.Value - insetHorizontal.End.Value, 0f);
                }
                else if (position != Position.Absolute
                    && margin.Left.HasValue && margin.Right.HasValue
                    && alignH == AlignItems.Stretch)
                {
                    width = gridAreaMinusMargins.Width;
                }
            }

            // Reapply aspect ratio (uses inherent_size.height per Rust)
            var afterW = SizeF.MaybeApplyAspectRatio(new Size<float?>(width, inherentSize.Height), aspectRatio);
            width  = afterW.Width;
            float? height = afterW.Height;

            // Resolve height
            if (!height.HasValue)
            {
                if (position == Position.Absolute
                    && insetVertical.Start.HasValue && insetVertical.End.HasValue)
                {
                    height = MathF.Max(
                        gridAreaMinusMargins.Height - insetVertical.Start.Value - insetVertical.End.Value, 0f);
                }
                else if (position != Position.Absolute
                    && margin.Top.HasValue && margin.Bottom.HasValue
                    && alignV == AlignItems.Stretch)
                {
                    height = gridAreaMinusMargins.Height;
                }
            }

            // Reapply aspect ratio after height resolution
            var afterH = SizeF.MaybeApplyAspectRatio(new Size<float?>(width, height), aspectRatio);
            width  = afterH.Width;
            height = afterH.Height;

            // Clamp by min/max
            width  = width.MaybeClamp(minSize.Width,  maxSize.Width);
            height = height.MaybeClamp(minSize.Height, maxSize.Height);

            // Perform child layout
            var knownDims = new Size<float?>(width, height);
            var layoutInput = new LayoutInput
            {
                KnownDimensions = knownDims,
                ParentSize      = gridAreaSizeOpt,
                availableSpace  = new Size<AvailableSpace>(
                    AvailableSpace.Definite(gridAreaMinusMargins.Width),
                    AvailableSpace.Definite(gridAreaMinusMargins.Height)),
                SizingMode      = SizingMode.InherentSize,
                Axis            = RequestedAxis.Both,
                RunMode         = RunMode.PerformLayout,
                VerticalMarginsAreCollapsible = LineHelpers.FALSE,
            };
            var layoutOutput = tree.PerformLayout(node, layoutInput);

            // Resolve final size (unwrap_or from layout output then clamp)
            float finalWidth  = (width  ?? layoutOutput.Size.Width ).MaybeClamp(minSize.Width,  maxSize.Width);
            float finalHeight = (height ?? layoutOutput.Size.Height).MaybeClamp(minSize.Height, maxSize.Height);

            // Position within grid area
            var (x, xMargin) = AlignItemWithinArea(
                new Line<float>(gridArea.Left,  gridArea.Right),
                alignH,
                finalWidth,
                position,
                insetHorizontal,
                margin.HorizontalComponents(),
                0f,
                direction);

            var (y, yMargin) = AlignItemWithinArea(
                new Line<float>(gridArea.Top, gridArea.Bottom),
                alignV,
                finalHeight,
                position,
                insetVertical,
                margin.VerticalComponents(),
                baselineShim,
                Direction.Ltr);

            var scrollbarSize = new Size<float>(
                overflow.Y == Overflow.Scroll ? scrollbarWidth : 0f,
                overflow.X == Overflow.Scroll ? scrollbarWidth : 0f);

            var finalLayout = Layout.New();
            finalLayout.Order         = order;
            finalLayout.Location      = new Point<float>(x, y);
            finalLayout.Size          = new Size<float>(finalWidth, finalHeight);
            finalLayout.ContentSize   = layoutOutput.ContentSize;
            finalLayout.ScrollbarSize = scrollbarSize;
            finalLayout.Padding       = padding;
            finalLayout.Border        = border;
            finalLayout.Margin        = new Rect<float>(xMargin.Start, xMargin.End, yMargin.Start, yMargin.End);

            tree.SetNodeLayout(node, finalLayout);

            return (y, finalHeight);
        }

        // ── align_item_within_area ────────────────────────────────────────────

        private static (float pos, Line<float> margin) AlignItemWithinArea(
            Line<float> gridArea,
            AlignItems alignmentStyle,
            float resolvedSize,
            Position position,
            Line<float?> inset,
            Line<float?> margin,
            float baselineShim,
            Direction direction)
        {
            var nonAutoMargin = new Line<float>(
                (margin.Start ?? 0f) + baselineShim,
                margin.End ?? 0f);

            float gridAreaSize = MathF.Max(gridArea.End - gridArea.Start, 0f);
            float freeSpace = MathF.Max(
                gridAreaSize - resolvedSize - nonAutoMargin.Start - nonAutoMargin.End, 0f);

            int autoCount = (margin.Start.HasValue ? 0 : 1) + (margin.End.HasValue ? 0 : 1);
            float autoSize = autoCount > 0 ? freeSpace / autoCount : 0f;

            var resolvedMargin = new Line<float>(
                (margin.Start ?? autoSize) + baselineShim,
                margin.End ?? autoSize);

            bool isRtl = direction == Direction.Rtl;
            float alignmentOffset = alignmentStyle switch
            {
                AlignItems.Start or AlignItems.FlexStart or AlignItems.Baseline or AlignItems.Stretch =>
                    isRtl ? gridAreaSize - resolvedSize - resolvedMargin.End : resolvedMargin.Start,
                AlignItems.End or AlignItems.FlexEnd =>
                    isRtl ? resolvedMargin.Start : gridAreaSize - resolvedSize - resolvedMargin.End,
                AlignItems.Center =>
                    (gridAreaSize - resolvedSize + resolvedMargin.Start - resolvedMargin.End) / 2f,
                _ => resolvedMargin.Start,
            };

            float offsetWithinArea;
            if (position == Position.Absolute)
            {
                if (inset.Start.HasValue && inset.End.HasValue)
                    offsetWithinArea = isRtl
                        ? gridAreaSize - inset.End.Value - resolvedSize - nonAutoMargin.End
                        : inset.Start.Value + nonAutoMargin.Start;
                else if (inset.Start.HasValue)
                    offsetWithinArea = inset.Start.Value + nonAutoMargin.Start;
                else if (inset.End.HasValue)
                    offsetWithinArea = gridAreaSize - inset.End.Value - resolvedSize - nonAutoMargin.End;
                else
                    offsetWithinArea = alignmentOffset;
            }
            else
            {
                offsetWithinArea = alignmentOffset;
            }

            float startPos = gridArea.Start + offsetWithinArea;
            if (position == Position.Relative)
            {
                float? relInset = isRtl
                    ? (inset.End.HasValue ? (float?)(-inset.End.Value) : inset.Start)
                    : (inset.Start.HasValue ? inset.Start : (inset.End.HasValue ? (float?)(-inset.End.Value) : null));
                startPos += relInset ?? 0f;
            }

            return (startPos, resolvedMargin);
        }
    }
}
