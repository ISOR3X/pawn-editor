// Port of taffy/src/compute/block.rs
//
// Implements CSS Block layout for Taffy.
// Float layout (#[cfg(feature = "float_layout")]) is omitted.
// CSS calc() is not supported; percentages resolve directly.

namespace Taffy
{
    // ── BlockContext ──────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal tracking context for a Block Formatting Context.
    /// Without float support this only records whether this is the BFC root.
    /// </summary>
    internal sealed class BlockContext
    {
        private readonly bool _isRoot;

        public BlockContext(bool isRoot = true)
        {
            _isRoot = isRoot;
        }

        public bool IsBfcRoot() => _isRoot;

        public BlockContext SubContext() => new BlockContext(false);
    }

    // ── BlockCompute ─────────────────────────────────────────────────────────

    /// <summary>
    /// CSS Block layout algorithm.
    /// Entry point: <see cref="Compute"/>.
    /// </summary>
    internal static class BlockCompute
    {
        // ── Public entry point ───────────────────────────────────────────────

        /// <summary>
        /// Computes block layout for <paramref name="nodeId"/> and writes child layouts
        /// into <paramref name="tree"/>.
        /// </summary>
        public static LayoutOutput Compute(TaffyTree tree, NodeId nodeId, LayoutInput input,
            BlockContext? blockCtx)
        {
            var style = tree.GetStyle(nodeId);
            var parentSize = input.ParentSize;

            var overflow = style.overflow;
            var isScrollContainer = overflow.X.IsScrollContainer() || overflow.Y.IsScrollContainer();
            var aspectRatio = style.aspectRatio;

            var padding = style.padding.ResolveOrZero(parentSize.Width);
            var border = style.border.ResolveOrZero(parentSize.Width);
            var paddingBorderSize = RectF.SumAxes(RectF.Add(padding, border));

            var boxSizingAdj = style.boxSizing == BoxSizing.ContentBox ? paddingBorderSize : SizeF.ZERO;

            var minSize = style.minSize.MaybeResolve(parentSize)
                .MaybeApplyAspectRatio(aspectRatio)
                .MaybeAdd(boxSizingAdj);
            var maxSize = style.maxSize.MaybeResolve(parentSize)
                .MaybeApplyAspectRatio(aspectRatio)
                .MaybeAdd(boxSizingAdj);

            Size<float?> clampedStyleSize;
            if (input.SizingMode == SizingMode.InherentSize)
            {
                clampedStyleSize = style.size.MaybeResolve(parentSize)
                    .MaybeApplyAspectRatio(aspectRatio)
                    .MaybeAdd(boxSizingAdj)
                    .MaybeClamp(minSize, maxSize);
            }
            else
            {
                clampedStyleSize = SizeF.NONE;
            }

            // If both min and max in an axis are set and max <= min, that axis is determined.
            var minMaxDefiniteSize = new Size<float?>(
                MinMaxDefinite(minSize.Width, maxSize.Width),
                MinMaxDefinite(minSize.Height, maxSize.Height));

            var knownDimensions = input.KnownDimensions
                .Or(minMaxDefiniteSize)
                .Or(clampedStyleSize)
                .MaybeMax(paddingBorderSize);

            // Short-circuit for ComputeSize if both dimensions are now known.
            if (input.RunMode == RunMode.ComputeSize)
            {
                if (knownDimensions.Width.HasValue && knownDimensions.Height.HasValue)
                    return LayoutOutput.FromOuterSize(
                        new Size<float>(knownDimensions.Width!.Value, knownDimensions.Height!.Value));
            }

            // Unwrap or create a BFC root.
            var updatedInput = input;
            updatedInput.KnownDimensions = knownDimensions;

            if (blockCtx != null && !isScrollContainer)
                return ComputeInner(tree, nodeId, updatedInput, blockCtx);

            var rootCtx = new BlockContext(isRoot: true);
            return ComputeInner(tree, nodeId, updatedInput, rootCtx);
        }

        // ── Inner algorithm ──────────────────────────────────────────────────

