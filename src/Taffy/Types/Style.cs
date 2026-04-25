// Port of taffy/src/style/mod.rs (Style struct)

namespace Taffy;

/// <summary>
///     The complete CSS style for a single layout node.
///     Set properties on this struct and pass it to <see cref="TaffyTree" />.
/// </summary>
public class Style
{
    /// <summary>Distribution of space in the cross/block axis. Default: null.</summary>
    public AlignContent? alignContent = null;

    // ── Alignment (Flex + Grid) ───────────────────────────────────────────

    /// <summary>Alignment of children in the cross/block axis. Default: null (stretch).</summary>
    public AlignItems? alignItems = null;

    /// <summary>Overrides parent's AlignItems for this child. Default: null.</summary>
    public AlignItems? alignSelf = null;

    /// <summary>Preferred aspect ratio (width / height). Default: null (no constraint).</summary>
    public float? aspectRatio = null;

    /// <summary>Border width on each side. Default: 0 on all sides.</summary>
    public Rect<LengthPercentage> border = RectLP.Zero();

    /// <summary>Whether the node generates a box. Default: Normal.</summary>
    public BoxGenerationMode boxGenerationMode = BoxGenerationMode.Normal;

    // ── Box model ─────────────────────────────────────────────────────────

    /// <summary>Whether size styles apply to the content box or border box. Default: BorderBox.</summary>
    public BoxSizing boxSizing = BoxSizing.BorderBox;

    /// <summary>Text direction. Default: Ltr.</summary>
    public Direction direction = Direction.Ltr;
    // ── Box generation ────────────────────────────────────────────────────

    /// <summary>Which layout algorithm to use for this node's children. Default: Flex.</summary>
    public Display display = Display.Flex;

    // ── Flexbox item ──────────────────────────────────────────────────────

    /// <summary>Initial main-axis size of this flex item. Default: auto.</summary>
    public Dimension flexBasis = Dimension.AUTO;

    // ── Flexbox container ─────────────────────────────────────────────────

    /// <summary>Main axis direction. Default: Row.</summary>
    public FlexDirection flexDirection = FlexDirection.Row;

    /// <summary>Flex grow factor. Default: 0.</summary>
    public float flexGrow = 0f;

    /// <summary>Flex shrink factor. Default: 1.</summary>
    public float flexShrink = 1f;

    /// <summary>Whether items wrap to new lines. Default: NoWrap.</summary>
    public FlexWrap flexWrap = FlexWrap.NoWrap;

    /// <summary>Gap between flex/grid items. Default: 0 on both axes.</summary>
    public Size<LengthPercentage> gap = SizeLP.Zero();

    /// <summary>Sizing for implicitly-created columns (cycled). Defaults to single auto track.</summary>
    public List<TrackSizingFunction> gridAutoColumns = new() { TrackSizingFunction.Auto() };

    /// <summary>Controls how auto-placed items are inserted into the grid. Default: Row.</summary>
    public GridAutoFlow gridAutoFlow = GridAutoFlow.Row;

    /// <summary>Sizing for implicitly-created rows (cycled). Defaults to single auto track.</summary>
    public List<TrackSizingFunction> gridAutoRows = new() { TrackSizingFunction.Auto() };

    // ── Grid item ─────────────────────────────────────────────────────────

    /// <summary>Column placement (start/end). Default: Auto/Auto.</summary>
    public Line<GridPlacement> gridColumn = new(GridPlacement.Auto, GridPlacement.Auto);

    /// <summary>Row placement (start/end). Default: Auto/Auto.</summary>
    public Line<GridPlacement> gridRow = new(GridPlacement.Auto, GridPlacement.Auto);

    // ── Grid container ────────────────────────────────────────────────────

    /// <summary>Explicit grid template for columns. Null means no explicit column template.</summary>
    public List<GridTemplateComponent>? gridTemplateColumns = null;

    /// <summary>Explicit grid template for rows. Null means no explicit row template.</summary>
    public List<GridTemplateComponent>? gridTemplateRows = null;

    /// <summary>Inset offsets (top/right/bottom/left). Default: auto on all sides.</summary>
    public Rect<LengthPercentageAuto> inset = RectLPA.Auto();

