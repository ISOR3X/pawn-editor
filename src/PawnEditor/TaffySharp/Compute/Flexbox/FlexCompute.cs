// Port of taffy/src/compute/flexbox.rs
// Full CSS Flexbox layout algorithm.

using System;
using System.Collections.Generic;

namespace PawnEditor.TaffySharp
{
    // ── FlexLine ──────────────────────────────────────────────────────────────

    /// <summary>A single flex line: a contiguous slice of <see cref="FlexItem"/> entries.</summary>
    internal struct FlexLine
    {
        /// <summary>Shared item storage (same array for all lines).</summary>
        public FlexItem[] Items;
        /// <summary>Index of the first item in <see cref="Items"/> belonging to this line.</summary>
        public int Start;
        /// <summary>Number of items in this line.</summary>
        public int Count;
        /// <summary>Used cross size of this line.</summary>
        public float CrossSize;
        /// <summary>Cross-axis offset assigned by align-content.</summary>
        public float OffsetCross;
    }

    // ── AlgoConstants ─────────────────────────────────────────────────────────

    /// <summary>Values that are constant for one ComputeFlexboxLayout call.</summary>
    internal struct AlgoConstants
    {
        public FlexDirection dir;
        public Direction layoutDirection;
        public bool isRow;
        public bool isColumn;
        public bool isWrap;
        public bool isWrapReverse;

        public Size<float?> minSize;
        public Size<float?> maxSize;
        public Rect<float> margin;
        public Rect<float> border;
        /// <summary>padding + border + scrollbar gutter (inset to the content box).</summary>
        public Rect<float> contentBoxInset;
        /// <summary>Space reserved for scrollbars.</summary>
        public Point<float> scrollbarGutter;
        public Size<float> gap;

        public AlignItems alignItems;
        public AlignContent alignContent;
        public AlignContent? justifyContent;

        public Size<float?> nodeOuterSize;
        public Size<float?> nodeInnerSize;

        public Size<float> containerSize;
        public Size<float> innerContainerSize;
    }

    // ── FlexCompute ───────────────────────────────────────────────────────────

    /// <summary>CSS Flexbox layout algorithm — port of <c>taffy/src/compute/flexbox.rs</c>.</summary>
    internal static class FlexCompute
    {
        // ── Public entry point ────────────────────────────────────────────────

        public static LayoutOutput Compute(TaffyTree tree, NodeId node, LayoutInput inputs)
        {
            var style = tree.GetNodeData(node).Style;

            var parentWidth = inputs.ParentSize.Width;
            var padding = style.padding.ResolveOrZero(parentWidth);
            var border = style.border.ResolveOrZero(parentWidth);
            var pbSum = SizeF.Add(RectF.SumAxes(padding), RectF.SumAxes(border));
            var boxSizingAdj = style.boxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

            var ar = style.aspectRatio;
            var minSize = SizeF.MaybeApplyAspectRatio(
                style.minSize.MaybeResolve(inputs.ParentSize).MaybeAdd(boxSizingAdj), ar);
            var maxSize = SizeF.MaybeApplyAspectRatio(
                style.maxSize.MaybeResolve(inputs.ParentSize).MaybeAdd(boxSizingAdj), ar);

            var clampedStyleSize = inputs.SizingMode == SizingMode.InherentSize
                ? SizeF.MaybeApplyAspectRatio(
                    style.size.MaybeResolve(inputs.ParentSize).MaybeAdd(boxSizingAdj), ar)
                    .MaybeClamp(minSize, maxSize)
                : SizeF.NONE;

            var minMaxDefiniteSize = new Size<float?>(
                (minSize.Width.HasValue && maxSize.Width.HasValue && maxSize.Width <= minSize.Width)
                    ? minSize.Width : null,
                (minSize.Height.HasValue && maxSize.Height.HasValue && maxSize.Height <= minSize.Height)
                    ? minSize.Height : null);

            var styledKnown = inputs.KnownDimensions.Or(
                minMaxDefiniteSize.Or(clampedStyleSize).MaybeMax(pbSum));

            if (inputs.RunMode == RunMode.ComputeSize)
            {
                if (styledKnown.Width.HasValue && styledKnown.Height.HasValue)
                    return LayoutOutput.FromOuterSize(
                        new Size<float>(styledKnown.Width!.Value, styledKnown.Height!.Value));
            }

            return ComputePreliminary(tree, node,
                new LayoutInput
                {
                    KnownDimensions = styledKnown,
                    ParentSize = inputs.ParentSize,
                    availableSpace = inputs.availableSpace,
                    RunMode = inputs.RunMode,
                    SizingMode = inputs.SizingMode,
                    Axis = inputs.Axis,
                    VerticalMarginsAreCollapsible = inputs.VerticalMarginsAreCollapsible,
                });
        }

        // ── compute_preliminary ───────────────────────────────────────────────

        private static LayoutOutput ComputePreliminary(TaffyTree tree, NodeId node, LayoutInput inputs)
        {
            var known = inputs.KnownDimensions;
            var parentSize = inputs.ParentSize;
            var outerAvailable = inputs.availableSpace;

            var style = tree.GetNodeData(node).Style;
            var constants = ComputeConstants(style, known, parentSize);

            // 1. Generate anonymous flex items
            var flexItems = GenerateFlexItems(tree, node, ref constants);

            // 2. Determine available space
            var available = DetermineAvailableSpace(known, outerAvailable, ref constants);

            // 3. Flex base sizes and hypothetical main sizes
            DetermineFlexBaseSize(tree, ref constants, available, flexItems);

            // 5. Collect into flex lines
            var flexLines = CollectFlexLines(ref constants, available, flexItems);

            // 4. Determine container main size (or use known inner size)
            if (constants.nodeInnerSize.Main(constants.dir).HasValue)
            {
                var innerMain = constants.nodeInnerSize.Main(constants.dir)!.Value;
                var outerMain = innerMain + RectF.MainAxisSum(constants.contentBoxInset, constants.dir);
                constants.innerContainerSize.SetMain(constants.dir, innerMain);
                constants.containerSize.SetMain(constants.dir, outerMain);
            }
            else
            {
                DetermineContainerMainSize(tree, available, flexLines, ref constants);
                constants.nodeInnerSize.SetMain(constants.dir, constants.innerContainerSize.Main(constants.dir));
                constants.nodeOuterSize.SetMain(constants.dir, constants.containerSize.Main(constants.dir));

                // Re-resolve gap for main axis now that main size is known
                float? innerMainForGap = constants.innerContainerSize.Main(constants.dir);
                var newGap = style.gap.Main(constants.dir).MaybeResolve(innerMainForGap) ?? 0f;
                constants.gap.SetMain(constants.dir, newGap);
            }

            // 6. Resolve flexible lengths
            for (var li = 0; li < flexLines.Count; li++)
                ResolveFlexibleLengths(flexLines, li, ref constants);

            // 7. Hypothetical cross sizes
            for (var li = 0; li < flexLines.Count; li++)
                DetermineHypotheticalCrossSize(tree, flexLines, li, ref constants, available);

            // Child baselines
            CalculateChildrenBaseLines(tree, known, available, flexLines, ref constants);

            // 8. Cross sizes of flex lines
            CalculateCrossSize(flexLines, known, ref constants);

            // 9. align-content: stretch
            HandleAlignContentStretch(flexLines, known, ref constants);

            // 11. Used cross size of each item
            DetermineUsedCrossSize(tree, flexLines, ref constants);

            // 12. Distribute free space
            DistributeRemainingFreeSpace(flexLines, ref constants);

            // 13 & 14. Cross-axis auto margins and alignment
            ResolveCrossAxisAutoMargins(flexLines, ref constants);

            // 15. Container cross size
            var totalLineCrossSize = DetermineContainerCrossSize(flexLines, known, ref constants);

            if (inputs.RunMode == RunMode.ComputeSize)
                return LayoutOutput.FromOuterSize(constants.containerSize);

            // 16. Align flex lines
            AlignFlexLinesPerAlignContent(flexLines, ref constants, totalLineCrossSize);

            // Final layout pass
            var inflowContentSize = FinalLayoutPass(tree, flexLines, ref constants);

            // Absolute children
            var absoluteContentSize = PerformAbsoluteLayout(tree, node, ref constants);

            // Hidden children (display:none)
            var childCount = tree.ChildCount(node);
            for (var oi = 0; oi < childCount; oi++)
            {
                var child = tree.ChildAt(node, oi);
                if (tree.GetNodeData(child).Style.boxGenerationMode == BoxGenerationMode.None)
                {
                    tree.SetNodeLayout(child, Layout.WithOrder((uint)oi));
                    PerformChildLayout(tree, child, SizeF.NONE, SizeF.NONE,
                        new Size<AvailableSpace>(AvailableSpace.MaxContent, AvailableSpace.MaxContent),
                        SizingMode.InherentSize);
                }
            }

            // Container baseline
            float? firstVerticalBaseline = null;
            if (flexLines.Count > 0)
            {
                var firstLine = flexLines[0];
                var found = -1;
                for (var i = firstLine.Start; i < firstLine.Start + firstLine.Count; i++)
                {
                    if (constants.isColumn || firstLine.Items[i].alignSelf == AlignItems.Baseline)
                    {
                        found = i; break;
                    }
                }
                if (found < 0 && firstLine.Count > 0) found = firstLine.Start;
                if (found >= 0)
                {
                    var child = firstLine.Items[found];
                    var offV = constants.isRow ? child.offsetCross : child.offsetMain;
                    firstVerticalBaseline = offV + child.baseline;
                }
            }

            return LayoutOutput.FromSizesAndBaselines(
                constants.containerSize,
                inflowContentSize.F32Max(absoluteContentSize),
                new Point<float?>(null, firstVerticalBaseline));
        }

        // ── compute_constants ─────────────────────────────────────────────────

