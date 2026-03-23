// Per-child data accumulated and modified during the block layout algorithm.
// Port of the BlockItem struct in taffy/src/compute/block.rs.

namespace PawnEditor.TaffySharp
{
    /// <summary>Per-child data accumulated during block layout.</summary>
    internal struct BlockItem
    {
        /// <summary>Node identifier of the child.</summary>
        public NodeId nodeId;

        /// <summary>Source-order index used to set the render order on the layout.</summary>
        public uint order;

        /// <summary>True for table-element children (stretch sizing is skipped for them).</summary>
        public bool isTable;

        /// <summary>
        /// True when the child is a non-independent block node that participates in the same
        /// Block Formatting Context as its parent (not absolutely positioned, not a scroll container,
        /// not a table).
        /// </summary>
        public bool isInSameBfc;

        // ── Pre-resolved sizes ────────────────────────────────────────────────

        public Size<float?> size;
        public Size<float?> minSize;
        public Size<float?> maxSize;

        // ── Style copies ──────────────────────────────────────────────────────

        public Point<Overflow> overflow;
        public float scrollbarWidth;
        public Position position;
        public Rect<LengthPercentageAuto> inset;
        public Rect<LengthPercentageAuto> margin;
        public Rect<float> padding;
        public Rect<float> border;
        public Size<float> paddingBorderSum;

        // ── Outputs computed during layout ────────────────────────────────────

        /// <summary>Final border-box size after layout.</summary>
        public Size<float> computedSize;

        /// <summary>
        /// "Static position" — where this item would be placed in normal flow
        /// (used to anchor absolutely positioned children).
        /// </summary>
        public Point<float> staticPosition;

        /// <summary>True when vertical margins can collapse through this item.</summary>
        public bool canBeCollapsedThrough;
    }
}