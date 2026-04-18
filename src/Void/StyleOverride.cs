using Taffy;

namespace Void;

/// <summary>
///     A near 1:1 copy of <see cref="Style" />, but with each field nullable.
///     This allows use to override each field when we want to, but resolve back to the default value in Style when unset.
///     Fields irrelevant to the usage of Taffy in RimWorld are removed.
///     Documentation for each field can be read in <see cref="Style" />
/// </summary>
public class StyleOverride
{
    public AlignContent? alignContent;
    public AlignItems? alignItems;
    public AlignItems? alignSelf;
    public Display? display;
    public Dimension? flexBasis;
    public FlexDirection? flexDirection;
    public float? flexGrow;
    public float? flexShrink;
    public FlexWrap? flexWrap;
    public Size<LengthPercentage>? gap;
    public List<TrackSizingFunction>? gridAutoColumns;
    public GridAutoFlow? gridAutoFlow;
    public List<TrackSizingFunction>? gridAutoRows;
    public Line<GridPlacement>? gridColumn;
    public Line<GridPlacement>? gridRow;
    public List<TrackSizingFunction>? gridTemplateColumns;
    public List<TrackSizingFunction>? gridTemplateRows;
    public Dimension? height;
    public AlignContent? justifyContent;
    public AlignItems? justifyItems;
    public AlignItems? justifySelf;
    public Rect<LengthPercentageAuto>? margin;
    public Dimension? maxHeight;
    public Dimension? maxWidth;
    public Dimension? minHeight;
    public Dimension? minWidth;
    public Rect<LengthPercentage>? padding;
    public Dimension? width;

    /// <summary>
    ///     Returns a new <see cref="StyleOverride" /> where each field is taken from this instance
    ///     when explicitly set (non-null), or from <paramref name="fallback" /> otherwise.
    /// </summary>
    public StyleOverride Merge(StyleOverride fallback)
    {
        return new StyleOverride
        {
            display = display ?? fallback.display,
            width = width ?? fallback.width,
            height = height ?? fallback.height,
            minWidth = minWidth ?? fallback.minWidth,
            minHeight = minHeight ?? fallback.minHeight,
            maxWidth = maxWidth ?? fallback.maxWidth,
            maxHeight = maxHeight ?? fallback.maxHeight,
            margin = margin ?? fallback.margin,
            padding = padding ?? fallback.padding,
            alignItems = alignItems ?? fallback.alignItems,
            alignSelf = alignSelf ?? fallback.alignSelf,
            justifyItems = justifyItems ?? fallback.justifyItems,
            justifySelf = justifySelf ?? fallback.justifySelf,
            alignContent = alignContent ?? fallback.alignContent,
            justifyContent = justifyContent ?? fallback.justifyContent,
            gap = gap ?? fallback.gap,
            flexDirection = flexDirection ?? fallback.flexDirection,
            flexWrap = flexWrap ?? fallback.flexWrap,
            flexBasis = flexBasis ?? fallback.flexBasis,
            flexGrow = flexGrow ?? fallback.flexGrow,
            flexShrink = flexShrink ?? fallback.flexShrink,
            gridTemplateColumns = gridTemplateColumns ?? fallback.gridTemplateColumns,
            gridTemplateRows = gridTemplateRows ?? fallback.gridTemplateRows,
            gridAutoColumns = gridAutoColumns ?? fallback.gridAutoColumns,
            gridAutoRows = gridAutoRows ?? fallback.gridAutoRows,
            gridAutoFlow = gridAutoFlow ?? fallback.gridAutoFlow,
            gridColumn = gridColumn ?? fallback.gridColumn,
            gridRow = gridRow ?? fallback.gridRow
        };
    }

    public Style Resolve()
    {
        var def = new Style(); // Defaults
        return new Style
        {
            display = display ?? def.display,
            size = new Size<Dimension>(width ?? def.size.Width, height ?? def.size.Height),
            minSize = new Size<Dimension>(minWidth ?? def.minSize.Width, minHeight ?? def.minSize.Height),
            maxSize = new Size<Dimension>(maxWidth ?? def.maxSize.Width, maxHeight ?? def.maxSize.Height),
            margin = margin ?? def.margin,
            padding = padding ?? def.padding,
            alignItems = alignItems,
            alignSelf = alignSelf,
            justifyItems = justifyItems,
            justifySelf = justifySelf,
            alignContent = alignContent,
            justifyContent = justifyContent,
            gap = gap ?? def.gap,
            flexDirection = flexDirection ?? def.flexDirection,
            flexWrap = flexWrap ?? def.flexWrap,
            flexBasis = flexBasis ?? def.flexBasis,
            flexGrow = flexGrow ?? def.flexGrow,
            flexShrink = flexShrink ?? def.flexShrink,
            gridTemplateColumns = gridTemplateColumns,
            gridTemplateRows = gridTemplateRows,
            gridAutoColumns = gridAutoColumns ?? def.gridAutoColumns,
            gridAutoRows = gridAutoRows ?? def.gridAutoRows,
            gridAutoFlow = gridAutoFlow ?? def.gridAutoFlow,
            gridColumn = gridColumn ?? def.gridColumn,
            gridRow = gridRow ?? def.gridRow
        };
    }
}