        private static LayoutOutput ComputeInner(TaffyTree tree, NodeId nodeId, LayoutInput input,
            BlockContext blockCtx)
        {
            var knownDimensions = input.KnownDimensions;
            var parentSize = input.ParentSize;
            var availableSpace = input.availableSpace;
            var runMode = input.RunMode;
            var vertMarginCollapsible = input.VerticalMarginsAreCollapsible;

            var style = tree.GetStyle(nodeId);
            var rawPadding = style.padding;
            var rawBorder = style.border;
            var rawMargin = style.margin;
            var aspectRatio = style.aspectRatio;
            var direction = style.direction;

            var padding = rawPadding.ResolveOrZero(parentSize.Width);
            var border = rawBorder.ResolveOrZero(parentSize.Width);

            // Scrollbar gutters: axis is transposed because a node that scrolls on Y
            // needs X space for the scrollbar, and vice-versa.
            var overflowTransposed = style.overflow.Transpose();
            var sbOffsets = overflowTransposed.Map(o => o == Overflow.Scroll ? style.scrollbarWidth : 0f);
            Rect<float> scrollbarGutter;
            if (direction == Direction.Ltr)
                scrollbarGutter = new Rect<float>(0f, sbOffsets.X, 0f, sbOffsets.Y);
            else
                scrollbarGutter = new Rect<float>(sbOffsets.X, 0f, 0f, sbOffsets.Y);

            var paddingBorder = RectF.Add(padding, border);
            var paddingBorderSize = RectF.SumAxes(paddingBorder);
            var contentBoxInset = RectF.Add(paddingBorder, scrollbarGutter);
            var containerContentBoxSize = knownDimensions.MaybeSub(RectF.SumAxes(contentBoxInset));

            var overflow = style.overflow;
            var isScrollContainer = overflow.X.IsScrollContainer() || overflow.Y.IsScrollContainer();
            var boxSizingAdj = style.boxSizing == BoxSizing.ContentBox ? paddingBorderSize : SizeF.ZERO;

            var size = style.size.MaybeResolve(parentSize).MaybeApplyAspectRatio(aspectRatio).MaybeAdd(boxSizingAdj);
            var minSize = style.minSize.MaybeResolve(parentSize).MaybeApplyAspectRatio(aspectRatio)
                .MaybeAdd(boxSizingAdj);
            var maxSize = style.maxSize.MaybeResolve(parentSize).MaybeApplyAspectRatio(aspectRatio)
                .MaybeAdd(boxSizingAdj);

            // Margin collapsing flags.
            var ownMarginsCollapseWithChildren = new Line<bool>(
                start: vertMarginCollapsible.Start
                       && !isScrollContainer
                       && style.position == Position.Relative
                       && padding.Top == 0f
                       && border.Top == 0f,
                end: vertMarginCollapsible.End
                     && !isScrollContainer
                     && style.position == Position.Relative
                     && padding.Bottom == 0f
                     && border.Bottom == 0f
                     && !size.Height.HasValue);

            var hasStylesPreventingCollapse =
                !style.IsBlock()
                || blockCtx.IsBfcRoot()
                || isScrollContainer
                || style.position == Position.Absolute
                || padding.Top > 0f
                || padding.Bottom > 0f
                || border.Top > 0f
                || border.Bottom > 0f
                || (size.Height.HasValue && size.Height.Value > 0f)
                || (minSize.Height.HasValue && minSize.Height.Value > 0f);

            var textAlign = style.textAlign;

            // 1. Generate item list.
            var items = GenerateItemList(tree, nodeId, containerContentBoxSize);

            // 2. Compute container width.
            float containerOuterWidth;
            if (knownDimensions.Width.HasValue)
            {
                containerOuterWidth = knownDimensions.Width.Value;
            }
            else
            {
                var availableWidth =
                    availableSpace.Width.MaybeSub(RectF.HorizontalAxisSum(contentBoxInset));
                var intrinsicWidth =
                    DetermineContentBasedContainerWidth(tree, items, availableWidth)
                    + RectF.HorizontalAxisSum(contentBoxInset);
                containerOuterWidth = intrinsicWidth
                    .MaybeClamp(minSize.Width, maxSize.Width)
                    .MaybeMax(paddingBorderSize.Width);
            }

            // Short-circuit if ComputeSize and height already known.
            if (runMode == RunMode.ComputeSize && knownDimensions.Height.HasValue)
                return LayoutOutput.FromOuterSize(new Size<float>(containerOuterWidth, knownDimensions.Height.Value));

            var containerPctResolutionHeight =
                knownDimensions.Height
                ?? size.Height.MaybeMax(minSize.Height)
                ?? minSize.Height;

            // 3. Final layout of in-flow children.
            var resolvedPadding = rawPadding.ResolveOrZero((float?)containerOuterWidth);
            var resolvedBorder = rawBorder.ResolveOrZero((float?)containerOuterWidth);
            var resolvedContentBoxInset = RectF.Add(RectF.Add(resolvedPadding, resolvedBorder), scrollbarGutter);

            PerformFinalLayoutOnInFlowChildren(
                tree, items,
                containerOuterWidth, containerPctResolutionHeight,
                contentBoxInset, resolvedContentBoxInset,
                textAlign, direction,
                ownMarginsCollapseWithChildren,
                out var inflowContentSize,
                out var intrinsicOuterHeight,
                out var firstChildTopMarginSet,
                out var lastChildBottomMarginSet);

            var containerOuterHeight = knownDimensions.Height
                                       ?? intrinsicOuterHeight
                                           .MaybeClamp(minSize.Height, maxSize.Height)
                                           .MaybeMax(paddingBorderSize.Height);

            var finalOuterSize = new Size<float>(containerOuterWidth, containerOuterHeight);

            // Short-circuit for ComputeSize.
            if (runMode == RunMode.ComputeSize)
                return LayoutOutput.FromOuterSize(finalOuterSize);

            // 4. Layout absolutely positioned children.
            var absolutePositionInset = RectF.Add(resolvedBorder, scrollbarGutter);
            var absolutePositionArea = SizeF.Sub(finalOuterSize, RectF.SumAxes(absolutePositionInset));
            var absolutePositionOffset = new Point<float>(absolutePositionInset.Left, absolutePositionInset.Top);
            var absoluteContentSize = PerformAbsoluteLayoutOnAbsoluteChildren(
                tree, items, absolutePositionArea, absolutePositionOffset, direction);

            // 5. Hidden layout for box-generation:none children.
            var childCount = tree.ChildCount(nodeId);
            for (var order = 0; order < childCount; order++)
            {
                var child = tree.ChildAt(nodeId, order);
                var childStyle = tree.GetStyle(child);
                if (childStyle.boxGenerationMode == BoxGenerationMode.None)
                {
                    tree.SetNodeLayout(child, Layout.WithOrder((uint)order));
                    tree.PerformLayout(child, LayoutInput.Hidden);
                }
            }

            // 7. Determine margin-collapse-through.
            var allInFlowChildrenCanCollapseThrough = true;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.position != Position.Absolute && !item.canBeCollapsedThrough)
                {
                    allInFlowChildrenCanCollapseThrough = false;
                    break;
                }
            }