        private static AlgoConstants ComputeConstants(Style style, Size<float?> known, Size<float?> parentSize)
        {
            var dir = style.flexDirection;
            var isRow = dir.IsRow();
            var isColumn = dir.IsColumn();
            var isWrap = style.flexWrap == FlexWrap.Wrap || style.flexWrap == FlexWrap.WrapReverse;
            var isWrapReverse = style.flexWrap == FlexWrap.WrapReverse;

            var ar = style.aspectRatio;
            var parentWidth = parentSize.Width;

            var margin = style.margin.ResolveOrZero(parentWidth);
            var padding = style.padding.ResolveOrZero(parentWidth);
            var border = style.border.ResolveOrZero(parentWidth);
            var pbSum = SizeF.Add(RectF.SumAxes(padding), RectF.SumAxes(border));
            var boxSizingAdj = style.boxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

            // Scrollbar gutters — transposed (vertical scroll → horizontal space, etc.)
            var scrollbarW = style.scrollbarWidth;
            var scrollbarGutter = new Point<float>(
                style.overflow.Y.IsScrollContainer() ? scrollbarW : 0f, // x-gutter from y-overflow
                style.overflow.X.IsScrollContainer() ? scrollbarW : 0f  // y-gutter from x-overflow
            );

            var contentBoxInset = RectF.Add(padding, border);
            contentBoxInset.Bottom += scrollbarGutter.Y;
            if (style.direction == Direction.Ltr)
                contentBoxInset.Right += scrollbarGutter.X;
            else
                contentBoxInset.Left += scrollbarGutter.X;

            var nodeOuterSize = known;
            var nodeInnerSize = nodeOuterSize.MaybeSub(RectF.SumAxes(contentBoxInset));

            var gapStyle = style.gap;
            var resolvedGap = new Size<float>(
                gapStyle.Width.MaybeResolve(nodeInnerSize.Width) ?? 0f,
                gapStyle.Height.MaybeResolve(nodeInnerSize.Height) ?? 0f
            );

            return new AlgoConstants
            {
                dir = dir,
                layoutDirection = style.direction,
                isRow = isRow,
                isColumn = isColumn,
                isWrap = isWrap,
                isWrapReverse = isWrapReverse,
                minSize = SizeF.MaybeApplyAspectRatio(
                    style.minSize.MaybeResolve(parentSize).MaybeAdd(boxSizingAdj), ar),
                maxSize = SizeF.MaybeApplyAspectRatio(
                    style.maxSize.MaybeResolve(parentSize).MaybeAdd(boxSizingAdj), ar),
                margin = margin,
                border = border,
                contentBoxInset = contentBoxInset,
                scrollbarGutter = scrollbarGutter,
                gap = resolvedGap,
                alignItems = style.alignItems ?? AlignItems.Stretch,
                alignContent = style.alignContent ?? AlignContent.Stretch,
                justifyContent = style.justifyContent,
                nodeOuterSize = nodeOuterSize,
                nodeInnerSize = nodeInnerSize,
                containerSize = SizeF.ZERO,
                innerContainerSize = SizeF.ZERO,
            };
        }

        // ── generate_anonymous_flex_items ─────────────────────────────────────

        private static FlexItem[] GenerateFlexItems(TaffyTree tree, NodeId node, ref AlgoConstants c)
        {
            var childCount = tree.ChildCount(node);
            var result = new List<FlexItem>(childCount);

            for (var i = 0; i < childCount; i++)
            {
                var childId = tree.ChildAt(node, i);
                var cs = tree.GetNodeData(childId).Style;

                if (cs.position == Position.Absolute) continue;
                if (cs.boxGenerationMode == BoxGenerationMode.None) continue;

                var ar = cs.aspectRatio;
                var nodeInnerWidth = c.nodeInnerSize.Width;
                var padding = cs.padding.ResolveOrZero(nodeInnerWidth);
                var border = cs.border.ResolveOrZero(nodeInnerWidth);
                var pbSumSize = RectF.SumAxes(RectF.Add(padding, border));
                var boxAdj = cs.boxSizing == BoxSizing.ContentBox ? pbSumSize : SizeF.ZERO;

                var item = new FlexItem
                {
                    nodeId = childId,
                    order = (uint)i,
                    size = SizeF.MaybeApplyAspectRatio(
                        cs.size.MaybeResolve(c.nodeInnerSize).MaybeAdd(boxAdj), ar),
                    minSize = SizeF.MaybeApplyAspectRatio(
                        cs.minSize.MaybeResolve(c.nodeInnerSize).MaybeAdd(boxAdj), ar),
                    maxSize = SizeF.MaybeApplyAspectRatio(
                        cs.maxSize.MaybeResolve(c.nodeInnerSize).MaybeAdd(boxAdj), ar),
                    inset = cs.inset.ZipSize(c.nodeInnerSize, (p, s) => p.MaybeResolve(s)),
                    margin = cs.margin.ResolveOrZero(nodeInnerWidth),
                    marginIsAuto = cs.margin.Map(m => m.IsAuto()),
                    padding = padding,
                    border = border,
                    alignSelf = cs.alignSelf ?? c.alignItems,
                    overflow = cs.overflow,
                    scrollbarWidth = cs.scrollbarWidth,
                    flexGrow = cs.flexGrow,
                    flexShrink = cs.flexShrink,
                    flexBasis = 0f,
                    innerFlexBasis = 0f,
                    violation = 0f,
                    frozen = false,
                    resolvedMinimumMainSize = 0f,
                    hypotheticalInnerSize = SizeF.ZERO,
                    hypotheticalOuterSize = SizeF.ZERO,
                    targetSize = SizeF.ZERO,
                    outerTargetSize = SizeF.ZERO,
                    contentFlexFraction = 0f,
                    baseline = 0f,
                    offsetMain = 0f,
                    offsetCross = 0f,
                };
                result.Add(item);
            }

            return result.ToArray();
        }

        // ── determine_available_space ─────────────────────────────────────────

        private static Size<AvailableSpace> DetermineAvailableSpace(
            Size<float?> known, Size<AvailableSpace> outer, ref AlgoConstants c)
        {
            var width = known.Width.HasValue
                ? AvailableSpace.Definite(known.Width!.Value - RectF.HorizontalAxisSum(c.contentBoxInset))
                : outer.Width
                    .MaybeSub(RectF.HorizontalAxisSum(c.margin))
                    .MaybeSub(RectF.HorizontalAxisSum(c.contentBoxInset));

            var height = known.Height.HasValue
                ? AvailableSpace.Definite(known.Height!.Value - RectF.VerticalAxisSum(c.contentBoxInset))
                : outer.Height
                    .MaybeSub(RectF.VerticalAxisSum(c.margin))
                    .MaybeSub(RectF.VerticalAxisSum(c.contentBoxInset));

            return new Size<AvailableSpace>(width, height);
        }

        // ── determine_flex_base_size ──────────────────────────────────────────

        private static void DetermineFlexBaseSize(
            TaffyTree tree, ref AlgoConstants c, Size<AvailableSpace> available, FlexItem[] items)
        {
            var dir = c.dir;

            for (var idx = 0; idx < items.Length; idx++)
            {
                ref var child = ref items[idx];
                var cs = tree.GetNodeData(child.nodeId).Style;

                var crossAxisParentSize = c.nodeInnerSize.Cross(dir);
                var childParentSize = dir.IsRow()
                    ? new Size<float?>(null, crossAxisParentSize)
                    : new Size<float?>(crossAxisParentSize, null);

                var crossAxisMarginSum = RectF.CrossAxisSum(c.margin, dir);
                var childMinCross = child.minSize.Cross(dir).MaybeAdd(crossAxisMarginSum);
                var childMaxCross = child.maxSize.Cross(dir).MaybeAdd(crossAxisMarginSum);

                AvailableSpace crossAvailable;
                var rawCrossAS = available.Cross(dir);
                if (rawCrossAS.IsDefinite)
                {
                    var val = crossAxisParentSize ?? rawCrossAS.Unwrap();
                    crossAvailable = AvailableSpace.Definite(val.MaybeClamp(childMinCross, childMaxCross));
                }
                else if (rawCrossAS.IsMinContent)
                {
                    crossAvailable = childMinCross.HasValue
                        ? AvailableSpace.Definite(childMinCross!.Value)
                        : AvailableSpace.MinContent;
                }
                else // MaxContent
                {
                    crossAvailable = childMaxCross.HasValue
                        ? AvailableSpace.Definite(childMaxCross!.Value)
                        : AvailableSpace.MaxContent;
                }

                // Known child dimensions (cross may be set by stretch)
                var childKnown = child.size.WithMain(dir, null);
                if (child.alignSelf == AlignItems.Stretch
                    && !child.marginIsAuto.CrossStart(dir)
                    && !child.marginIsAuto.CrossEnd(dir)
                    && !childKnown.Cross(dir).HasValue)
                {
                    var stretchCross = crossAvailable.IntoOption()
                        .MaybeSub(child.margin.CrossStart(dir) + child.margin.CrossEnd(dir));
                    // Use SetCross which returns a new Size
                    childKnown.SetCross(dir, stretchCross);
                }

                // Box-sizing adjustment for flex-basis
                var containerWidth = c.nodeInnerSize.Main(dir);
                var bsAdj = 0f;
                if (cs.boxSizing == BoxSizing.ContentBox)
                {
                    var p2 = cs.padding.ResolveOrZero(containerWidth);
                    var b2 = cs.border.ResolveOrZero(containerWidth);
                    bsAdj = RectF.MainAxisSum(RectF.Add(p2, b2), dir);
                }

                var flexBasisFromStyle = cs.flexBasis.MaybeResolve(containerWidth).MaybeAdd(bsAdj);

                // Case A/B: definite flex basis or main size
                var mainSize = child.size.Main(dir);
                var basis = flexBasisFromStyle ?? mainSize;

                if (basis.HasValue)
                {
                    child.flexBasis = basis!.Value;
                }
                else
                {
                    // Case E: measure child under max-content (or min-content)
                    var isMinContent = available.Main(dir) == AvailableSpace.MinContent;
                    var childAvail = new Size<AvailableSpace>(AvailableSpace.MaxContent, AvailableSpace.MaxContent)
                        .WithMain(dir, isMinContent ? AvailableSpace.MinContent : AvailableSpace.MaxContent)
                        .WithCross(dir, crossAvailable);

                    child.flexBasis = MeasureChildSize(tree, child.nodeId,
                        childKnown, childParentSize, childAvail,
                        SizingMode.ContentSize, dir.MainAxis());
                }

                // Floor flex-basis at padding+border sum
                var pbMainSum = RectF.MainAxisSum(child.padding, dir) + RectF.MainAxisSum(child.border, dir);
                child.flexBasis = MathF.Max(child.flexBasis, pbMainSum);

                child.innerFlexBasis = child.flexBasis
                    - RectF.MainAxisSum(child.padding, dir)
                    - RectF.MainAxisSum(child.border, dir);

                // Automatic minimum main size
                var pbAxesSums = RectF.SumAxes(RectF.Add(child.padding, child.border)).Map(v => (float?)v);

                var overflowMinSize = new Size<float?>(
                    child.overflow.X.MaybeIntoAutomaticMinSize(),
                    child.overflow.Y.MaybeIntoAutomaticMinSize());

                var styleMinMainSize = child.minSize.Or(overflowMinSize).Main(dir);

                if (!styleMinMainSize.HasValue)
                {
                    var crossForMin = new Size<AvailableSpace>(AvailableSpace.MinContent, AvailableSpace.MinContent)
                        .WithCross(dir, crossAvailable);
                    var minContentMain = MeasureChildSize(tree, child.nodeId,
                        childKnown, childParentSize, crossForMin,
                        SizingMode.ContentSize, dir.MainAxis());

                    var clamped = minContentMain
                        .MaybeMin(child.size.Main(dir))
                        .MaybeMin(child.maxSize.Main(dir));
                    child.resolvedMinimumMainSize = clamped.MaybeMax(pbAxesSums.Main(dir));
                }
                else
                {
                    child.resolvedMinimumMainSize = styleMinMainSize!.Value;
                }

                var hypoInnerMin = child.resolvedMinimumMainSize
                    .MaybeMax(pbAxesSums.Main(c.dir));
                var hypoInner = child.flexBasis.MaybeClamp(
                    (float?)hypoInnerMin, child.maxSize.Main(c.dir));
                var hypoOuter = hypoInner + RectF.MainAxisSum(child.margin, c.dir);

                child.hypotheticalInnerSize.SetMain(c.dir, hypoInner);
                child.hypotheticalOuterSize.SetMain(c.dir, hypoOuter);
            }
        }

