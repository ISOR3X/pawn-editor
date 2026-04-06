// Port of taffy/src/compute/common/alignment.rs
// Generic CSS alignment helpers shared by Flexbox and CSS Grid.

namespace Taffy
{
    internal static class Alignment
    {
        /// <summary>
        /// Implements CSS alignment fallback rules.
        /// Distributed keywords (Stretch, SpaceBetween, SpaceAround, SpaceEvenly) fall back when
        /// there is only one item or free space is non-positive.  Safe alignment falls back to Start
        /// when free space is negative.
        /// Port of <c>apply_alignment_fallback</c> in <c>taffy/src/compute/common/alignment.rs</c>.
        /// </summary>
        public static AlignContent ApplyAlignmentFallback(
            float freeSpace, int numItems, AlignContent mode, bool isSafe)
        {
            if (numItems <= 1 || freeSpace <= 0f)
            {
                switch (mode)
                {
                    case AlignContent.Stretch:
                    case AlignContent.SpaceBetween:
                        mode = AlignContent.FlexStart;
                        isSafe = true;
                        break;
                    case AlignContent.SpaceAround:
                    case AlignContent.SpaceEvenly:
                        mode = AlignContent.Center;
                        isSafe = true;
                        break;
                }
            }

            if (freeSpace <= 0f && isSafe)
                mode = AlignContent.Start;

            return mode;
        }

        /// <summary>
        /// Computes the offset to apply to a single item during alignment distribution.
        /// <paramref name="isFirst"/> selects the "initial offset" branch (applied once before the
        /// first item) vs. the "per-item gap" branch (applied before every subsequent item).
        /// Port of <c>compute_alignment_offset</c> in <c>taffy/src/compute/common/alignment.rs</c>.
        /// </summary>
        public static float ComputeAlignmentOffset(
            float freeSpace,
            int numItems,
            float gap,
            AlignContent mode,
            bool layoutIsFlexReversed,
            bool isFirst)
        {
            if (isFirst)
            {
                return mode switch
                {
                    AlignContent.Start => 0f,
                    AlignContent.FlexStart => layoutIsFlexReversed ? freeSpace : 0f,
                    AlignContent.End => freeSpace,
                    AlignContent.FlexEnd => layoutIsFlexReversed ? 0f : freeSpace,
                    AlignContent.Center => freeSpace / 2f,
                    AlignContent.Stretch => 0f,
                    AlignContent.SpaceBetween => 0f,
                    AlignContent.SpaceAround => freeSpace >= 0f ? freeSpace / numItems / 2f : freeSpace / 2f,
                    AlignContent.SpaceEvenly => freeSpace >= 0f ? freeSpace / (numItems + 1) : freeSpace / 2f,
                    _ => 0f
                };
            }

            var fs = MathF.Max(freeSpace, 0f);
            var extra = mode switch
            {
                AlignContent.SpaceBetween => numItems > 1 ? fs / (numItems - 1) : 0f,
                AlignContent.SpaceAround => fs / numItems,
                AlignContent.SpaceEvenly => fs / (numItems + 1),
                _ => 0f
            };
            return gap + extra;
        }
    }
}