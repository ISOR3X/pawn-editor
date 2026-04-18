// Per-child data accumulated during the flexbox layout algorithm.
// Port of the FlexItem struct in taffy/src/compute/flexbox.rs.

namespace Taffy;

/// <summary>Intermediate results for a single flex item during layout.</summary>
internal struct FlexItem
{
    // ── Identity ─────────────────────────────────────────────────────────

    public NodeId nodeId;
    public uint order;

    // ── Style-resolved sizes ──────────────────────────────────────────────

    /// <summary>Resolved preferred size (null = auto).</summary>
    public Size<float?> size;

    /// <summary>Resolved minimum size.</summary>
    public Size<float?> minSize;

    /// <summary>Resolved maximum size.</summary>
    public Size<float?> maxSize;

    /// <summary>Cross-axis alignment (inherits from container's align-items if not set).</summary>
    public AlignItems alignSelf;

    // ── Scrollbar / overflow ──────────────────────────────────────────────

    public Point<Overflow> overflow;
    public float scrollbarWidth;

    // ── Flex factors ──────────────────────────────────────────────────────

    public float flexGrow;
    public float flexShrink;

    // ── Auto minimum size ─────────────────────────────────────────────────

    /// <summary>
    ///     The resolved minimum main size including automatic content-based minimum.
    ///     Differs from <see cref="minSize" /> because it incorporates auto min-size logic.
    /// </summary>
    public float resolvedMinimumMainSize;

    // ── Box model ─────────────────────────────────────────────────────────

    /// <summary>Resolved inset offsets (null = auto).</summary>
    public Rect<float?> inset;

    /// <summary>Resolved margin (auto margins resolved to 0 here, then expanded later).</summary>
    public Rect<float> margin;

    /// <summary>True for each margin side that was 'auto' in the style.</summary>
    public Rect<bool> marginIsAuto;

    public Rect<float> padding;
    public Rect<float> border;

    // ── Flex algorithm temporaries ────────────────────────────────────────

    /// <summary>The flex base size (main axis).</summary>
    public float flexBasis;

    /// <summary>Flex base size minus padding and border on the main axis.</summary>
    public float innerFlexBasis;

    /// <summary>Amount by which this item deviated from its target size (for clamping).</summary>
    public float violation;

    /// <summary>Whether this item's main size is locked (frozen).</summary>
    public bool frozen;

    /// <summary>Max- or min-content flex fraction (used for intrinsic container sizing).</summary>
    public float contentFlexFraction;

    // ── Computed intermediate sizes ───────────────────────────────────────

    public Size<float> hypotheticalInnerSize;
    public Size<float> hypotheticalOuterSize;
    public Size<float> targetSize;
    public Size<float> outerTargetSize;

    // ── Baseline ─────────────────────────────────────────────────────────

    /// <summary>Position of the first text baseline (used for baseline alignment).</summary>
    public float baseline;

    // ── Final offsets ─────────────────────────────────────────────────────

    /// <summary>Offset from natural position along the main axis.</summary>
    public float offsetMain;

    /// <summary>Offset from natural position along the cross axis.</summary>
    public float offsetCross;

    // ── Helpers ───────────────────────────────────────────────────────────

    public bool IsScrollContainer()
    {
        return overflow.X.IsScrollContainer() || overflow.Y.IsScrollContainer();
    }
}