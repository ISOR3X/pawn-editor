using System;
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
            float containerW = ResolveSize(container.Style.width,  availableRect.width,  availableRect.width);
            float containerH = ResolveSize(container.Style.height, availableRect.height, availableRect.height);
            containerW = Clamp(containerW, container.Style.minWidth,  container.Style.maxWidth,  availableRect.width);
            containerH = Clamp(containerH, container.Style.minHeight, container.Style.maxHeight, availableRect.height);

            var containerRect = new Rect(availableRect.x, availableRect.y, containerW, containerH);
            container.ComputedRect = containerRect;

            if (container.Children.Count == 0) return;
            if (container.Style.display != Display.Flex)
            {
                return;
            }

            bool isRow     = container.Style.flexDirection == FlexDirection.Row
                          || container.Style.flexDirection == FlexDirection.RowReverse;
            bool isReverse = container.Style.flexDirection == FlexDirection.RowReverse
                          || container.Style.flexDirection == FlexDirection.ColumnReverse;
            bool canWrap   = container.Style.flexWrap != FlexWrap.NoWrap;

            float mainSize  = isRow ? containerRect.width  : containerRect.height;
            float crossSize = isRow ? containerRect.height : containerRect.width;

            // Map CSS row-gap/column-gap to main/cross axis based on direction.
            // In a row container:    items flow horizontally → column-gap between items, row-gap between lines.
            // In a column container: items flow vertically   → row-gap between items, column-gap between lines.
            float itemGap = isRow ? container.Style.columnGap : container.Style.rowGap;
            float lineGap = isRow ? container.Style.rowGap    : container.Style.columnGap;

            // Sort children by order, preserving source order for ties
            var items = SortByOrder(container.Children);
            if (!canWrap)
            {
                var line = ComputeLine(items, mainSize, crossSize, isRow, itemGap, container);
                PlaceLine(line, containerRect, isRow, isReverse,
                          container.Style.flexWrap == FlexWrap.WrapReverse,
                          itemGap, 0f, 0f);
            }
            else
            {
                var lines = WrapIntoLines(items, mainSize, crossSize, isRow,
                                          itemGap, container);
                float crossOffset = 0f;
                bool wrapReverse  = container.Style.flexWrap == FlexWrap.WrapReverse;

                foreach (var line in lines)
                {
                    PlaceLine(line, containerRect, isRow, isReverse, wrapReverse,
                              itemGap, crossOffset, 0f);
                    crossOffset += line.CrossSize + lineGap;
                }
            }

            // Recurse into nested containers
            foreach (var child in items)
            {
                if (child.NestedContainer is LayoutContainer nested)
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

                if (item.AsContainer is LayoutContainer nested)
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
            public FlexItem Item;
            public float    Basis;       // resolved flex-basis along main axis
            public float    CrossSize;   // resolved size along cross axis
            public float    FinalMain;   // result of grow/shrink resolution
            public bool     Frozen;      // clamped during shrink pass
        }

        private struct FlexLine
        {
            public List<ItemData> Items;
            public float CrossSize;  // tallest/widest item in the line
        }

        // ──────────────────────────────────────────────────────────────────────
        // Line building
        // ──────────────────────────────────────────────────────────────────────

        private static List<FlexLine> WrapIntoLines(
            List<FlexItem> items, float mainSize, float crossSize,
            bool isRow, float gap, LayoutContainer container)
        {
            var lines     = new List<FlexLine>();
            var current   = new List<FlexItem>();
            float used     = 0f;
            bool  firstItem = true;

            foreach (var item in items)
            {
                float basis = ResolveBasis(item, mainSize, crossSize, isRow, container);
                float itemMin = ResolveMinMain(item, isRow, mainSize);
                float need    = basis;

                if (!firstItem && used + gap + need > mainSize && current.Count > 0)
                {
                    lines.Add(ComputeLine(current, mainSize, crossSize, isRow, gap, container));
                    current   = new List<FlexItem>();
                    used      = 0f;
                    firstItem = true;
                }

                current.Add(item);
                used      += (firstItem ? 0f : gap) + need;
                firstItem  = false;
            }

            if (current.Count > 0)
                lines.Add(ComputeLine(current, mainSize, crossSize, isRow, gap, container));

            return lines;
        }

        private static FlexLine ComputeLine(
            List<FlexItem> items, float mainSize, float crossSize,
            bool isRow, float gap, LayoutContainer container)
        {
            int count = items.Count;
            var data  = new List<ItemData>(count);

            // Step 1: resolve basis and cross size for each item
            foreach (var item in items)
            {
                float basis = ResolveBasis(item, mainSize, crossSize, isRow, container);
                float cs    = ResolveCrossSize(item, crossSize, isRow);
                data.Add(new ItemData { Item = item, Basis = basis, CrossSize = cs, Frozen = false });
            }

            // Step 2: compute free space
            float totalGaps = gap * (count - 1);
            float usedBasis = 0f;
            foreach (var d in data) usedBasis += d.Basis;
            float freeSpace = mainSize - usedBasis - totalGaps;

            // Step 3: grow or shrink
            if (freeSpace > 0f)
                GrowItems(data, freeSpace, mainSize, isRow);
            else if (freeSpace < 0f)
                ShrinkItems(data, freeSpace, mainSize, isRow);
            else
                for (int i = 0; i < data.Count; i++)
                {
                    var d = data[i]; d.FinalMain = d.Basis; data[i] = d;
                }

            // Step 4: find line cross size (max of all items)
            float lineCross = 0f;
            foreach (var d in data) lineCross = Mathf.Max(lineCross, d.CrossSize);

            return new FlexLine { Items = data, CrossSize = lineCross };
        }

        // ──────────────────────────────────────────────────────────────────────
        // Grow pass
        // ──────────────────────────────────────────────────────────────────────

        private static void GrowItems(List<ItemData> data, float freeSpace, float mainSize, bool isRow)
        {
            float totalGrow = 0f;
            foreach (var d in data) totalGrow += d.Item.Style.grow;

            if (totalGrow <= 0f)
            {
                // Nobody wants to grow; just use basis
                for (int i = 0; i < data.Count; i++)
                {
                    var d = data[i]; d.FinalMain = d.Basis; data[i] = d;
                }
                return;
            }

            // Iterative: clamp to max, redistribute remainder
            bool changed = true;
            while (changed)
            {
                changed = false;
                float remainingFree  = freeSpace;
                float remainingGrow  = 0f;

                foreach (var d in data)
                    if (!d.Frozen) remainingGrow += d.Item.Style.grow;

                if (remainingGrow <= 0f) break;

                for (int i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    if (d.Frozen) continue;

                    float share  = (d.Item.Style.grow / remainingGrow) * freeSpace;
                    float target = d.Basis + share;
                    float maxVal = ResolveMaxMain(d.Item, isRow, mainSize);

                    if (maxVal >= 0f && target > maxVal)
                    {
                        d.FinalMain = maxVal;
                        d.Frozen    = true;
                        freeSpace  -= (maxVal - d.Basis);  // give back overshoot
                        changed     = true;
                    }
                    else
                    {
                        d.FinalMain = target;
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
            float overflow = -freeSpace; // positive amount we need to remove

            bool changed = true;
            while (changed && overflow > 0.001f)
            {
                changed = false;

                // Scaled shrink factor = flex-shrink * flex-basis
                float totalScaled = 0f;
                foreach (var d in data)
                    if (!d.Frozen) totalScaled += d.Item.Style.shrink * d.Basis;

                if (totalScaled <= 0f) break;

                for (int i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    if (d.Frozen) continue;

                    float scaled     = d.Item.Style.shrink * d.Basis;
                    float reduction  = (scaled / totalScaled) * overflow;
                    float target     = d.Basis - reduction;
                    float minVal     = ResolveMinMain(d.Item, isRow, mainSize);

                    if (target < minVal)
                    {
                        d.FinalMain = minVal;
                        d.Frozen    = true;
                        overflow   -= (d.Basis - minVal);  // this item absorbed less than its share
                        changed     = true;
                    }
                    else
                    {
                        d.FinalMain = target;
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
            float mainStart  = isRow ? containerRect.x : containerRect.y;
            float crossStart = isRow ? containerRect.y : containerRect.x;

            // Reverse direction: start from the far end
            if (isReverse)
            {
                float totalMain = 0f;
                foreach (var d in line.Items) totalMain += d.FinalMain;
                totalMain += mainGap * (line.Items.Count - 1);
                mainStart += (isRow ? containerRect.width : containerRect.height) - totalMain;
            }

            float cursor = mainStart + mainOffset;

            // WrapReverse: flip which side of the container lines start on
            float lineOffset = wrapReverse
                ? (isRow ? containerRect.height : containerRect.width) - crossOffset - line.CrossSize
                : crossOffset;

            foreach (var d in line.Items)
            {
                float mainPos  = cursor;
                float crossPos = crossStart + lineOffset;

                Rect rect = isRow
                    ? new Rect(mainPos, crossPos, d.FinalMain, d.CrossSize)
                    : new Rect(crossPos, mainPos, d.CrossSize, d.FinalMain);

                d.Item.ComputedRect = rect;

                cursor += d.FinalMain + mainGap;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers — size resolution
        // ──────────────────────────────────────────────────────────────────────

        private static float ResolveBasis(
            FlexItem item, float mainSize, float crossSize,
            bool isRow, LayoutContainer container)
        {
            StyleLength basis = item.Style.basis;

            float raw;
            if (basis.IsAuto)
            {
                // Use Width (row) or Height (column) if explicitly set; else 0
                StyleLength sizeAlongMain = isRow ? item.Style.width : item.Style.height;
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
            StyleLength sizeAlongCross = isRow ? item.Style.height : item.Style.width;
            float raw = sizeAlongCross.IsAuto ? crossSize : sizeAlongCross.ResolveOr(crossSize, crossSize);
            raw = ClampCross(raw, item, isRow, crossSize);
            return raw;
        }

        private static float ResolveMinMain(FlexItem item, bool isRow, float parentMain)
        {
            StyleLength min = isRow ? item.Style.minWidth : item.Style.minHeight;
            return min.ResolveOr(parentMain, 0f);
        }

        /// <returns>-1 if unconstrained.</returns>
        private static float ResolveMaxMain(FlexItem item, bool isRow, float parentMain)
        {
            StyleLength max = isRow ? item.Style.maxWidth : item.Style.maxHeight;
            if (max.IsAuto) return -1f;
            return max.ResolveOr(parentMain, -1f);
        }

        private static float ClampMain(float value, FlexItem item, bool isRow, float parentMain)
        {
            float min = ResolveMinMain(item, isRow, parentMain);
            float max = ResolveMaxMain(item, isRow, parentMain);
            value = Mathf.Max(value, min);
            if (max >= 0f) value = Mathf.Min(value, max);
            return value;
        }

        private static float ClampCross(float value, FlexItem item, bool isRow, float parentCross)
        {
            StyleLength minL = isRow ? item.Style.minHeight : item.Style.minWidth;
            StyleLength maxL = isRow ? item.Style.maxHeight : item.Style.maxWidth;
            float min  = minL.ResolveOr(parentCross, 0f);
            float max  = maxL.IsAuto ? -1f : maxL.ResolveOr(parentCross, -1f);
            value = Mathf.Max(value, min);
            if (max >= 0f) value = Mathf.Min(value, max);
            return value;
        }

        private static float ResolveSize(StyleLength length, float parentSize, float fallback)
            => length.IsAuto ? fallback : length.ResolveOr(parentSize, fallback);

        private static float Clamp(float value, StyleLength min, StyleLength max, float parent)
        {
            float minVal = min.ResolveOr(parent, 0f);
            float maxVal = max.IsAuto ? float.MaxValue : max.ResolveOr(parent, float.MaxValue);
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
            for (int i = 0; i < children.Count; i++)
                if (children[i] is FlexItem fi) indexed.Add((fi, i));

            indexed.Sort((a, b) =>
            {
                int cmp = a.item.Style.order.CompareTo(b.item.Style.order);
                return cmp != 0 ? cmp : a.index.CompareTo(b.index);
            });

            var result = new List<FlexItem>(indexed.Count);
            foreach (var (item, _) in indexed) result.Add(item);
            return result;
        }
    }
}