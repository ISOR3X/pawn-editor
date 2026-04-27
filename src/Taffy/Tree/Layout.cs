// Port of taffy/src/tree/layout.rs

namespace Taffy;
// ── RunMode ───────────────────────────────────────────────────────────────

/// <summary>Whether to compute a full layout or only measure the node's size.</summary>
public enum RunMode : byte
{
    /// <summary>A full layout pass for this node and all its children.</summary>
    PerformLayout,

    /// <summary>
    ///     Execute the algorithm only far enough to determine the node's container size.
    ///     Steps not needed for sizing may be skipped.
    /// </summary>
    ComputeSize,

    /// <summary>Set a null layout - the node is hidden (<c>Display::None</c>).</summary>
    PerformHiddenLayout
}

// ── SizingMode ────────────────────────────────────────────────────────────

/// <summary>Whether style sizes should be considered when computing size.</summary>
public enum SizingMode : byte
{
    /// <summary>Only content contributions are considered.</summary>
    ContentSize,

    /// <summary>Inherent size styles are considered in addition to content.</summary>
    InherentSize
}

// ── RequestedAxis ─────────────────────────────────────────────────────────

/// <summary>Which axis a layout algorithm should compute a size for.</summary>
public enum RequestedAxis : byte
{
    Horizontal,
    Vertical,
    Both
}

public static class RequestedAxisExt
{
    public static RequestedAxis FromAbsolute(AbsoluteAxis axis)
    {
        return axis == AbsoluteAxis.Horizontal ? RequestedAxis.Horizontal : RequestedAxis.Vertical;
    }

    /// <summary>Converts to <see cref="AbsoluteAxis" />; returns false if Both.</summary>
    public static bool TryToAbsolute(this RequestedAxis r, out AbsoluteAxis axis)
    {
        if (r == RequestedAxis.Horizontal)
        {
            axis = AbsoluteAxis.Horizontal;
            return true;
        }

        if (r == RequestedAxis.Vertical)
        {
            axis = AbsoluteAxis.Vertical;
            return true;
        }

        axis = default;
        return false;
    }
}

// ── CollapsibleMarginSet ──────────────────────────────────────────────────

/// <summary>A set of margins available for block-layout margin collapsing.</summary>
public struct CollapsibleMarginSet
{
    public static readonly CollapsibleMarginSet ZERO = new(0f, 0f);

    private float _positive;
    private float _negative;

    private CollapsibleMarginSet(float positive, float negative)
    {
        _positive = positive;
        _negative = negative;
    }

    public static CollapsibleMarginSet FromMargin(float margin)
    {
        return margin >= 0f
            ? new CollapsibleMarginSet(margin, 0f)
            : new CollapsibleMarginSet(0f, margin);
    }

    public CollapsibleMarginSet CollapseWithMargin(float margin)
    {
        if (margin >= 0f) _positive = MathF.Max(_positive, margin);
        else _negative = MathF.Min(_negative, margin);
        return this;
    }

    public CollapsibleMarginSet CollapseWithSet(CollapsibleMarginSet other)
    {
        _positive = MathF.Max(_positive, other._positive);
        _negative = MathF.Min(_negative, other._negative);
        return this;
    }

    /// <summary>The net margin after all collapsing: positive + negative.</summary>
    public float Resolve()
    {
        return _positive + _negative;
    }
}

// ── LayoutInput ───────────────────────────────────────────────────────────

/// <summary>
///     Input constraints/hints passed from a parent to a child layout algorithm.
/// </summary>
public struct LayoutInput
{
    public RunMode RunMode;
    public SizingMode SizingMode;
    public RequestedAxis Axis;
    public Size<float?> KnownDimensions;
    public Size<float?> ParentSize;
    public Size<AvailableSpace> availableSpace;
    public Line<bool> VerticalMarginsAreCollapsible;

    /// <summary>A LayoutInput that triggers hidden layout.</summary>
    public static LayoutInput Hidden => new()
    {
        RunMode = RunMode.PerformHiddenLayout,
        KnownDimensions = new Size<float?>(null, null),
        ParentSize = new Size<float?>(null, null),
        availableSpace = new Size<AvailableSpace>(AvailableSpace.MaxContent, AvailableSpace.MaxContent),
        SizingMode = SizingMode.InherentSize,
        Axis = RequestedAxis.Both,
        VerticalMarginsAreCollapsible = new Line<bool>(false, false)
    };
}