        // ── collect_flex_lines ────────────────────────────────────────────────

        private static List<FlexLine> CollectFlexLines(
            ref AlgoConstants c, Size<AvailableSpace> available, FlexItem[] items)
        {
            if (items.Length == 0)
                return [];

            if (!c.isWrap)
            {
                return [new FlexLine { Items = items, Start = 0, Count = items.Length }];
            }

            // Determine effective available space for main axis
            AvailableSpace mainAS;
            var maxMain = c.maxSize.Main(c.dir);
            if (maxMain.HasValue)
            {
                var raw = available.Main(c.dir).IntoOption() ?? maxMain.Value;
                mainAS = AvailableSpace.Definite(raw.MaybeMax(c.minSize.Main(c.dir)));
            }
            else
            {
                mainAS = available.Main(c.dir);
            }

            if (mainAS.IsMaxContent)
            {
                return [new FlexLine { Items = items, Start = 0, Count = items.Length }];
            }

            if (mainAS.IsMinContent)
            {
                var lines = new List<FlexLine>(items.Length);
                for (var i = 0; i < items.Length; i++)
                    lines.Add(new FlexLine { Items = items, Start = i, Count = 1 });
                return lines;
            }

            // Definite: break lines when they exceed available space
            var mainAvail = mainAS.Unwrap();
            var mainGap = c.gap.Main(c.dir);
            var result = new List<FlexLine>();

            var lineStart = 0;
            while (lineStart < items.Length)
            {
                var lineLen = 0f;
                var lineEnd = lineStart;
                for (var i = lineStart; i < items.Length; i++)
                {
                    var gapContrib = (i == lineStart) ? 0f : mainGap;
                    lineLen += items[i].hypotheticalOuterSize.Main(c.dir) + gapContrib;
                    if (lineLen > mainAvail && i != lineStart)
                    {
                        lineEnd = i; goto addLine;
                    }
                }
                lineEnd = items.Length;

                addLine:
                result.Add(new FlexLine { Items = items, Start = lineStart, Count = lineEnd - lineStart });
                lineStart = lineEnd;
            }

            return result;
        }

        // ── determine_container_main_size ─────────────────────────────────────

        private static void DetermineContainerMainSize(
            TaffyTree tree, Size<AvailableSpace> available, List<FlexLine> lines, ref AlgoConstants c)
        {
            var dir = c.dir;
            var mainCBI = RectF.MainAxisSum(c.contentBoxInset, dir);

            var outerMain = c.nodeOuterSize.Main(dir) ?? ComputeOuterMain(tree, available, lines, ref c, mainCBI);

            outerMain = outerMain
                .MaybeClamp(c.minSize.Main(dir), c.maxSize.Main(dir))
                .MaybeMax(mainCBI - c.scrollbarGutter.Main(dir));

            var innerMain = MathF.Max(outerMain - mainCBI, 0f);
            c.containerSize.SetMain(dir, outerMain);
            c.innerContainerSize.SetMain(dir, innerMain);
            c.nodeInnerSize.SetMain(dir, innerMain);
        }

        private static float ComputeOuterMain(
            TaffyTree tree, Size<AvailableSpace> available, List<FlexLine> lines,
            ref AlgoConstants c, float mainCBI)
        {
            var dir = c.dir;

            switch (available.Main(dir).IsDefinite
                    ? 0 // definite
                    : available.Main(dir).IsMinContent && c.isWrap
                    ? 1 // min-content + wrap
                    : 2 // min/max-content (or min-content no-wrap)
                    )
            {
                case 0: // Definite
                {
                    var definiteMain = available.Main(dir).Unwrap();
                    var longest = 0f;
                    foreach (var line in lines)
                    {
                        var gapSum = SumAxisGaps(c.gap.Main(dir), line.Count);
                        var total = 0f;
                        for (var i = line.Start; i < line.Start + line.Count; i++)
                        {
                            ref var item = ref line.Items[i];
                            var pbSum = RectF.MainAxisSum(item.padding, dir) + RectF.MainAxisSum(item.border, dir);
                            var s = (item.flexBasis.MaybeMax(item.minSize.Main(dir))
                                     + RectF.MainAxisSum(item.margin, dir))
                                .MaybeMax(pbSum);
                            total += s;
                        }
                        var lineLen = total + gapSum;
                        if (lineLen > longest) longest = lineLen;
                    }
                    var size = longest + mainCBI;
                    return lines.Count > 1 ? MathF.Max(size, definiteMain) : size;
                }
                case 1: // MinContent + wrap
                {
                    var longest = 0f;
                    foreach (var line in lines)
                    {
                        var gapSum = SumAxisGaps(c.gap.Main(dir), line.Count);
                        var total = 0f;
                        for (var i = line.Start; i < line.Start + line.Count; i++)
                        {
                            ref var item = ref line.Items[i];
                            var pbSum = RectF.MainAxisSum(item.padding, dir) + RectF.MainAxisSum(item.border, dir);
                            var s = (item.flexBasis.MaybeMax(item.minSize.Main(dir))
                                     + RectF.MainAxisSum(item.margin, dir))
                                .MaybeMax(pbSum);
                            total += s;
                        }
                        var lineLen = total + gapSum;
                        if (lineLen > longest) longest = lineLen;
                    }
                    return longest + mainCBI;
                }
                default: // MaxContent or MinContent no-wrap
                {
                    var mainSize = 0f;
                    foreach (var line in lines)
                    {
                        for (var i = line.Start; i < line.Start + line.Count; i++)
                        {
                            ref var item = ref line.Items[i];
                            var styleMin = item.minSize.Main(dir);
                            var stylePreferred = item.size.Main(dir);
                            var styleMax = item.maxSize.Main(dir);

                            var clampBasis = ((float?)item.flexBasis).MaybeMax(stylePreferred);
                            var flexBasisMin = item.flexShrink == 0f ? clampBasis : null;
                            var flexBasisMax = item.flexGrow == 0f ? clampBasis : null;

                            var minMain = styleMin.MaybeMax(flexBasisMin).Or(flexBasisMin)
                                          ?? MathF.Max(item.resolvedMinimumMainSize,
                                              styleMin ?? item.resolvedMinimumMainSize);
                            minMain = MathF.Max(minMain, item.resolvedMinimumMainSize);
                            var maxMain = styleMax.MaybeMin(flexBasisMax).Or(flexBasisMax)
                                          ?? float.PositiveInfinity;

                            float contribution;
                            if ((stylePreferred.HasValue && maxMain <= stylePreferred!.Value)
                                || (stylePreferred.HasValue && maxMain <= minMain))
                            {
                                var pref = stylePreferred!.Value;
                                contribution = MathF.Max(MathF.Min(pref, maxMain), minMain)
                                    + RectF.MainAxisSum(item.margin, dir);
                            }
                            else if (maxMain <= minMain)
                            {
                                contribution = minMain + RectF.MainAxisSum(item.margin, dir);
                            }
                            else if (item.IsScrollContainer())
                            {
                                contribution = item.flexBasis + RectF.MainAxisSum(item.margin, dir);
                            }
                            else
                            {
                                var crossParent = c.nodeInnerSize.Cross(dir);
                                var crossMarginSum = RectF.CrossAxisSum(c.margin, dir);
                                var itemMinCross = item.minSize.Cross(dir).MaybeAdd(crossMarginSum);
                                var itemMaxCross = item.maxSize.Cross(dir).MaybeAdd(crossMarginSum);
                                var crossAS = available.Cross(dir)
                                    .MapDefiniteValue(v => crossParent ?? v)
                                    .MaybeClamp(itemMinCross, itemMaxCross);

                                var childKnown = item.size.WithMain(dir, null);
                                if (item.alignSelf == AlignItems.Stretch && !childKnown.Cross(dir).HasValue)
                                {
                                    var stretch = crossAS.IntoOption()
                                        .MaybeSub(RectF.CrossAxisSum(item.margin, dir));
                                    childKnown.SetCross(dir, stretch);
                                }

                                var childAvail = available.WithCross(dir, crossAS);
                                var contentMain = MeasureChildSize(tree, item.nodeId,
                                                      childKnown, c.nodeInnerSize, childAvail,
                                                      SizingMode.InherentSize, dir.MainAxis())
                                                  + RectF.MainAxisSum(item.margin, dir);

                                if (c.isRow)
                                    contribution = contentMain.MaybeClamp(styleMin, styleMax)
                                        .MaybeMax(mainCBI);
                                else
                                    contribution = MathF.Max(contentMain, item.flexBasis)
                                        .MaybeClamp(styleMin, styleMax)
                                        .MaybeMax(mainCBI);
                            }

                            item.contentFlexFraction = ComputeContentFlexFraction(ref item, contribution);
                        }

                        var itemSum = 0f;
                        for (var i = line.Start; i < line.Start + line.Count; i++)
                        {
                            ref var item = ref line.Items[i];
                            var ff = item.contentFlexFraction;
                            float contrib;
                            if (ff > 0f)
                                contrib = item.flexBasis + MathF.Max(1f, item.flexGrow) * ff;
                            else if (ff < 0f)
                                contrib = item.flexBasis + MathF.Max(1f, item.flexShrink)
                                    * item.innerFlexBasis * ff;
                            else
                                contrib = item.flexBasis;

                            item.outerTargetSize.SetMain(c.dir, contrib);
                            item.targetSize.SetMain(c.dir, contrib);
                            itemSum += contrib;
                        }

                        var lineGap = SumAxisGaps(c.gap.Main(c.dir), line.Count);
                        if (itemSum + lineGap > mainSize) mainSize = itemSum + lineGap;
                    }
                    return mainSize + mainCBI;
                }
            }
        }