    /// <summary>True for replaced elements (e.g. images); affects automatic min-size.</summary>
    public bool itemIsReplaced = false;

    /// <summary>True if the item is a table element (affects block layout).</summary>
    public bool itemIsTable = false;

    /// <summary>Distribution of space in the main/inline axis. Default: null.</summary>
    public AlignContent? justifyContent = null;

    /// <summary>Alignment of children in the inline axis (Grid only). Default: null.</summary>
    public AlignItems? justifyItems = null;

    /// <summary>Overrides parent's JustifyItems for this child (Grid only). Default: null.</summary>
    public AlignItems? justifySelf = null;

    // ── Spacing ───────────────────────────────────────────────────────────

    /// <summary>Margin on each side. Default: 0 on all sides.</summary>
    public Rect<LengthPercentageAuto> margin = RectLPA.Zero();

    /// <summary>Maximum size. Default: auto on both axes.</summary>
    public Size<Dimension> maxSize = SizeDim.Auto();

    /// <summary>Minimum size. Default: auto on both axes.</summary>
    public Size<Dimension> minSize = SizeDim.Auto();

    // ── Overflow ──────────────────────────────────────────────────────────

    /// <summary>How x/y overflow affects layout. Default: Visible on both axes.</summary>
    public Point<Overflow> overflow = new(Overflow.Visible, Overflow.Visible);

    /// <summary>Padding on each side. Default: 0 on all sides.</summary>
    public Rect<LengthPercentage> padding = RectLP.Zero();

    // ── Position ──────────────────────────────────────────────────────────

    /// <summary>Positioning strategy. Default: Relative.</summary>
    public Position position = Position.Relative;

    /// <summary>Space to reserve for scrollbars on Scroll nodes (in abstract units).</summary>
    public float scrollbarWidth = 0f;

    // ── Size ──────────────────────────────────────────────────────────────

    /// <summary>Preferred size. Default: auto on both axes.</summary>
    public Size<Dimension> size = SizeDim.Auto();

    // ── Block container ───────────────────────────────────────────────────

    /// <summary>Legacy block text-align for children. Default: Auto.</summary>
    public TextAlign textAlign = TextAlign.Auto;

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Returns true if this node uses block layout (Display.Block).</summary>
    public bool IsBlock()
    {
        return display == Display.Block;
    }

    /// <summary>Returns a <see cref="Style" /> with all default values.</summary>
    public static Style Default()
    {
        return new Style();
    }

    /// <summary>Creates a shallow copy of this style.</summary>
    public Style Clone()
    {
        return (Style)MemberwiseClone();
    }
}

// ── Convenience factories for Rect/Size with typed contents ──────────────

internal static class RectLP
{
    public static Rect<LengthPercentage> Zero()
    {
        return new Rect<LengthPercentage>(LengthPercentage.ZERO, LengthPercentage.ZERO,
            LengthPercentage.ZERO, LengthPercentage.ZERO);
    }
}

internal static class RectLPA
{
    public static Rect<LengthPercentageAuto> Zero()
    {
        return new Rect<LengthPercentageAuto>(LengthPercentageAuto.ZERO, LengthPercentageAuto.ZERO,
            LengthPercentageAuto.ZERO, LengthPercentageAuto.ZERO);
    }

    public static Rect<LengthPercentageAuto> Auto()
    {
        return new Rect<LengthPercentageAuto>(LengthPercentageAuto.AUTO, LengthPercentageAuto.AUTO,
            LengthPercentageAuto.AUTO, LengthPercentageAuto.AUTO);
    }
}

internal static class SizeDim
{
    public static Size<Dimension> Auto()
    {
        return new Size<Dimension>(Dimension.AUTO, Dimension.AUTO);
    }

    public static Size<Dimension> Zero()
    {
        return new Size<Dimension>(Dimension.ZERO, Dimension.ZERO);
    }
}

internal static class SizeLP
{
    public static Size<LengthPercentage> Zero()
    {
        return new Size<LengthPercentage>(LengthPercentage.ZERO, LengthPercentage.ZERO);
    }
}