// ── LayoutOutput ──────────────────────────────────────────────────────────

/// <summary>The result of laying out a single node, returned to its parent.</summary>
public struct LayoutOutput
{
    /// <summary>The computed size of the node.</summary>
    public Size<float> Size;

    /// <summary>The size of the content within the node (may exceed Size for scrollable nodes).</summary>
    public Size<float> ContentSize;

    /// <summary>The first baseline in each dimension, if any.</summary>
    public Point<float?> FirstBaselines;

    /// <summary>Top margin available for collapsing (block layout). Zero for other modes.</summary>
    public CollapsibleMarginSet TopMargin;

    /// <summary>Bottom margin available for collapsing (block layout). Zero for other modes.</summary>
    public CollapsibleMarginSet BottomMargin;

    /// <summary>Whether margins can collapse through this node (block layout).</summary>
    public bool MarginsCanCollapseThrough;

    public static LayoutOutput Hidden => new()
    {
        Size = SizeF.ZERO,
        ContentSize = SizeF.ZERO,
        FirstBaselines = new Point<float?>(null, null),
        TopMargin = CollapsibleMarginSet.ZERO,
        BottomMargin = CollapsibleMarginSet.ZERO,
        MarginsCanCollapseThrough = false
    };

    public static LayoutOutput FromSizesAndBaselines(
        Size<float> size, Size<float> contentSize, Point<float?> firstBaselines)
    {
        return new LayoutOutput
        {
            Size = size,
            ContentSize = contentSize,
            FirstBaselines = firstBaselines,
            TopMargin = CollapsibleMarginSet.ZERO,
            BottomMargin = CollapsibleMarginSet.ZERO,
            MarginsCanCollapseThrough = false
        };
    }

    public static LayoutOutput FromSizes(Size<float> size, Size<float> contentSize)
    {
        return FromSizesAndBaselines(size, contentSize, new Point<float?>(null, null));
    }

    public static LayoutOutput FromOuterSize(Size<float> size)
    {
        return FromSizes(size, SizeF.ZERO);
    }
}

// ── Layout ────────────────────────────────────────────────────────────────

/// <summary>The final computed layout result stored on each node.</summary>
public struct Layout
{
    /// <summary>
    ///     Render order. Higher values are drawn on top.
    ///     Effectively a topological sort position within the tree.
    /// </summary>
    public uint Order;

    /// <summary>The top-left corner of the node relative to its parent.</summary>
    public Point<float> Location;

    /// <summary>The width and height of the node.</summary>
    public Size<float> Size;

    /// <summary>
    ///     The size of the content inside the node.
    ///     May exceed <see cref="Size" /> for overflowing/scrollable nodes.
    /// </summary>
    public Size<float> ContentSize;

    /// <summary>Space reserved for scrollbars in each axis.</summary>
    public Size<float> ScrollbarSize;

    /// <summary>Resolved border widths on each side.</summary>
    public Rect<float> Border;

    /// <summary>Resolved padding widths on each side.</summary>
    public Rect<float> Padding;

    /// <summary>Resolved margin widths on each side.</summary>
    public Rect<float> Margin;

    public static Layout New()
    {
        return new Layout
        {
            Order = 0,
            Location = PointF.ZERO,
            Size = SizeF.ZERO,
            ContentSize = SizeF.ZERO,
            ScrollbarSize = SizeF.ZERO,
            Border = RectF.ZERO,
            Padding = RectF.ZERO,
            Margin = RectF.ZERO
        };
    }

    public static Layout WithOrder(uint order)
    {
        var l = New();
        l.Order = order;
        return l;
    }

    /// <summary>Width of the content box (size minus padding and border).</summary>
    public float ContentBoxWidth =>
        Size.Width - Padding.Left - Padding.Right - Border.Left - Border.Right;

    /// <summary>Height of the content box (size minus padding and border).</summary>
    public float ContentBoxHeight =>
        Size.Height - Padding.Top - Padding.Bottom - Border.Top - Border.Bottom;
}