        private static float ComputeContentFlexFraction(ref FlexItem item, float contribution)
        {
            var diff = contribution - item.flexBasis;
            if (diff > 0f)
                return diff / MathF.Max(1f, item.flexGrow);
            if (diff < 0f)
                return diff / MathF.Max(1f, item.flexShrink * item.innerFlexBasis);
            return 0f;
        }

        // ── resolve_flexible_lengths ──────────────────────────────────────────

        private static void ResolveFlexibleLengths(List<FlexLine> lines, int lineIdx, ref AlgoConstants c)
        {
            var line = lines[lineIdx];
            var dir = c.dir;

            var totalGap = SumAxisGaps(c.gap.Main(dir), line.Count);

            var totalHypoOuter = 0f;
            for (var i = line.Start; i < line.Start + line.Count; i++)
                totalHypoOuter += line.Items[i].hypotheticalOuterSize.Main(dir);

            var usedFlexFactor = totalGap + totalHypoOuter;
            var nodeInnerMain = c.nodeInnerSize.Main(dir);
            var growing = usedFlexFactor < (nodeInnerMain ?? 0f);
            var shrinking = usedFlexFactor > (nodeInnerMain ?? 0f);
            var exactlySized = !growing && !shrinking;

            // Freeze inflexible items
            for (var i = line.Start; i < line.Start + line.Count; i++)
            {
                ref var child = ref line.Items[i];
                var innerTarget = child.hypotheticalInnerSize.Main(dir);
                child.targetSize.SetMain(dir, innerTarget);

                if (exactlySized
                    || (child.flexGrow == 0f && child.flexShrink == 0f)
                    || (growing && child.flexBasis > child.hypotheticalInnerSize.Main(dir))
                    || (shrinking && child.flexBasis < child.hypotheticalInnerSize.Main(dir)))
                {
                    child.frozen = true;
                    child.outerTargetSize.SetMain(dir, innerTarget + RectF.MainAxisSum(child.margin, dir));
                }
            }

            if (exactlySized) return;

            // Initial free space
            var usedSpace = totalGap;
            for (var i = line.Start; i < line.Start + line.Count; i++)
            {
                ref var child = ref line.Items[i];
                usedSpace += child.frozen
                    ? child.outerTargetSize.Main(dir)
                    : child.flexBasis + RectF.MainAxisSum(child.margin, dir);
            }
            var initialFreeSpace = nodeInnerMain.MaybeSub(usedSpace) ?? 0f;

            // Flex loop
            while (true)
            {
                // Check all frozen
                var allFrozen = true;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                    if (!line.Items[i].frozen) { allFrozen = false; break; }
                if (allFrozen) break;

                // Recalculate used space
                var usedSpace2 = totalGap;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    ref var child = ref line.Items[i];
                    usedSpace2 += child.frozen
                        ? child.outerTargetSize.Main(dir)
                        : child.flexBasis + RectF.MainAxisSum(child.margin, dir);
                }

                float sumFlexGrow = 0f, sumFlexShrink = 0f;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    if (!line.Items[i].frozen)
                    {
                        sumFlexGrow += line.Items[i].flexGrow;
                        sumFlexShrink += line.Items[i].flexShrink;
                    }
                }

                var nodeInnerMinusUsed = nodeInnerMain.MaybeSub(usedSpace2);
                float freeSpace;
                if (growing && sumFlexGrow < 1f)
                    freeSpace = ((float?)(initialFreeSpace * sumFlexGrow - totalGap))
                        .MaybeMin(nodeInnerMinusUsed) ?? 0f;
                else if (shrinking && sumFlexShrink < 1f)
                    freeSpace = ((float?)(initialFreeSpace * sumFlexShrink - totalGap))
                        .MaybeMax(nodeInnerMinusUsed) ?? 0f;
                else
                    freeSpace = nodeInnerMinusUsed ?? (usedFlexFactor - usedSpace2);

                // Distribute free space
                if (float.IsNormal(freeSpace))
                {
                    if (growing && sumFlexGrow > 0f)
                    {
                        for (var i = line.Start; i < line.Start + line.Count; i++)
                        {
                            if (!line.Items[i].frozen)
                                line.Items[i].targetSize.SetMain(dir,
                                    line.Items[i].flexBasis
                                    + freeSpace * (line.Items[i].flexGrow / sumFlexGrow));
                        }
                    }
                    else if (shrinking && sumFlexShrink > 0f)
                    {
                        var sumScaled = 0f;
                        for (var i = line.Start; i < line.Start + line.Count; i++)
                            if (!line.Items[i].frozen)
                                sumScaled += line.Items[i].innerFlexBasis * line.Items[i].flexShrink;

                        if (sumScaled > 0f)
                        {
                            for (var i = line.Start; i < line.Start + line.Count; i++)
                            {
                                if (!line.Items[i].frozen)
                                {
                                    var scaled = line.Items[i].innerFlexBasis * line.Items[i].flexShrink;
                                    line.Items[i].targetSize.SetMain(dir,
                                        line.Items[i].flexBasis + freeSpace * (scaled / sumScaled));
                                }
                            }
                        }
                    }
                }

                // Clamp and compute violations
                var totalViolation = 0f;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    if (!line.Items[i].frozen)
                    {
                        float? resolvedMin = line.Items[i].resolvedMinimumMainSize;
                        var maxMain = line.Items[i].maxSize.Main(dir);
                        var clamped = MathF.Max(
                            line.Items[i].targetSize.Main(dir).MaybeClamp(resolvedMin, maxMain), 0f);
                        line.Items[i].violation = clamped - line.Items[i].targetSize.Main(dir);
                        line.Items[i].targetSize.SetMain(dir, clamped);
                        line.Items[i].outerTargetSize.SetMain(dir,
                            clamped + RectF.MainAxisSum(line.Items[i].margin, dir));
                        totalViolation += line.Items[i].violation;
                    }
                }