            var canBeCollapsedThrough = !hasStylesPreventingCollapse && allInFlowChildrenCanCollapseThrough;

            var contentSize = inflowContentSize.F32Max(absoluteContentSize);

            CollapsibleMarginSet topMargin;
            if (ownMarginsCollapseWithChildren.Start)
            {
                topMargin = firstChildTopMarginSet;
            }
            else
            {
                var marginTopVal = rawMargin.Top.ResolveOrZero(parentSize.Width);
                topMargin = CollapsibleMarginSet.FromMargin(marginTopVal);
            }

            CollapsibleMarginSet bottomMargin;
            if (ownMarginsCollapseWithChildren.End)
            {
                bottomMargin = lastChildBottomMarginSet;
            }
            else
            {
                var marginBottomVal = rawMargin.Bottom.ResolveOrZero(parentSize.Width);
                bottomMargin = CollapsibleMarginSet.FromMargin(marginBottomVal);
            }

            return new LayoutOutput
            {
                Size = finalOuterSize,
                ContentSize = contentSize,
                FirstBaselines = new Point<float?>(null, null),
                TopMargin = topMargin,
                BottomMargin = bottomMargin,
                MarginsCanCollapseThrough = canBeCollapsedThrough,
            };
        }

        // ── Generate item list ────────────────────────────────────────────────

        private static List<BlockItem> GenerateItemList(TaffyTree tree, NodeId nodeId,
            Size<float?> nodeInnerSize)
        {
            var childCount = tree.ChildCount(nodeId);
            var items = new List<BlockItem>(childCount);
            uint visibleOrder = 0;

            for (var i = 0; i < childCount; i++)
            {
                var childId = tree.ChildAt(nodeId, i);
                var childStyle = tree.GetStyle(childId);

                if (childStyle.boxGenerationMode == BoxGenerationMode.None)
                    continue;

                var aspectRatio = childStyle.aspectRatio;
                var padding = childStyle.padding.ResolveOrZero(nodeInnerSize.Width);
                var border = childStyle.border.ResolveOrZero(nodeInnerSize.Width);
                var pbSum = RectF.SumAxes(RectF.Add(padding, border));
                var bsAdj = childStyle.boxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

                var position = childStyle.position;
                var overflow = childStyle.overflow;
                var isScrollContainer = overflow.X.IsScrollContainer() || overflow.Y.IsScrollContainer();
                var isBlock = childStyle.IsBlock();
                var isTable = childStyle.itemIsTable;
                var isInSameBfc = isBlock && !isTable
                                          && position != Position.Absolute
                                          && !isScrollContainer;

                var item = new BlockItem
                {
                    nodeId = childId,
                    order = visibleOrder++,
                    isTable = isTable,
                    isInSameBfc = isInSameBfc,
                    size = childStyle.size.MaybeResolve(nodeInnerSize).MaybeApplyAspectRatio(aspectRatio)
                        .MaybeAdd(bsAdj),
                    minSize = childStyle.minSize.MaybeResolve(nodeInnerSize).MaybeApplyAspectRatio(aspectRatio)
                        .MaybeAdd(bsAdj),
                    maxSize = childStyle.maxSize.MaybeResolve(nodeInnerSize).MaybeApplyAspectRatio(aspectRatio)
                        .MaybeAdd(bsAdj),
                    overflow = overflow,
                    scrollbarWidth = childStyle.scrollbarWidth,
                    position = position,
                    inset = childStyle.inset,
                    margin = childStyle.margin,
                    padding = padding,
                    border = border,
                    paddingBorderSum = pbSum,
                    computedSize = SizeF.ZERO,
                    staticPosition = PointF.ZERO,
                    canBeCollapsedThrough = false,
                };
                items.Add(item);
            }

            return items;
        }

        // ── Content-based container width ─────────────────────────────────────

        private static float DetermineContentBasedContainerWidth(TaffyTree tree,
            List<BlockItem> items, AvailableSpace availableWidth)
        {
            var availableSpace = new Size<AvailableSpace>(availableWidth, AvailableSpace.MinContent);

            var maxChildWidth = 0f;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.position == Position.Absolute)
                    continue;

                var knownDimensions = item.size.MaybeClamp(item.minSize, item.maxSize);

                var itemXMarginSum = RectF.HorizontalAxisSum(
                    item.margin.MaybeResolve(availableSpace.Width.IntoOption()).map(m => m ?? 0f));

                float width;
                if (knownDimensions.Width.HasValue)
                {
                    width = knownDimensions.Width.Value;
                }
                else
                {
                    var childInput = new LayoutInput
                    {
                        RunMode = RunMode.PerformLayout,
                        SizingMode = SizingMode.InherentSize,
                        Axis = RequestedAxis.Both,
                        KnownDimensions = knownDimensions,
                        ParentSize = SizeF.NONE,
                        availableSpace = availableSpace.MapWidth(w => w.MaybeSub(itemXMarginSum)),
                        VerticalMarginsAreCollapsible = LineHelpers.TRUE,
                    };
                    var output = tree.PerformLayout(item.nodeId, childInput);
                    width = output.Size.Width;
                }

                var totalWidth = MathF.Max(width, item.paddingBorderSum.Width) + itemXMarginSum;
                maxChildWidth = MathF.Max(maxChildWidth, totalWidth);
            }

            return maxChildWidth;
        }

        // ── Final layout of in-flow children ──────────────────────────────────

        private static void PerformFinalLayoutOnInFlowChildren(
            TaffyTree tree,
            List<BlockItem> items,
            float containerOuterWidth,
            float? containerPctResolutionHeight,
            Rect<float> contentBoxInset,
            Rect<float> resolvedContentBoxInset,
            TextAlign textAlign,
            Direction direction,
            Line<bool> ownMarginsCollapseWithChildren,
            out Size<float> inflowContentSize,
            out float intrinsicOuterHeight,
            out CollapsibleMarginSet firstChildTopMarginSet,
            out CollapsibleMarginSet lastChildBottomMarginSet)
        {
            var containerInnerWidth =
                containerOuterWidth - RectF.HorizontalAxisSum(resolvedContentBoxInset);
            var resolvedPctHeight =
                containerPctResolutionHeight.MaybeSub(RectF.VerticalAxisSum(resolvedContentBoxInset));
            var parentSize = new Size<float?>(containerInnerWidth, resolvedPctHeight);
            var availableSpace = new Size<AvailableSpace>(
                AvailableSpace.Definite(containerInnerWidth),
                AvailableSpace.MinContent);

            inflowContentSize = SizeF.ZERO;
            var committedYOffset = resolvedContentBoxInset.Top;
            var yOffsetForAbsolute = resolvedContentBoxInset.Top;
            firstChildTopMarginSet = CollapsibleMarginSet.ZERO;
            var activeCollapsibleMarginSet = CollapsibleMarginSet.ZERO;
            var isCollapsingWithFirstMarginSet = true;

            for (var idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];

                if (item.position == Position.Absolute)
                {
                    var staticX = direction == Direction.Ltr
                        ? resolvedContentBoxInset.Left
                        : containerOuterWidth - resolvedContentBoxInset.Right;
                    item.staticPosition = new Point<float>(staticX, yOffsetForAbsolute);
                    items[idx] = item;
                    continue;
                }

                // Resolve margins (auto → 0 for now; expanded later for x-axis).
                var itemMargin = item.margin.MaybeResolve((float?)containerOuterWidth);
                var itemNonAutoMargin = itemMargin.map(m => m ?? 0f);
                var itemNonAutoXMarginSum = RectF.HorizontalAxisSum(itemNonAutoMargin);

                var scrollbarSize = new Size<float>(
                    item.overflow.Y == Overflow.Scroll ? item.scrollbarWidth : 0f,
                    item.overflow.X == Overflow.Scroll ? item.scrollbarWidth : 0f);

                // Determine stretch width and position for this item.
                float stretchWidth;
                Point<float> itemPos;

                if (item.isInSameBfc)
                {
                    stretchWidth = containerInnerWidth - itemNonAutoXMarginSum;
                    itemPos = PointF.ZERO;
                }
                else
                {
                    var yMarginOffset = 0f;
                    if (!isCollapsingWithFirstMarginSet || !ownMarginsCollapseWithChildren.Start)
                        yMarginOffset = activeCollapsibleMarginSet.CollapseWithMargin(itemNonAutoMargin.Top).Resolve();

                    stretchWidth = containerInnerWidth - itemNonAutoXMarginSum;
                    itemPos = new Point<float>(
                        resolvedContentBoxInset.Left,
                        committedYOffset + yMarginOffset);
                }

                // Build known_dimensions for child layout.
                Size<float?> knownDimensions;
                if (item.isTable)
                {
                    knownDimensions = SizeF.NONE;
                }
                else
                {
                    var knownWidth = item.size.Width.HasValue
                        ? item.size.Width.Value.MaybeClamp(item.minSize.Width, item.maxSize.Width)
                        : (float?)stretchWidth.MaybeClamp(item.minSize.Width, item.maxSize.Width);
                    knownDimensions = new Size<float?>(knownWidth, item.size.Height)
                        .MaybeClamp(item.minSize, item.maxSize);
                }

                var inputs = new LayoutInput
                {
                    RunMode = RunMode.PerformLayout,
                    SizingMode = SizingMode.InherentSize,
                    Axis = RequestedAxis.Both,
                    KnownDimensions = knownDimensions,
                    ParentSize = parentSize,
                    availableSpace = availableSpace.MapWidth(_ => AvailableSpace.Definite(stretchWidth)),
                    VerticalMarginsAreCollapsible = item.isInSameBfc ? LineHelpers.TRUE : LineHelpers.FALSE,
                };

                // Lay out the child.
                LayoutOutput itemLayout;
                if (item.isInSameBfc)
                {
                    // Recurse through block algorithm with a sub-context.
                    var subCtx = new BlockContext(isRoot: false);
                    itemLayout = BlockCompute.Compute(tree, item.nodeId, inputs, subCtx);
                }
                else
                {
                    itemLayout = tree.PerformLayout(item.nodeId, inputs);
                }

                var finalSize = itemLayout.Size;

                var topMarginSet = itemLayout.TopMargin.CollapseWithMargin(itemMargin.Top ?? 0f);
                var bottomMarginSet = itemLayout.BottomMargin.CollapseWithMargin(itemMargin.Bottom ?? 0f);

                // Expand auto x-margins.
                var freeXSpace = MathF.Max(0f, stretchWidth - finalSize.Width);
                var autoMarginCount = (byte)((itemMargin.Left == null ? 1 : 0)
                                             + (itemMargin.Right == null ? 1 : 0));
                var autoMarginSize = autoMarginCount > 0 ? freeXSpace / autoMarginCount : 0f;

                var yMarginOffsetForSameBfc = 0f;
                if (item.isInSameBfc
                    && (!isCollapsingWithFirstMarginSet || !ownMarginsCollapseWithChildren.Start))
                {
                    yMarginOffsetForSameBfc =
                        activeCollapsibleMarginSet.CollapseWithMargin(
                            itemNonAutoMargin.Top + (itemMargin.Top ?? 0f)).Resolve();
                }

                // Use the correct y-margin-offset for same-BFC items.
                var effectiveYMarginOffset = item.isInSameBfc ? yMarginOffsetForSameBfc : 0f;

                var resolvedMargin = new Rect<float>(
                    left: itemMargin.Left ?? autoMarginSize,
                    right: itemMargin.Right ?? autoMarginSize,
                    top: topMarginSet.Resolve(),
                    bottom: bottomMarginSet.Resolve());

                // Resolve inset.
                var insetLeft = item.inset.Left.MaybeResolve((float?)containerInnerWidth) ?? 0f;
                var insetRight = item.inset.Right.MaybeResolve((float?)containerInnerWidth) ?? 0f;
                var insetTop = item.inset.Top.MaybeResolve((float?)0f) ?? 0f;
                var insetBottom = item.inset.Bottom.MaybeResolve((float?)0f) ?? 0f;
                var insetOffset = new Point<float>(
                    x: direction.IsRtl()
                        ? (item.inset.Right.MaybeResolve((float?)containerInnerWidth).HasValue
                            ? -insetRight
                            : insetLeft)
                        : (item.inset.Left.MaybeResolve((float?)containerInnerWidth) ?? -insetRight),
                    y: insetTop - insetBottom != 0f ? insetTop : 0f);

                // Compute location.
                Point<float> location;
                if (item.isInSameBfc)
                {
                    var locX = direction == Direction.Ltr
                        ? resolvedContentBoxInset.Left + insetOffset.X + resolvedMargin.Left
                        : containerOuterWidth - resolvedContentBoxInset.Right - finalSize.Width
                        - resolvedMargin.Right + insetOffset.X;
                    location = new Point<float>(locX,
                        committedYOffset + effectiveYMarginOffset + insetOffset.Y);
                }
                else
                {
                    var locX = direction == Direction.Ltr
                        ? itemPos.X + resolvedMargin.Left + insetOffset.X
                        : itemPos.X - finalSize.Width - resolvedMargin.Right + insetOffset.X;
                    location = new Point<float>(locX, itemPos.Y + insetOffset.Y);
                }

                // Apply text-align.
                var itemOuterWidth = itemLayout.Size.Width + resolvedMargin.Left + resolvedMargin.Right;
                if (itemOuterWidth < containerInnerWidth)
                {
                    var freeX = containerInnerWidth - itemOuterWidth;
                    switch (textAlign)
                    {
                        case TextAlign.LegacyLeft:
                            if (direction == Direction.Rtl) location.X -= freeX;
                            break;
                        case TextAlign.LegacyRight:
                            if (direction == Direction.Ltr) location.X += freeX;
                            break;
                        case TextAlign.LegacyCenter:
                            location.X += direction == Direction.Ltr ? freeX / 2f : -freeX / 2f;
                            break;
                        // TextAlign.Auto: do nothing.
                    }
                }

                // Set child layout.
                var childLayout = new Layout
                {
                    Order = item.order,
                    Location = location,
                    Size = itemLayout.Size,
                    ContentSize = itemLayout.ContentSize,
                    ScrollbarSize = scrollbarSize,
                    Padding = item.padding,
                    Border = item.border,
                    Margin = resolvedMargin,
                };
                tree.SetNodeLayout(item.nodeId, childLayout);

                // Accumulate inflow content size.
                var cbLeft = location.X - resolvedContentBoxInset.Left;
                var cbTop = location.Y - resolvedContentBoxInset.Top;
                inflowContentSize = inflowContentSize.F32Max(ComputeContentSizeContribution(
                    new Point<float>(cbLeft, cbTop), finalSize, itemLayout.ContentSize, item.overflow));

                // Update first-child top margin set.
                if (isCollapsingWithFirstMarginSet)
                {
                    if (item.canBeCollapsedThrough)
                    {
                        firstChildTopMarginSet = firstChildTopMarginSet
                            .CollapseWithSet(topMarginSet)
                            .CollapseWithSet(bottomMarginSet);
                    }
                    else
                    {
                        firstChildTopMarginSet = firstChildTopMarginSet.CollapseWithSet(topMarginSet);
                        isCollapsingWithFirstMarginSet = false;
                    }
                }

                // Write back and advance y.
                item.computedSize = finalSize;
                item.canBeCollapsedThrough = itemLayout.MarginsCanCollapseThrough;
                items[idx] = item;

                if (item.canBeCollapsedThrough)
                {
                    activeCollapsibleMarginSet = activeCollapsibleMarginSet
                        .CollapseWithSet(topMarginSet)
                        .CollapseWithSet(bottomMarginSet);
                    yOffsetForAbsolute = committedYOffset + itemLayout.Size.Height + effectiveYMarginOffset;
                }
                else
                {
                    committedYOffset = location.Y - insetOffset.Y + itemLayout.Size.Height;
                    activeCollapsibleMarginSet = bottomMarginSet;
                    yOffsetForAbsolute = committedYOffset + activeCollapsibleMarginSet.Resolve();
                }
            }

            lastChildBottomMarginSet = activeCollapsibleMarginSet;
            var bottomYMarginOffset = ownMarginsCollapseWithChildren.End
                ? 0f
                : lastChildBottomMarginSet.Resolve();

            committedYOffset += resolvedContentBoxInset.Bottom + bottomYMarginOffset;
            intrinsicOuterHeight = MathF.Max(0f, committedYOffset);
        }

        // ── Absolute children ─────────────────────────────────────────────────

        private static Size<float> PerformAbsoluteLayoutOnAbsoluteChildren(
            TaffyTree tree, List<BlockItem> items,
            Size<float> areaSize, Point<float> areaOffset, Direction direction)
        {
            var areaWidth = areaSize.Width;
            var areaHeight = areaSize.Height;
            var contentSize = SizeF.ZERO;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.position != Position.Absolute)
                    continue;

                var childStyle = tree.GetStyle(item.nodeId);
                if (childStyle.boxGenerationMode == BoxGenerationMode.None
                    || childStyle.position != Position.Absolute)
                    continue;

                var aspectRatio = childStyle.aspectRatio;
                var margin = childStyle.margin.MaybeResolve((float?)areaWidth);
                var padding = childStyle.padding.ResolveOrZero((float?)areaWidth);
                var border = childStyle.border.ResolveOrZero((float?)areaWidth);
                var pbSum = RectF.SumAxes(RectF.Add(padding, border));
                var bsAdj = childStyle.boxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

                var left = childStyle.inset.Left.MaybeResolve((float?)areaWidth);
                var right = childStyle.inset.Right.MaybeResolve((float?)areaWidth);
                var top = childStyle.inset.Top.MaybeResolve((float?)areaHeight);
                var bottom = childStyle.inset.Bottom.MaybeResolve((float?)areaHeight);

                var styleSize = childStyle.size.MaybeResolve(areaSize.Map(v => (float?)v))
                    .MaybeApplyAspectRatio(aspectRatio).MaybeAdd(bsAdj);
                var minSize = childStyle.minSize.MaybeResolve(areaSize.Map(v => (float?)v))
                    .MaybeApplyAspectRatio(aspectRatio).MaybeAdd(bsAdj)
                    .MaybeMax(pbSum);
                var maxSize = childStyle.maxSize.MaybeResolve(areaSize.Map(v => (float?)v))
                    .MaybeApplyAspectRatio(aspectRatio).MaybeAdd(bsAdj);

                var knownDimensions = styleSize.MaybeClamp(minSize, maxSize);

                // Fill in width from left+right inset.
                if (!knownDimensions.Width.HasValue && left.HasValue && right.HasValue)
                {
                    var raw = areaWidth
                        .MaybeSub(margin.Left).MaybeSub(margin.Right) - left.Value - right.Value;
                    knownDimensions.Width = MathF.Max(raw, 0f);
                    knownDimensions = knownDimensions.MaybeApplyAspectRatio(aspectRatio).MaybeClamp(minSize, maxSize);
                }

                // Fill in height from top+bottom inset.
                if (!knownDimensions.Height.HasValue && top.HasValue && bottom.HasValue)
                {
                    var raw = areaHeight
                        .MaybeSub(margin.Top).MaybeSub(margin.Bottom) - top.Value - bottom.Value;
                    knownDimensions.Height = MathF.Max(raw, 0f);
                    knownDimensions = knownDimensions.MaybeApplyAspectRatio(aspectRatio).MaybeClamp(minSize, maxSize);
                }

                // Measure child size.
                var measureInput = new LayoutInput
                {
                    RunMode = RunMode.ComputeSize,
                    SizingMode = SizingMode.ContentSize,
                    Axis = RequestedAxis.Both,
                    KnownDimensions = knownDimensions,
                    ParentSize = areaSize.Map(v => (float?)v),
                    availableSpace = new Size<AvailableSpace>(
                        AvailableSpace.Definite(areaWidth.MaybeClamp(minSize.Width, maxSize.Width)),
                        AvailableSpace.Definite(areaHeight.MaybeClamp(minSize.Height, maxSize.Height))),
                    VerticalMarginsAreCollapsible = LineHelpers.FALSE,
                };
                var measuredOutput = tree.PerformLayout(item.nodeId, measureInput);
                var measuredSize = measuredOutput.Size;

                var finalSize = SizeF.UnwrapOr(knownDimensions, measuredSize).MaybeClamp(minSize, maxSize);

                // Full layout with known final size.
                var layoutInput = measureInput;
                layoutInput.RunMode = RunMode.PerformLayout;
                layoutInput.KnownDimensions = finalSize.Map(v => (float?)v);
                var layoutOutput = tree.PerformLayout(item.nodeId, layoutInput);

                // Resolve auto margins.
                var nonAutoMargin = new Rect<float>(
                    left: left.HasValue ? margin.Left ?? 0f : 0f,
                    right: right.HasValue ? margin.Right ?? 0f : 0f,
                    top: top.HasValue ? margin.Top ?? 0f : 0f,
                    bottom: bottom.HasValue ? margin.Bottom ?? 0f : 0f);

                var freeW = right.HasValue
                    ? areaSize.Width - right.Value - (left ?? 0f) - finalSize.Width - nonAutoMargin.Left -
                      nonAutoMargin.Right
                    : finalSize.Width;
                var freeH = bottom.HasValue
                    ? areaSize.Height - bottom.Value - (top ?? 0f) - finalSize.Height - nonAutoMargin.Top -
                      nonAutoMargin.Bottom
                    : finalSize.Height;

                var autoW = (byte)((margin.Left == null ? 1 : 0) + (margin.Right == null ? 1 : 0));
                var autoH = (byte)((margin.Top == null ? 1 : 0) + (margin.Bottom == null ? 1 : 0));
                var autoMarginW = autoW > 0 ? freeW / autoW : 0f;
                var autoMarginH = autoH > 0 ? freeH / autoH : 0f;

                var resolvedMargin = new Rect<float>(
                    left: margin.Left ?? autoMarginW,
                    right: margin.Right ?? autoMarginW,
                    top: margin.Top ?? autoMarginH,
                    bottom: margin.Bottom ?? autoMarginH);

                // Compute x location.
                float xOffset;
                if (left.HasValue && right.HasValue)
                    xOffset = direction.IsRtl()
                        ? areaSize.Width - finalSize.Width - right.Value - resolvedMargin.Right
                        : left.Value + resolvedMargin.Left;
                else if (left.HasValue)
                    xOffset = left.Value + resolvedMargin.Left;
                else if (right.HasValue)
                    xOffset = areaSize.Width - finalSize.Width - right.Value - resolvedMargin.Right;
                else
                    xOffset = direction.IsRtl()
                        ? item.staticPosition.X - finalSize.Width - resolvedMargin.Right - areaOffset.X
                        : item.staticPosition.X + resolvedMargin.Left - areaOffset.X;

                var rawY = top.HasValue
                    ? (float?)(top.Value + resolvedMargin.Top)
                    : bottom.HasValue
                        ? (float?)(areaSize.Height - finalSize.Height - bottom.Value - resolvedMargin.Bottom)
                        : null;

                var yLoc = rawY.HasValue
                    ? rawY.Value + areaOffset.Y
                    : item.staticPosition.Y + resolvedMargin.Top;

                var location = new Point<float>(xOffset + areaOffset.X, yLoc);

                var scrollbarSize = new Size<float>(
                    item.overflow.Y == Overflow.Scroll ? item.scrollbarWidth : 0f,
                    item.overflow.X == Overflow.Scroll ? item.scrollbarWidth : 0f);

                tree.SetNodeLayout(item.nodeId, new Layout
                {
                    Order = item.order,
                    Location = location,
                    Size = finalSize,
                    ContentSize = layoutOutput.ContentSize,
                    ScrollbarSize = scrollbarSize,
                    Padding = padding,
                    Border = border,
                    Margin = resolvedMargin,
                });

                var relLoc = new Point<float>(location.X - areaOffset.X, location.Y - areaOffset.Y);
                contentSize = contentSize.F32Max(ComputeContentSizeContribution(
                    relLoc, finalSize, layoutOutput.ContentSize, item.overflow));
            }

            return contentSize;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static float? MinMaxDefinite(float? min, float? max)
        {
            if (min.HasValue && max.HasValue && max.Value <= min.Value) return min;
            return null;
        }

        /// <summary>
        /// Computes the visible content-size contribution of a child given its location,
        /// border-box size, own content-size, and overflow settings.
        /// Mirrors taffy's <c>compute_content_size_contribution</c>.
        /// </summary>
        private static Size<float> ComputeContentSizeContribution(
            Point<float> location, Size<float> size, Size<float> contentSize, Point<Overflow> overflow)
        {
            var width = overflow.X.IsScrollContainer() ? size.Width : MathF.Max(size.Width, contentSize.Width);
            var height = overflow.Y.IsScrollContainer() ? size.Height : MathF.Max(size.Height, contentSize.Height);
            return new Size<float>(location.X + width, location.Y + height);
        }
    }

    // ── Small helpers on Rect<float?> ────────────────────────────────────────

    internal static class RectFloatNullableExt
    {
        public static float HorizontalAxisSum(this Rect<float?> r) =>
            (r.Left ?? 0f) + (r.Right ?? 0f);

        public static Rect<float> map(this Rect<float?> r, Func<float?, float> f) =>
            new Rect<float>(f(r.Left), f(r.Right), f(r.Top), f(r.Bottom));
    }

    // ── Small helpers on Size<float?> ────────────────────────────────────────

    internal static class SizeFloatNullableExt
    {
        public static Size<float?> MaybeApplyAspectRatio(this Size<float?> self, float? aspectRatio) =>
            SizeF.MaybeApplyAspectRatio(self, aspectRatio);

        public static Size<float> MaybeClamp(this Size<float> self, Size<float?> min, Size<float?> max) =>
            new Size<float>(self.Width.MaybeClamp(min.Width, max.Width),
                self.Height.MaybeClamp(min.Height, max.Height));
    }
}