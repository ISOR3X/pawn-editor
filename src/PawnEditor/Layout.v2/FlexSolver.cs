using System.Collections.Generic;
using UnityEngine;

namespace FlexLayout
{
    /// <summary>
    /// Stateless layout solver for <see cref="LayoutContainer"/>.
    /// Call <see cref="Compute"/> with a container and the available rect;
    /// it will populate <see cref="LayoutElement.ComputedRect"/> on every child
    /// (and recursively on any nested containers).
    /// </summary>
    public static class FlexSolver
    {
        // ──────────────────────────────────────────────────────────────────────
        // Public entry point
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes layout for <paramref name="container"/> inside <paramref name="availableRect"/>.
        /// </summary>
        public static void Compute(LayoutContainer container, Rect availableRect)
        {
            // Resolve the container's own rect
            var containerW = ResolveSize(container.Style.width, availableRect.width, availableRect.width);
            var containerH = ResolveSize(container.Style.height, availableRect.height, availableRect.height);
            containerW = Clamp(containerW, container.Style.minWidth, container.Style.maxWidth, availableRect.width);
            containerH = Clamp(containerH, container.Style.minHeight, container.Style.maxHeight, availableRect.height);

            var containerRect = new Rect(availableRect.x, availableRect.y, containerW, containerH);
            container.ComputedRect = containerRect;

            if (container.Children.Count == 0) return;
            if (container.Style.display != Display.Flex)
            {
                return;
            }

            var isRow = container.Style.flexDirection == FlexDirection.Row
                        || container.Style.flexDirection == FlexDirection.RowReverse;
            var isReverse = container.Style.flexDirection == FlexDirection.RowReverse
                            || container.Style.flexDirection == FlexDirection.ColumnReverse;
            var canWrap = container.Style.flexWrap != FlexWrap.NoWrap;

            var mainSize = isRow ? containerRect.width : containerRect.height;
            var crossSize = isRow ? containerRect.height : containerRect.width;

            // Map CSS row-gap/column-gap to main/cross axis based on direction.
            // In a row container:    items flow horizontally → column-gap between items, row-gap between lines.
            // In a column container: items flow vertically   → row-gap between items, column-gap between lines.
            var itemGap = isRow ? container.Style.columnGap : container.Style.rowGap;
            var lineGap = isRow ? container.Style.rowGap : container.Style.columnGap;

            // Sort children by order, preserving source order for ties
            var items = SortByOrder(container.Children);
            // actualCrossSize is the real cross size from availableRect — used as fallback
            // for Auto cross-size items. Distinct from crossSize which may be 100_000f
            // during fitContent measurement passes.
            var actualCrossSize = isRow ? availableRect.height : availableRect.width;

            if (!canWrap)
            {
                var line = ComputeLine(items, mainSize, crossSize, actualCrossSize, isRow, itemGap);
                PlaceLine(line, containerRect, isRow, isReverse,
                    container.Style.flexWrap == FlexWrap.WrapReverse,
                    itemGap, 0f, 0f);
            }
            else
            {
                var lines = WrapIntoLines(items, mainSize, crossSize, actualCrossSize, isRow, itemGap);
                var crossOffset = 0f;
                var wrapReverse = container.Style.flexWrap == FlexWrap.WrapReverse;

                foreach (var line in lines)
                {
                    PlaceLine(line, containerRect, isRow, isReverse, wrapReverse,
                        itemGap, crossOffset, 0f);
                    crossOffset += line.crossSize + lineGap;
                }
            }

            // Recurse into nested containers
            foreach (var child in items)
            {
                if (child.NestedContainer is { } nested)
                    Compute(nested, child.ComputedRect);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Draw
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes layout for <paramref name="container"/> inside <paramref name="availableRect"/>,
        /// then immediately invokes each <see cref="FlexItem.OnDraw"/> callback in tree order.
        /// Equivalent to calling <see cref="Compute"/> followed by <see cref="Draw"/>.
        /// </summary>
        public static void ComputeAndDraw(LayoutContainer container, Rect availableRect)
        {
            Compute(container, availableRect);
            Draw(container);
        }

        /// <summary>
        /// Walks the already-computed tree and invokes each <see cref="FlexItem.OnDraw"/>
        /// callback. Call this after <see cref="Compute"/> when you need to inspect rects
        /// before drawing (e.g. hit testing, tooltips).
        /// </summary>
        public static void Draw(LayoutContainer container)
        {
            foreach (var child in container.Children)
            {
                if (child is not FlexItem item) continue;

                if (item.AsContainer is { } nested)
                {
                    // Nested container: recurse, don't call OnDraw on the wrapper item
                    Draw(nested);
                }
                else
                {
                    item.OnDraw?.Invoke(item.ComputedRect);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Internal data structures
        // ──────────────────────────────────────────────────────────────────────

        private struct ItemData
        {
            public FlexItem item;
            public float basis; // resolved flex-basis along main axis
            public float crossSize; // resolved size along cross axis
            public float finalMain; // result of grow/shrink resolution
            public bool frozen; // clamped during shrink pass
        }

        private struct FlexLine
        {
            public List<ItemData> items;
            public float crossSize; // tallest/widest item in the line
        }

        // ──────────────────────────────────────────────────────────────────────
        // Line building
        // ──────────────────────────────────────────────────────────────────────

        private static List<FlexLine> WrapIntoLines(
            List<FlexItem> items, float mainSize, float crossSize, float actualCrossSize,
            bool isRow, float gap)
        {
            var lines = new List<FlexLine>();
            var current = new List<FlexItem>();
            var used = 0f;
            var firstItem = true;

            foreach (var item in items)
            {
                var basis = ResolveBasis(item, mainSize, isRow);
                var need = basis;

                if (!firstItem && used + gap + need > mainSize && current.Count > 0)
                {
                    lines.Add(ComputeLine(current, mainSize, crossSize, actualCrossSize, isRow, gap));
                    current = new List<FlexItem>();
                    used = 0f;
                    firstItem = true;
                }

                current.Add(item);
                used += (firstItem ? 0f : gap) + need;
                firstItem = false;
            }

            if (current.Count > 0)
                lines.Add(ComputeLine(current, mainSize, crossSize, actualCrossSize, isRow, gap));

            return lines;
        }

        private static FlexLine ComputeLine(
            List<FlexItem> items, float mainSize, float crossSize, float actualCrossSize,
            bool isRow, float gap)
        {
            var count = items.Count;
            var data = new List<ItemData>(count);

            // Step 1: resolve basis and cross size for each item
            foreach (var item in items)
            {
                var basis = ResolveBasis(item, mainSize, isRow);
                var cs = ResolveCrossSize(item, crossSize, isRow);
                data.Add(new ItemData { item = item, basis = basis, crossSize = cs, frozen = false });
            }

            // Step 2: compute free space
            var totalGaps = gap * (count - 1);
            var usedBasis = 0f;
            foreach (var d in data) usedBasis += d.basis;
            var freeSpace = mainSize - usedBasis - totalGaps;

            // Step 3: grow or shrink
            if (freeSpace > 0f)
                GrowItems(data, freeSpace, mainSize, isRow);
            else if (freeSpace < 0f)
                ShrinkItems(data, freeSpace, mainSize, isRow);
            else
                for (var i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    d.finalMain = d.basis;
                    data[i] = d;
                }

            // Step 4: find line cross size.
            // First try items with explicit cross sizes — they define the natural line height.
            // If all items are Auto (no explicit sizes), fall back to the available crossSize
            // so items still fill the container (e.g. all items in a column have Auto width).
            var lineCross = 0f;
            foreach (var d in data)
                if (!(isRow ? d.item.Style.height : d.item.Style.width).IsAuto)
                    lineCross = Mathf.Max(lineCross, d.crossSize);

            // If no explicit cross sizes found, use available crossSize as the line height.
            if (lineCross <= 0f) lineCross = actualCrossSize;

            // Step 5: stretch Auto cross-size items to the line cross size.
            for (var i = 0; i < data.Count; i++)
            {
                var d = data[i];
                if ((isRow ? d.item.Style.height : d.item.Style.width).IsAuto)
                {
                    d.crossSize = lineCross;
                    data[i] = d;
                }
            }

            return new FlexLine { items = data, crossSize = lineCross };
        }

        // ──────────────────────────────────────────────────────────────────────
        // Grow pass
        // ──────────────────────────────────────────────────────────────────────

        private static void GrowItems(List<ItemData> data, float freeSpace, float mainSize, bool isRow)
        {
            var totalGrow = 0f;
            foreach (var d in data) totalGrow += d.item.Style.flexGrow;

            if (totalGrow <= 0f)
            {
                // Nobody wants to grow; just use basis
                for (var i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    d.finalMain = d.basis;
                    data[i] = d;
                }

                return;
            }

            // Iterative: clamp to max, redistribute remainder
            var changed = true;
            while (changed)
            {
                changed = false;
                var remainingGrow = 0f;

                foreach (var d in data)
                    if (!d.frozen)
                        remainingGrow += d.item.Style.flexGrow;

                if (remainingGrow <= 0f) break;

                for (var i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    if (d.frozen) continue;

                    var share = (d.item.Style.flexGrow / remainingGrow) * freeSpace;
                    var target = d.basis + share;
                    var maxVal = ResolveMaxMain(d.item, isRow, mainSize);

                    if (maxVal >= 0f && target > maxVal)
                    {
                        d.finalMain = maxVal;
                        d.frozen = true;
                        freeSpace -= (maxVal - d.basis); // give back overshoot
                        changed = true;
                    }
                    else
                    {
                        d.finalMain = target;
                    }

                    data[i] = d;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Shrink pass (weighted by basis × shrink factor)
        // ──────────────────────────────────────────────────────────────────────

        private static void ShrinkItems(List<ItemData> data, float freeSpace, float mainSize, bool isRow)
        {
            var overflow = -freeSpace; // positive amount we need to remove

            var changed = true;
            while (changed && overflow > 0.001f)
            {
                changed = false;

                // Scaled shrink factor = flex-shrink * flex-basis
                var totalScaled = 0f;
                foreach (var d in data)
                    if (!d.frozen)
                        totalScaled += d.item.Style.flexShrink * d.basis;

                if (totalScaled <= 0f) break;

                for (var i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    if (d.frozen) continue;

                    var scaled = d.item.Style.flexShrink * d.basis;
                    var reduction = (scaled / totalScaled) * overflow;
                    var target = d.basis - reduction;
                    var minVal = ResolveMinMain(d.item, isRow, mainSize);

                    if (target < minVal)
                    {
                        d.finalMain = minVal;
                        d.frozen = true;
                        overflow -= (d.basis - minVal); // this item absorbed less than its share
                        changed = true;
                    }
                    else
                    {
                        d.finalMain = target;
                    }

                    data[i] = d;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Placement
        // ──────────────────────────────────────────────────────────────────────

        private static void PlaceLine(
            FlexLine line, Rect containerRect,
            bool isRow, bool isReverse, bool wrapReverse,
            float mainGap, float crossOffset, float mainOffset)
        {
            var mainStart = isRow ? containerRect.x : containerRect.y;
            var crossStart = isRow ? containerRect.y : containerRect.x;

            // Reverse direction: start from the far end
            if (isReverse)
            {
                var totalMain = 0f;
                foreach (var d in line.items) totalMain += d.finalMain;
                totalMain += mainGap * (line.items.Count - 1);
                mainStart += (isRow ? containerRect.width : containerRect.height) - totalMain;
            }

            var cursor = mainStart + mainOffset;

            // WrapReverse: flip which side of the container lines start on
            var lineOffset = wrapReverse
                ? (isRow ? containerRect.height : containerRect.width) - crossOffset - line.crossSize
                : crossOffset;

            foreach (var d in line.items)
            {
                var mainPos = cursor;
                var crossPos = crossStart + lineOffset;

                var rect = isRow
                    ? new Rect(mainPos, crossPos, d.finalMain, d.crossSize)
                    : new Rect(crossPos, mainPos, d.crossSize, d.finalMain);

                d.item.ComputedRect = rect;

                cursor += d.finalMain + mainGap;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers — size resolution
        // ──────────────────────────────────────────────────────────────────────

        private static float ResolveBasis(
            FlexItem item, float mainSize, bool isRow)
        {
            var basis = item.Style.flexBasis;

            float raw;
            if (basis.IsAuto)
            {
                // Use Width (row) or Height (column) if explicitly set; else 0
                var sizeAlongMain = isRow ? item.Style.width : item.Style.height;
                raw = sizeAlongMain.IsAuto ? 0f : sizeAlongMain.ResolveOr(mainSize, 0f);
            }
            else
            {
                raw = basis.ResolveOr(mainSize, 0f);
            }

            // Clamp to min/max along the main axis
            raw = ClampMain(raw, item, isRow, mainSize);
            return raw;
        }

        private static float ResolveCrossSize(FlexItem item, float crossSize, bool isRow)
        {
            var sizeAlongCross = isRow ? item.Style.height : item.Style.width;
            // Auto items get 0 as sentinel — they stretch to the natural line cross size,
            // which is determined from siblings with explicit cross sizes.
            // This prevents Auto items from stretching to an artificially large crossSize
            // (e.g. 100_000f used for unconstrained measurement passes).
            if (sizeAlongCross.IsAuto) return 0f;
            var raw = sizeAlongCross.ResolveOr(crossSize, crossSize);
            raw = ClampCross(raw, item, isRow, crossSize);
            return raw;
        }

        private static float ResolveMinMain(FlexItem item, bool isRow, float parentMain)
        {
            var min = isRow ? item.Style.minWidth : item.Style.minHeight;
            return min.ResolveOr(parentMain, 0f);
        }

        /// <returns>-1 if unconstrained.</returns>
        private static float ResolveMaxMain(FlexItem item, bool isRow, float parentMain)
        {
            var max = isRow ? item.Style.maxWidth : item.Style.maxHeight;
            if (max.IsAuto) return -1f;
            return max.ResolveOr(parentMain, -1f);
        }

        private static float ClampMain(float value, FlexItem item, bool isRow, float parentMain)
        {
            var min = ResolveMinMain(item, isRow, parentMain);
            var max = ResolveMaxMain(item, isRow, parentMain);
            value = Mathf.Max(value, min);
            if (max >= 0f) value = Mathf.Min(value, max);
            return value;
        }

        private static float ClampCross(float value, FlexItem item, bool isRow, float parentCross)
        {
            var minL = isRow ? item.Style.minHeight : item.Style.minWidth;
            var maxL = isRow ? item.Style.maxHeight : item.Style.maxWidth;
            var min = minL.ResolveOr(parentCross, 0f);
            var max = maxL.IsAuto ? -1f : maxL.ResolveOr(parentCross, -1f);
            value = Mathf.Max(value, min);
            if (max >= 0f) value = Mathf.Min(value, max);
            return value;
        }

        private static float ResolveSize(StyleSize length, float parentSize, float fallback)
            => length.IsAuto ? fallback : length.ResolveOr(parentSize, fallback);

        private static float Clamp(float value, StyleSize min, StyleSize max, float parent)
        {
            var minVal = min.ResolveOr(parent, 0f);
            var maxVal = max.IsAuto ? float.MaxValue : max.ResolveOr(parent, float.MaxValue);
            return Mathf.Clamp(value, minVal, maxVal);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers — ordering
        // ──────────────────────────────────────────────────────────────────────

        private static List<FlexItem> SortByOrder(IReadOnlyList<LayoutElement> children)
        {
            // Capture original indices before sorting so the tiebreak is stable.
            // List.Sort is not a stable sort — using IndexOf mid-sort is unsafe
            // because the list is partially mutated during comparison.
            var indexed = new List<(FlexItem item, int index)>(children.Count);
            for (var i = 0; i < children.Count; i++)
                if (children[i] is FlexItem fi)
                    indexed.Add((fi, i));

            indexed.Sort((a, b) =>
            {
                var cmp = a.item.Style.order.CompareTo(b.item.Style.order);
                return cmp != 0 ? cmp : a.index.CompareTo(b.index);
            });

            var result = new List<FlexItem>(indexed.Count);
            foreach (var (item, _) in indexed) result.Add(item);
            return result;
        }
    }
}