                // Freeze violating items
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    if (!line.Items[i].frozen)
                    {
                        if (totalViolation > 0f)
                            line.Items[i].frozen = line.Items[i].violation > 0f;
                        else if (totalViolation < 0f)
                            line.Items[i].frozen = line.Items[i].violation < 0f;
                        else
                            line.Items[i].frozen = true;
                    }
                }
            }
        }

        // ── determine_hypothetical_cross_size ─────────────────────────────────

        private static void DetermineHypotheticalCrossSize(
            TaffyTree tree, List<FlexLine> lines, int lineIdx,
            ref AlgoConstants c, Size<AvailableSpace> available)
        {
            var line = lines[lineIdx];
            var dir = c.dir;

            for (var i = line.Start; i < line.Start + line.Count; i++)
            {
                ref var child = ref line.Items[i];
                var pbCrossSum = RectF.CrossAxisSum(child.padding, dir)
                                 + RectF.CrossAxisSum(child.border, dir);

                float? childKnownMain = c.containerSize.Main(dir);
                var childCross = child.size.Cross(dir)
                    .MaybeClamp(child.minSize.Cross(dir), child.maxSize.Cross(dir))
                    .MaybeMax(pbCrossSum);

                var childAvailCross = available.Cross(dir)
                    .MaybeClamp(child.minSize.Cross(dir), child.maxSize.Cross(dir))
                    .MaybeMax(pbCrossSum);

                float childInnerCross;
                if (childCross.HasValue)
                {
                    childInnerCross = childCross!.Value;
                }
                else
                {
                    var knownForMeasure = dir.IsRow()
                        ? new Size<float?>((float?)child.targetSize.Width, null)
                        : new Size<float?>(null, (float?)child.targetSize.Height);
                    knownForMeasure.SetCross(dir, null);

                    var availForMeasure = dir.IsRow()
                        ? new Size<AvailableSpace>(
                            AvailableSpace.Definite(childKnownMain ?? 0f),
                            childAvailCross)
                        : new Size<AvailableSpace>(
                            childAvailCross,
                            AvailableSpace.Definite(childKnownMain ?? 0f));

                    childInnerCross = MeasureChildSize(tree, child.nodeId,
                        knownForMeasure, c.nodeInnerSize, availForMeasure,
                        SizingMode.ContentSize, dir.CrossAxis())
                        .MaybeClamp(child.minSize.Cross(dir), child.maxSize.Cross(dir))
                        .MaybeMax(pbCrossSum);
                }

                var childOuterCross = childInnerCross + RectF.CrossAxisSum(child.margin, dir);
                child.hypotheticalInnerSize.SetCross(dir, childInnerCross);
                child.hypotheticalOuterSize.SetCross(dir, childOuterCross);
            }
        }

        // ── calculate_children_base_lines ─────────────────────────────────────

        private static void CalculateChildrenBaseLines(
            TaffyTree tree, Size<float?> nodeSize, Size<AvailableSpace> available,
            List<FlexLine> lines, ref AlgoConstants c)
        {
            if (!c.isRow) return;

            foreach (var line in lines)
            {
                // Count baseline-aligned items
                var baselineCount = 0;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                    if (line.Items[i].alignSelf == AlignItems.Baseline) baselineCount++;
                if (baselineCount <= 1) continue;

                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    if (line.Items[i].alignSelf != AlignItems.Baseline) continue;
                    ref var child = ref line.Items[i];

                    var knownW = c.isRow
                        ? (float?)child.targetSize.Width
                        : (float?)child.hypotheticalInnerSize.Width;
                    var knownH = c.isRow
                        ? (float?)child.hypotheticalInnerSize.Height
                        : (float?)child.targetSize.Height;

                    var availW = c.isRow
                        ? AvailableSpace.Definite(c.containerSize.Width)
                        : available.Width.MaybeSet(nodeSize.Width);
                    var availH = c.isRow
                        ? available.Height.MaybeSet(nodeSize.Height)
                        : AvailableSpace.Definite(c.containerSize.Height);

                    var output = PerformChildLayout(tree, child.nodeId,
                        new Size<float?>(knownW, knownH),
                        c.nodeInnerSize,
                        new Size<AvailableSpace>(availW, availH),
                        SizingMode.ContentSize);

                    var baseline = output.FirstBaselines.Y ?? output.Size.Height;
                    child.baseline = baseline + child.margin.Top;
                }
            }
        }

        // ── calculate_cross_size ──────────────────────────────────────────────

        private static void CalculateCrossSize(
            List<FlexLine> lines, Size<float?> nodeSize, ref AlgoConstants c)
        {
            if (!c.isWrap && nodeSize.Cross(c.dir).HasValue)
            {
                var crossPB = RectF.CrossAxisSum(c.contentBoxInset, c.dir);
                lines[0] = new FlexLine
                {
                    Items = lines[0].Items, Start = lines[0].Start, Count = lines[0].Count,
                    OffsetCross = lines[0].OffsetCross,
                    CrossSize = nodeSize.Cross(c.dir)
                        .MaybeClamp(c.minSize.Cross(c.dir), c.maxSize.Cross(c.dir))
                        .MaybeSub(crossPB)
                        .MaybeMax(0f)
                        ?? 0f,
                };
                return;
            }

            for (var li = 0; li < lines.Count; li++)
            {
                var line = lines[li];
                var maxBaseline = 0f;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                    if (line.Items[i].baseline > maxBaseline) maxBaseline = line.Items[i].baseline;

                var maxCross = 0f;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    ref var child = ref line.Items[i];
                    float crossSz;
                    if (child.alignSelf == AlignItems.Baseline
                        && !child.marginIsAuto.CrossStart(c.dir)
                        && !child.marginIsAuto.CrossEnd(c.dir))
                    {
                        crossSz = maxBaseline - child.baseline
                            + child.hypotheticalOuterSize.Cross(c.dir);
                    }
                    else
                    {
                        crossSz = child.hypotheticalOuterSize.Cross(c.dir);
                    }
                    if (crossSz > maxCross) maxCross = crossSz;
                }
                lines[li] = new FlexLine
                {
                    Items = line.Items, Start = line.Start, Count = line.Count,
                    OffsetCross = line.OffsetCross, CrossSize = maxCross,
                };
            }

            // Single-line: clamp to min/max
            if (!c.isWrap && lines.Count > 0)
            {
                var crossPB = RectF.CrossAxisSum(c.contentBoxInset, c.dir);
                var line = lines[0];
                lines[0] = new FlexLine
                {
                    Items = line.Items, Start = line.Start, Count = line.Count,
                    OffsetCross = line.OffsetCross,
                    CrossSize = line.CrossSize.MaybeClamp(
                        c.minSize.Cross(c.dir).MaybeSub(crossPB),
                        c.maxSize.Cross(c.dir).MaybeSub(crossPB)),
                };
            }
        }

        // ── handle_align_content_stretch ──────────────────────────────────────

        private static void HandleAlignContentStretch(
            List<FlexLine> lines, Size<float?> nodeSize, ref AlgoConstants c)
        {
            if (c.alignContent != AlignContent.Stretch) return;

            var crossPB = RectF.CrossAxisSum(c.contentBoxInset, c.dir);
            var nodeInnerCross = nodeSize.Cross(c.dir)
                .Or(c.minSize.Cross(c.dir))
                .MaybeClamp(c.minSize.Cross(c.dir), c.maxSize.Cross(c.dir))
                .MaybeSub(crossPB)
                .MaybeMax(0f);
            if (!nodeInnerCross.HasValue) return;

            var totalGap = SumAxisGaps(c.gap.Cross(c.dir), lines.Count);
            var linesTotal = 0f;
            foreach (var line in lines) linesTotal += line.CrossSize;
            linesTotal += totalGap;

            if (linesTotal < nodeInnerCross!.Value)
            {
                var addition = (nodeInnerCross!.Value - linesTotal) / lines.Count;
                for (var li = 0; li < lines.Count; li++)
                {
                    var l = lines[li];
                    lines[li] = new FlexLine
                    {
                        Items = l.Items, Start = l.Start, Count = l.Count,
                        OffsetCross = l.OffsetCross, CrossSize = l.CrossSize + addition,
                    };
                }
            }
        }

        // ── determine_used_cross_size ─────────────────────────────────────────

        private static void DetermineUsedCrossSize(
            TaffyTree tree, List<FlexLine> lines, ref AlgoConstants c)
        {
            foreach (var line in lines)
            {
                var lineCross = line.CrossSize;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    ref var child = ref line.Items[i];
                    var cs = tree.GetNodeData(child.nodeId).Style;

                    var stretch = child.alignSelf == AlignItems.Stretch
                                  && !child.marginIsAuto.CrossStart(c.dir)
                                  && !child.marginIsAuto.CrossEnd(c.dir)
                                  && cs.size.Cross(c.dir).IsAuto();

                    float targetCross;
                    if (stretch)
                    {
                        var padding = cs.padding.ResolveOrZero(c.nodeInnerSize);
                        var border = cs.border.ResolveOrZero(c.nodeInnerSize);
                        var pbSumSize = RectF.SumAxes(RectF.Add(padding, border));
                        var maxCrossIgnAR = cs.maxSize.MaybeResolve(c.nodeInnerSize)
                            .MaybeAdd(cs.boxSizing == BoxSizing.ContentBox ? pbSumSize : SizeF.ZERO);

                        float? clamped = (lineCross - RectF.CrossAxisSum(child.margin, c.dir))
                            .MaybeClamp(child.minSize.Cross(c.dir), maxCrossIgnAR.Cross(c.dir));
                        targetCross = clamped ?? child.hypotheticalInnerSize.Cross(c.dir);
                    }
                    else
                    {
                        targetCross = child.hypotheticalInnerSize.Cross(c.dir);
                    }

                    child.targetSize.SetCross(c.dir, targetCross);
                    child.outerTargetSize.SetCross(c.dir,
                        targetCross + RectF.CrossAxisSum(child.margin, c.dir));
                }
            }
        }

        // ── distribute_remaining_free_space ───────────────────────────────────

        private static void DistributeRemainingFreeSpace(List<FlexLine> lines, ref AlgoConstants c)
        {
            foreach (var line in lines)
            {
                var totalGap = SumAxisGaps(c.gap.Main(c.dir), line.Count);
                var usedSpace = totalGap;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                    usedSpace += line.Items[i].outerTargetSize.Main(c.dir);

                var freeSpace = c.innerContainerSize.Main(c.dir) - usedSpace;

                var numAutoMargins = 0;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    if (line.Items[i].marginIsAuto.MainStart(c.dir)) numAutoMargins++;
                    if (line.Items[i].marginIsAuto.MainEnd(c.dir)) numAutoMargins++;
                }

                if (freeSpace > 0f && numAutoMargins > 0)
                {
                    var margin = freeSpace / numAutoMargins;
                    for (var i = line.Start; i < line.Start + line.Count; i++)
                    {
                        if (line.Items[i].marginIsAuto.MainStart(c.dir))
                        {
                            if (c.isRow) line.Items[i].margin.Left = margin;
                            else         line.Items[i].margin.Top  = margin;
                        }
                        if (line.Items[i].marginIsAuto.MainEnd(c.dir))
                        {
                            if (c.isRow) line.Items[i].margin.Right  = margin;
                            else         line.Items[i].margin.Bottom  = margin;
                        }
                    }
                }
                else
                {
                    var numItems = line.Count;
                    var layoutReverse = c.dir.IsReverse();
                    var gap = c.gap.Main(c.dir);
                    var rawJC = c.justifyContent ?? AlignContent.FlexStart;
                    var jc = Alignment.ApplyAlignmentFallback(freeSpace, numItems, rawJC, false);

                    if (layoutReverse)
                    {
                        for (var rev = 0; rev < line.Count; rev++)
                        {
                            var i = line.Start + line.Count - 1 - rev;
                            line.Items[i].offsetMain = Alignment.ComputeAlignmentOffset(
                                freeSpace, numItems, gap, jc, layoutReverse, rev == 0);
                        }
                    }
                    else
                    {
                        for (var idx = 0; idx < line.Count; idx++)
                        {
                            var i = line.Start + idx;
                            line.Items[i].offsetMain = Alignment.ComputeAlignmentOffset(
                                freeSpace, numItems, gap, jc, layoutReverse, idx == 0);
                        }
                    }
                }
            }
        }

        // ── resolve_cross_axis_auto_margins ───────────────────────────────────

        private static void ResolveCrossAxisAutoMargins(List<FlexLine> lines, ref AlgoConstants c)
        {
            foreach (var line in lines)
            {
                var lineCross = line.CrossSize;
                var maxBaseline = 0f;
                for (var i = line.Start; i < line.Start + line.Count; i++)
                    if (line.Items[i].baseline > maxBaseline) maxBaseline = line.Items[i].baseline;

                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    ref var child = ref line.Items[i];
                    var freeSpace = lineCross - child.outerTargetSize.Cross(c.dir);
                    var crossStart = child.marginIsAuto.CrossStart(c.dir);
                    var crossEnd   = child.marginIsAuto.CrossEnd(c.dir);

                    if (crossStart && crossEnd)
                    {
                        if (c.isRow) { child.margin.Top = freeSpace / 2f; child.margin.Bottom = freeSpace / 2f; }
                        else         { child.margin.Left = freeSpace / 2f; child.margin.Right  = freeSpace / 2f; }
                    }
                    else if (crossStart)
                    {
                        if (c.isRow) child.margin.Top  = freeSpace;
                        else         child.margin.Left  = freeSpace;
                    }
                    else if (crossEnd)
                    {
                        if (c.isRow) child.margin.Bottom = freeSpace;
                        else         child.margin.Right  = freeSpace;
                    }
                    else
                    {
                        child.offsetCross = AlignFlexItem(ref child, freeSpace, maxBaseline, ref c);
                    }
                }
            }
        }

        // ── align_flex_items_along_cross_axis ─────────────────────────────────

        private static float AlignFlexItem(
            ref FlexItem child, float freeSpace, float maxBaseline, ref AlgoConstants c)
        {
            var crossShouldReverse = c.isColumn && c.layoutDirection == Direction.Rtl;

            switch (child.alignSelf)
            {
                case AlignItems.Start:
                    return crossShouldReverse ? freeSpace : 0f;
                case AlignItems.FlexStart:
                    return c.isWrapReverse ^ crossShouldReverse ? freeSpace : 0f;
                case AlignItems.End:
                    return crossShouldReverse ? 0f : freeSpace;
                case AlignItems.FlexEnd:
                    return c.isWrapReverse ^ crossShouldReverse ? 0f : freeSpace;
                case AlignItems.Center:
                    return freeSpace / 2f;
                case AlignItems.Baseline:
                    if (c.isRow)
                        return maxBaseline - child.baseline;
                    else
                    {
                        var baselineColReverse = crossShouldReverse && !c.isWrap;
                        return c.isWrapReverse ^ baselineColReverse ? freeSpace : 0f;
                    }
                default: // Stretch
                    return c.isWrapReverse ^ crossShouldReverse ? freeSpace : 0f;
            }
        }

        // ── determine_container_cross_size ────────────────────────────────────

        private static float DetermineContainerCrossSize(
            List<FlexLine> lines, Size<float?> nodeSize, ref AlgoConstants c)
        {
            var totalGap = SumAxisGaps(c.gap.Cross(c.dir), lines.Count);
            var totalLineCross = 0f;
            foreach (var line in lines) totalLineCross += line.CrossSize;

            var crossPB = RectF.CrossAxisSum(c.contentBoxInset, c.dir);
            var crossScrollGutter = c.scrollbarGutter.Cross(c.dir);
            var minCross = c.minSize.Cross(c.dir);
            var maxCross = c.maxSize.Cross(c.dir);

            var outerCross = (nodeSize.Cross(c.dir)
                              ?? (totalLineCross + totalGap + crossPB))
                .MaybeClamp(minCross, maxCross)
                .MaybeMax(crossPB - crossScrollGutter);
            var innerCross = MathF.Max(outerCross - crossPB, 0f);

            c.containerSize.SetCross(c.dir, outerCross);
            c.innerContainerSize.SetCross(c.dir, innerCross);

            return totalLineCross;
        }

        // ── align_flex_lines_per_align_content ────────────────────────────────

        private static void AlignFlexLinesPerAlignContent(
            List<FlexLine> lines, ref AlgoConstants c, float totalCrossSize)
        {
            var numLines = lines.Count;
            var gap = c.gap.Cross(c.dir);
            var totalGap = SumAxisGaps(gap, numLines);
            var freeSpace = c.innerContainerSize.Cross(c.dir) - totalCrossSize - totalGap;
            var mode = Alignment.ApplyAlignmentFallback(freeSpace, numLines, c.alignContent, false);

            if (c.isWrapReverse)
            {
                for (var rev = 0; rev < numLines; rev++)
                {
                    var li = numLines - 1 - rev;
                    var l = lines[li];
                    lines[li] = new FlexLine
                    {
                        Items = l.Items, Start = l.Start, Count = l.Count, CrossSize = l.CrossSize,
                        OffsetCross = Alignment.ComputeAlignmentOffset(
                            freeSpace, numLines, gap, mode, c.isWrapReverse, rev == 0),
                    };
                }
            }
            else
            {
                for (var li = 0; li < numLines; li++)
                {
                    var l = lines[li];
                    lines[li] = new FlexLine
                    {
                        Items = l.Items, Start = l.Start, Count = l.Count, CrossSize = l.CrossSize,
                        OffsetCross = Alignment.ComputeAlignmentOffset(
                            freeSpace, numLines, gap, mode, c.isWrapReverse, li == 0),
                    };
                }
            }
        }

        // ── final_layout_pass ─────────────────────────────────────────────────

        private static Size<float> FinalLayoutPass(
            TaffyTree tree, List<FlexLine> lines, ref AlgoConstants c)
        {
            float totalOffsetCross;
            if (c.isColumn && c.layoutDirection == Direction.Rtl)
                totalOffsetCross = c.containerSize.Width - c.contentBoxInset.CrossEnd(c.dir);
            else
                totalOffsetCross = c.contentBoxInset.CrossStart(c.dir);

            var contentSize = SizeF.ZERO;

            if (c.isWrapReverse)
            {
                for (var li = lines.Count - 1; li >= 0; li--)
                    CalculateLayoutLine(tree, lines, li, ref totalOffsetCross, ref contentSize, ref c);
            }
            else
            {
                for (var li = 0; li < lines.Count; li++)
                    CalculateLayoutLine(tree, lines, li, ref totalOffsetCross, ref contentSize, ref c);
            }

            // Adjust content size for padding/border
            var sbX = c.scrollbarGutter.X;
            var sbY = c.scrollbarGutter.Y;
            if (c.layoutDirection == Direction.Rtl)
                contentSize.Width += c.contentBoxInset.Left - c.border.Left - sbX;
            else
                contentSize.Width += c.contentBoxInset.Right - c.border.Right - sbX;
            contentSize.Height += c.contentBoxInset.Bottom - c.border.Bottom - sbY;

            return contentSize;
        }

        private static void CalculateLayoutLine(
            TaffyTree tree, List<FlexLine> lines, int lineIdx,
            ref float totalOffsetCross, ref Size<float> contentSize, ref AlgoConstants c)
        {
            var line = lines[lineIdx];
            var dir = c.dir;
            var isRtlRow = dir.IsRow() && c.layoutDirection == Direction.Rtl;
            var isRtlColumn = dir.IsColumn() && c.layoutDirection == Direction.Rtl;

            var totalOffsetMain = isRtlRow
                ? c.containerSize.Width - c.contentBoxInset.MainEnd(dir)
                : c.contentBoxInset.MainStart(dir);

            var lineOffsetCross = line.OffsetCross;

            if (isRtlColumn)
                totalOffsetCross -= lineOffsetCross + line.CrossSize;

            if (dir.IsReverse())
            {
                for (var rev = 0; rev < line.Count; rev++)
                {
                    var i = line.Start + line.Count - 1 - rev;
                    CalculateFlexItem(tree, ref line.Items[i],
                        ref totalOffsetMain, totalOffsetCross, lineOffsetCross,
                        ref contentSize, ref c);
                }
            }
            else
            {
                for (var i = line.Start; i < line.Start + line.Count; i++)
                {
                    CalculateFlexItem(tree, ref line.Items[i],
                        ref totalOffsetMain, totalOffsetCross, lineOffsetCross,
                        ref contentSize, ref c);
                }
            }

            if (!isRtlColumn)
                totalOffsetCross += lineOffsetCross + line.CrossSize;
        }

        private static void CalculateFlexItem(
            TaffyTree tree, ref FlexItem item,
            ref float totalOffsetMain, float totalOffsetCross, float lineOffsetCross,
            ref Size<float> contentSize, ref AlgoConstants c)
        {
            var dir = c.dir;
            var isRtlRow    = dir.IsRow()    && c.layoutDirection == Direction.Rtl;
            var isRtlColumn = dir.IsColumn() && c.layoutDirection == Direction.Rtl;

            var output = PerformChildLayout(tree, item.nodeId,
                item.targetSize.Map(v => (float?)v),
                c.nodeInnerSize,
                new Size<AvailableSpace>(
                    AvailableSpace.Definite(c.containerSize.Width),
                    AvailableSpace.Definite(c.containerSize.Height)),
                SizingMode.ContentSize);

            var size = output.Size;

            // Relative inset
            float mainRelInset, crossRelInset;
            if (isRtlRow)
            {
                mainRelInset = item.inset.MainEnd(dir) ?? -(item.inset.MainStart(dir) ?? 0f);
            }
            else
            {
                mainRelInset = item.inset.MainStart(dir) ?? -(item.inset.MainEnd(dir) ?? 0f);
            }
            if (isRtlColumn)
            {
                crossRelInset = -(item.inset.CrossEnd(dir) ?? 0f);
                if (!item.inset.CrossEnd(dir).HasValue)
                    crossRelInset = item.inset.CrossStart(dir) ?? 0f;
            }
            else
            {
                crossRelInset = item.inset.CrossStart(dir) ?? -(item.inset.CrossEnd(dir) ?? 0f);
            }

            var effectiveLineCross = isRtlColumn ? 0f : lineOffsetCross;

            float offsetMain, offsetCross;
            if (isRtlRow)
            {
                offsetMain = totalOffsetMain - item.offsetMain - item.margin.MainEnd(dir)
                    - mainRelInset - size.Main(dir);
            }
            else
            {
                offsetMain = totalOffsetMain + item.offsetMain + item.margin.MainStart(dir) + mainRelInset;
            }

            offsetCross = totalOffsetCross + item.offsetCross + effectiveLineCross
                + item.margin.CrossStart(dir) + crossRelInset;

            // Baseline update
            if (dir.IsRow())
            {
                var baseCross = totalOffsetCross + item.offsetCross + effectiveLineCross
                                + item.margin.CrossStart(dir);
                var innerBaseline = output.FirstBaselines.Y ?? size.Height;
                item.baseline = baseCross + innerBaseline;
            }
            else
            {
                var baseMain = totalOffsetMain + item.offsetMain + item.margin.MainStart(dir);
                var innerBaseline = output.FirstBaselines.Y ?? size.Height;
                item.baseline = baseMain + innerBaseline;
            }

            var location = dir.IsRow()
                ? new Point<float>(offsetMain, offsetCross)
                : new Point<float>(offsetCross, offsetMain);

            var scrollbarSize = new Size<float>(
                item.overflow.Y == Overflow.Scroll ? item.scrollbarWidth : 0f,
                item.overflow.X == Overflow.Scroll ? item.scrollbarWidth : 0f);

            tree.SetNodeLayout(item.nodeId, new Layout
            {
                Order = item.order,
                Location = location,
                Size = size,
                ContentSize = output.ContentSize,
                ScrollbarSize = scrollbarSize,
                Border = item.border,
                Padding = item.padding,
                Margin = item.margin,
            });

            if (isRtlRow)
                totalOffsetMain -= item.offsetMain + RectF.MainAxisSum(item.margin, dir) + size.Main(dir);
            else
                totalOffsetMain += item.offsetMain + RectF.MainAxisSum(item.margin, dir) + size.Main(dir);

            // Content size contribution
            var contribLocation = c.layoutDirection == Direction.Rtl
                ? new Point<float>(c.containerSize.Width - (location.X + size.Width), location.Y)
                : location;

            contentSize = contentSize.F32Max(ComputeContentSizeContribution(
                contribLocation, size, output.ContentSize, item.overflow));
        }

        // ── perform_absolute_layout_on_absolute_children ─────────────────────

        private static Size<float> PerformAbsoluteLayout(
            TaffyTree tree, NodeId node, ref AlgoConstants c)
        {
            var cw = c.containerSize.Width;
            var ch = c.containerSize.Height;
            var insetRel = SizeF.Sub(c.containerSize,
                SizeF.Add(RectF.SumAxes(c.border), c.scrollbarGutter.ToSize()));

            var contentSize = SizeF.ZERO;
            var count = tree.ChildCount(node);

            for (var oi = 0; oi < count; oi++)
            {
                var child = tree.ChildAt(node, oi);
                var cs = tree.GetNodeData(child).Style;

                if (cs.boxGenerationMode == BoxGenerationMode.None) continue;
                if (cs.position != Position.Absolute) continue;

                var overflow = cs.overflow;
                var scrollW = cs.scrollbarWidth;
                var ar = cs.aspectRatio;
                var alignSelf = cs.alignSelf ?? c.alignItems;
                float? insetW = insetRel.Width;

                var margin = cs.margin.Map(m => m.ResolveToOption(insetW ?? 0f));
                var padding = cs.padding.ResolveOrZero(insetW);
                var border = cs.border.ResolveOrZero(insetW);
                var pbSize = RectF.SumAxes(RectF.Add(padding, border));
                var boxAdj = cs.boxSizing == BoxSizing.ContentBox ? pbSize : SizeF.ZERO;

                var left   = cs.inset.Left.MaybeResolve(insetW);
                var right  = cs.inset.Right.MaybeResolve(insetW);
                var top    = cs.inset.Top.MaybeResolve(insetRel.Height);
                var bottom = cs.inset.Bottom.MaybeResolve(insetRel.Height);

                var styleSize = SizeF.MaybeApplyAspectRatio(
                    cs.size.MaybeResolve(insetRel).MaybeAdd(boxAdj), ar);
                var minSz = SizeF.MaybeApplyAspectRatio(
                    cs.minSize.MaybeResolve(insetRel).MaybeAdd(boxAdj)
                    .Or(pbSize.Map(v => (float?)v))
                    .MaybeMax(pbSize), ar);
                var maxSz = SizeF.MaybeApplyAspectRatio(
                    cs.maxSize.MaybeResolve(insetRel).MaybeAdd(boxAdj), ar);

                var known = styleSize.MaybeClamp(minSz, maxSz);

                // Fill width from left/right
                if (!known.Width.HasValue && left.HasValue && right.HasValue)
                {
                    float? ml = margin.Left, mr = margin.Right;
                    var raw = (insetW ?? 0f).MaybeSub(ml).MaybeSub(mr) - left!.Value - right!.Value;
                    known.Width = MathF.Max(raw, 0f);
                    known = SizeF.MaybeApplyAspectRatio(known, ar).MaybeClamp(minSz, maxSz);
                }

                // Fill height from top/bottom
                if (!known.Height.HasValue && top.HasValue && bottom.HasValue)
                {
                    float? mt = margin.Top, mb = margin.Bottom;
                    var raw = insetRel.Height.MaybeSub(mt).MaybeSub(mb) - top!.Value - bottom!.Value;
                    known.Height = MathF.Max(raw, 0f);
                    known = SizeF.MaybeApplyAspectRatio(known, ar).MaybeClamp(minSz, maxSz);
                }

                var measured = PerformChildLayout(tree, child, known, c.nodeInnerSize,
                    new Size<AvailableSpace>(
                        AvailableSpace.Definite(cw.MaybeClamp(minSz.Width, maxSz.Width)),
                        AvailableSpace.Definite(ch.MaybeClamp(minSz.Height, maxSz.Height))),
                    SizingMode.InherentSize);

                var finalSize = known.Map(v => v ?? 0f)
                    .ZipMap(SizeF.ZERO, (k, _) => k > 0f ? k : measured.Size.Width > 0f
                        ? measured.Size.Width : 0f); // use measured if not known
                // Simpler: unwrap or measured
                finalSize = new Size<float>(
                    known.Width ?? measured.Size.Width,
                    known.Height ?? measured.Size.Height)
                    .MaybeClamp(minSz, maxSz);

                var layoutOut = PerformChildLayout(tree, child,
                    finalSize.Map(v => (float?)v), c.nodeInnerSize,
                    new Size<AvailableSpace>(
                        AvailableSpace.Definite(cw.MaybeClamp(minSz.Width, maxSz.Width)),
                        AvailableSpace.Definite(ch.MaybeClamp(minSz.Height, maxSz.Height))),
                    SizingMode.InherentSize);

                var nonAutoMargin = margin.Map(m => m ?? 0f);
                var freeSpace = new Size<float>(
                    MathF.Max(cw - finalSize.Width - RectF.HorizontalAxisSum(nonAutoMargin), 0f),
                    MathF.Max(ch - finalSize.Height - RectF.VerticalAxisSum(nonAutoMargin), 0f));

                float autoW = (margin.Left == null ? 1 : 0) + (margin.Right == null ? 1 : 0);
                float autoH = (margin.Top == null ? 1 : 0) + (margin.Bottom == null ? 1 : 0);
                var autoMarginW = autoW > 0f ? freeSpace.Width / autoW : 0f;
                var autoMarginH = autoH > 0f ? freeSpace.Height / autoH : 0f;
                var resolved = new Rect<float>(
                    margin.Left ?? autoMarginW, margin.Right ?? autoMarginW,
                    margin.Top ?? autoMarginH, margin.Bottom ?? autoMarginH);

                var isRtlRow    = c.isRow    && c.layoutDirection == Direction.Rtl;
                var isRtlColumn = c.isColumn && c.layoutDirection == Direction.Rtl;

                // Determine flex-relative insets
                var mainIsHoriz = c.isRow;
                var crossIsHoriz = !c.isRow;
                var mainIsRtl = mainIsHoriz && c.layoutDirection == Direction.Rtl;
                var crossIsRtl = crossIsHoriz && c.layoutDirection == Direction.Rtl;
                var mainFlexStartRev = c.dir.IsReverse() ^ mainIsRtl;
                var crossFlexStartRev = c.isWrapReverse ^ crossIsRtl;

                var startMain = c.isRow ? left : top;
                var endMain   = c.isRow ? right : bottom;
                var startCross = c.isRow ? top : left;
                var endCross   = c.isRow ? bottom : right;

                var mainSBStart = mainIsRtl ? c.scrollbarGutter.Main(c.dir) : 0f;
                var mainSBEnd   = mainIsRtl ? 0f : c.scrollbarGutter.Main(c.dir);
                var crossSBStart = crossIsRtl ? c.scrollbarGutter.Cross(c.dir) : 0f;
                var crossSBEnd   = crossIsRtl ? 0f : c.scrollbarGutter.Cross(c.dir);

                float offsetMain;
                if (startMain.HasValue || endMain.HasValue)
                {
                    if (mainIsRtl && endMain.HasValue)
                        offsetMain = c.containerSize.Main(c.dir) - c.border.MainEnd(c.dir)
                            - mainSBEnd - finalSize.Main(c.dir) - endMain!.Value
                            - resolved.MainEnd(c.dir);
                    else if (startMain.HasValue)
                        offsetMain = startMain!.Value + c.border.MainStart(c.dir)
                            + mainSBStart + resolved.MainStart(c.dir);
                    else
                        offsetMain = c.containerSize.Main(c.dir) - c.border.MainEnd(c.dir)
                            - mainSBEnd - finalSize.Main(c.dir) - (endMain ?? 0f)
                            - resolved.MainEnd(c.dir);
                }
                else
                {
                    var jc = c.justifyContent ?? AlignContent.Start;
                    offsetMain = ComputeAbsMainOffset(jc, mainFlexStartRev, ref c, finalSize, resolved);
                }

                float offsetCross;
                if (startCross.HasValue || endCross.HasValue)
                {
                    if (crossIsRtl && endCross.HasValue)
                        offsetCross = c.containerSize.Cross(c.dir) - c.border.CrossEnd(c.dir)
                            - crossSBEnd - finalSize.Cross(c.dir) - endCross!.Value
                            - resolved.CrossEnd(c.dir);
                    else if (startCross.HasValue)
                        offsetCross = startCross!.Value + c.border.CrossStart(c.dir)
                            + crossSBStart + resolved.CrossStart(c.dir);
                    else
                        offsetCross = c.containerSize.Cross(c.dir) - c.border.CrossEnd(c.dir)
                            - crossSBEnd - finalSize.Cross(c.dir) - (endCross ?? 0f)
                            - resolved.CrossEnd(c.dir);
                }
                else
                {
                    offsetCross = ComputeAbsCrossOffset(alignSelf, crossFlexStartRev, ref c, finalSize, resolved);
                }

                var loc = c.isRow
                    ? new Point<float>(offsetMain, offsetCross)
                    : new Point<float>(offsetCross, offsetMain);

                var sbSz = new Size<float>(
                    overflow.Y == Overflow.Scroll ? scrollW : 0f,
                    overflow.X == Overflow.Scroll ? scrollW : 0f);

                tree.SetNodeLayout(child, new Layout
                {
                    Order = (uint)oi,
                    Location = loc,
                    Size = finalSize,
                    ContentSize = layoutOut.ContentSize,
                    ScrollbarSize = sbSz,
                    Border = border,
                    Padding = padding,
                    Margin = resolved,
                });

                // Content size contribution
                var szContr = new Size<float>(
                    overflow.X == Overflow.Visible
                        ? MathF.Max(finalSize.Width, layoutOut.ContentSize.Width)
                        : finalSize.Width,
                    overflow.Y == Overflow.Visible
                        ? MathF.Max(finalSize.Height, layoutOut.ContentSize.Height)
                        : finalSize.Height);

                if (szContr.Width > 0f || szContr.Height > 0f)
                {
                    var absAreaOff = new Point<float>(
                        c.border.Left + (c.layoutDirection == Direction.Rtl ? c.scrollbarGutter.X : 0f),
                        c.border.Top);
                    var relLoc = new Point<float>(loc.X - absAreaOff.X, loc.Y - absAreaOff.Y);

                    Size<float> contribSz;
                    if (c.layoutDirection == Direction.Rtl)
                    {
                        var extra = MathF.Max(szContr.Width - finalSize.Width, 0f);
                        contribSz = new Size<float>(
                            MathF.Max(insetRel.Width - relLoc.X, 0f) + extra,
                            relLoc.Y + szContr.Height);
                    }
                    else
                    {
                        contribSz = new Size<float>(
                            relLoc.X + szContr.Width,
                            relLoc.Y + szContr.Height);
                    }
                    contentSize = contentSize.F32Max(contribSz);
                }
            }

            return contentSize;
        }

        private static float ComputeAbsMainOffset(
            AlignContent jc, bool mainFlexStartRev, ref AlgoConstants c,
            Size<float> finalSize, Rect<float> resolved)
        {
            switch (jc, mainFlexStartRev)
            {
                case (AlignContent.SpaceBetween, _):
                case (AlignContent.Stretch, false):
                case (AlignContent.FlexStart, false):
                case (AlignContent.FlexEnd, true):
                    return c.contentBoxInset.MainStart(c.dir) + resolved.MainStart(c.dir);
                case (AlignContent.Start, false):
                    return c.contentBoxInset.MainStart(c.dir) + resolved.MainStart(c.dir);
                case (AlignContent.Start, true):
                    return c.containerSize.Main(c.dir) - c.contentBoxInset.MainEnd(c.dir)
                        - finalSize.Main(c.dir) - resolved.MainEnd(c.dir);
                case (AlignContent.End, false):
                    return c.containerSize.Main(c.dir) - c.contentBoxInset.MainEnd(c.dir)
                        - finalSize.Main(c.dir) - resolved.MainEnd(c.dir);
                case (AlignContent.End, true):
                    return c.contentBoxInset.MainStart(c.dir) + resolved.MainStart(c.dir);
                case (AlignContent.FlexEnd, false):
                case (AlignContent.FlexStart, true):
                case (AlignContent.Stretch, true):
                    return c.containerSize.Main(c.dir) - c.contentBoxInset.MainEnd(c.dir)
                        - finalSize.Main(c.dir) - resolved.MainEnd(c.dir);
                default: // SpaceEvenly, SpaceAround, Center
                    return (c.containerSize.Main(c.dir)
                        + c.contentBoxInset.MainStart(c.dir)
                        - c.contentBoxInset.MainEnd(c.dir)
                        - finalSize.Main(c.dir)
                        + resolved.MainStart(c.dir)
                        - resolved.MainEnd(c.dir)) / 2f;
            }
        }

        private static float ComputeAbsCrossOffset(
            AlignItems alignSelf, bool crossFlexStartRev, ref AlgoConstants c,
            Size<float> finalSize, Rect<float> resolved)
        {
            switch (alignSelf, crossFlexStartRev)
            {
                case (AlignItems.Start, false):
                    return c.contentBoxInset.CrossStart(c.dir) + resolved.CrossStart(c.dir);
                case (AlignItems.Start, true):
                    return c.containerSize.Cross(c.dir) - c.contentBoxInset.CrossEnd(c.dir)
                        - finalSize.Cross(c.dir) - resolved.CrossEnd(c.dir);
                case (AlignItems.End, false):
                    return c.containerSize.Cross(c.dir) - c.contentBoxInset.CrossEnd(c.dir)
                        - finalSize.Cross(c.dir) - resolved.CrossEnd(c.dir);
                case (AlignItems.End, true):
                    return c.contentBoxInset.CrossStart(c.dir) + resolved.CrossStart(c.dir);
                case (AlignItems.Baseline, false):
                case (AlignItems.Stretch, false):
                case (AlignItems.FlexStart, false):
                case (AlignItems.FlexEnd, true):
                    return c.contentBoxInset.CrossStart(c.dir) + resolved.CrossStart(c.dir);
                default: // Baseline true, Stretch true, FlexStart true, FlexEnd false
                    if (alignSelf == AlignItems.Center)
                        return (c.containerSize.Cross(c.dir)
                            + c.contentBoxInset.CrossStart(c.dir)
                            - c.contentBoxInset.CrossEnd(c.dir)
                            - finalSize.Cross(c.dir)
                            + resolved.CrossStart(c.dir)
                            - resolved.CrossEnd(c.dir)) / 2f;
                    return c.containerSize.Cross(c.dir) - c.contentBoxInset.CrossEnd(c.dir)
                        - finalSize.Cross(c.dir) - resolved.CrossEnd(c.dir);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static float SumAxisGaps(float gap, int numItems) =>
            numItems <= 1 ? 0f : gap * (numItems - 1);

        private static Size<float> ComputeContentSizeContribution(
            Point<float> location, Size<float> size, Size<float> contentSize, Point<Overflow> overflow)
        {
            var effectiveSize = new Size<float>(
                overflow.X == Overflow.Visible ? MathF.Max(size.Width, contentSize.Width) : size.Width,
                overflow.Y == Overflow.Visible ? MathF.Max(size.Height, contentSize.Height) : size.Height);
            return new Size<float>(
                location.X + effectiveSize.Width,
                location.Y + effectiveSize.Height);
        }

        // ── Child layout helpers ──────────────────────────────────────────────

        /// <summary>Measures a child's size along one axis (RunMode.ComputeSize).</summary>
        private static float MeasureChildSize(
            TaffyTree tree, NodeId child,
            Size<float?> knownDimensions, Size<float?> parentSize,
            Size<AvailableSpace> available,
            SizingMode sizingMode, AbsoluteAxis axis)
        {
            var output = tree.PerformLayout(child, new LayoutInput
            {
                RunMode = RunMode.ComputeSize,
                KnownDimensions = knownDimensions,
                ParentSize = parentSize,
                availableSpace = available,
                SizingMode = sizingMode,
                Axis = RequestedAxisExt.FromAbsolute(axis),
                VerticalMarginsAreCollapsible = LineHelpers.FALSE,
            });
            return output.Size.GetAbs(axis);
        }

        /// <summary>Performs a full layout pass on a child (RunMode.PerformLayout).</summary>
        private static LayoutOutput PerformChildLayout(
            TaffyTree tree, NodeId child,
            Size<float?> knownDimensions, Size<float?> parentSize,
            Size<AvailableSpace> available,
            SizingMode sizingMode)
        {
            return tree.PerformLayout(child, new LayoutInput
            {
                RunMode = RunMode.PerformLayout,
                KnownDimensions = knownDimensions,
                ParentSize = parentSize,
                availableSpace = available,
                SizingMode = sizingMode,
                Axis = RequestedAxis.Both,
                VerticalMarginsAreCollapsible = LineHelpers.FALSE,
            });
        }
    }
}
