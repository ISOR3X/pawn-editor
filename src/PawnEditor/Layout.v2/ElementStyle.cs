// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace FlexLayout;

public enum Display
{
    Block, // default — no layout algorithm applied to children (CSS default)
    Flex // children are laid out using the flexbox algorithm
}

public enum FlexDirection
{
    Row, // main axis = horizontal, left → right
    RowReverse, // main axis = horizontal, right → left
    Column, // main axis = vertical,   top  → bottom
    ColumnReverse // main axis = vertical,   bottom → top
}

public enum FlexWrap
{
    NoWrap, // single line, may overflow
    Wrap, // wrap to new lines
    WrapReverse // wrap to new lines, reversed
}

/// <summary>
///     Unified style for any layout element or container, mirroring how CSS applies
///     properties to elements regardless of their role. Properties irrelevant to a
///     given context (e.g. flexDirection on a leaf item) are simply ignored by the solver.
/// </summary>
public struct ElementStyle(
    float? flexGrow = null,
    float? flexShrink = null,
    StyleSize? flexBasis = null,
    StyleSize? width = null,
    StyleSize? height = null,
    StyleSize? minWidth = null,
    StyleSize? minHeight = null,
    StyleSize? maxWidth = null,
    StyleSize? maxHeight = null,
    int? order = null,
    FlexDirection? flexDirection = null,
    FlexWrap? flexWrap = null,
    float? columnGap = null,
    float? rowGap = null,
    Display? display = null)
{
    public StyleSize width = width ?? StyleSize.Auto();
    public StyleSize height = height ?? StyleSize.Auto();

    public StyleSize minWidth = minWidth ?? StyleSize.Px(0f);
    public StyleSize minHeight = minHeight ?? StyleSize.Px(0f);

    public StyleSize maxWidth = maxWidth ?? StyleSize.Auto();
    public StyleSize maxHeight = maxHeight ?? StyleSize.Auto();

    public Display display = display ?? Display.Block;

    public FlexDirection flexDirection = flexDirection ?? FlexDirection.Column;
    public FlexWrap flexWrap = flexWrap ?? FlexWrap.NoWrap;
    public float flexGrow = flexGrow ?? 0f;
    public float flexShrink = flexShrink ?? 1f;
    public StyleSize flexBasis = flexBasis ?? StyleSize.Auto();

    public int order = order ?? 0;
    public float columnGap = columnGap ?? 0f;
    public float rowGap = rowGap ?? 0f;

    #region PRESETS

    /// <summary>Grows to fill available space. Equivalent to <c>flex-grow: 1</c>.</summary>
    public static ElementStyle Fill => new(1f, 0f);

    /// <summary>Fixed width, does not shrink.</summary>
    public static ElementStyle FixedWidth(float width)
    {
        return new ElementStyle(0f, 0f, width: width);
    }

    /// <summary>Fixed height, does not shrink.</summary>
    public static ElementStyle FixedHeight(float height)
    {
        return new ElementStyle(0f, 0f, height: height);
    }

    #endregion
}

public static class ElementStyleUtility
{
    public static ElementStyle With(
        this ElementStyle self,
        float? flexGrow = null,
        float? flexShrink = null,
        StyleSize? flexBasis = null,
        StyleSize? width = null,
        StyleSize? height = null,
        StyleSize? minWidth = null,
        StyleSize? minHeight = null,
        StyleSize? maxWidth = null,
        StyleSize? maxHeight = null,
        int? order = null,
        FlexDirection? flexDirection = null,
        FlexWrap? flexWrap = null,
        float? columnGap = null,
        float? rowGap = null,
        Display? display = null
    )
    {
        return new ElementStyle(
            flexGrow ?? self.flexGrow,
            flexShrink ?? self.flexShrink,
            flexBasis ?? self.flexBasis,
            width ?? self.width,
            height ?? self.height,
            minWidth ?? self.minWidth,
            minHeight ?? self.minHeight,
            maxWidth ?? self.maxWidth,
            maxHeight ?? self.maxHeight,
            order ?? self.order,
            flexDirection ?? self.flexDirection,
            flexWrap ?? self.flexWrap,
            columnGap ?? self.columnGap,
            rowGap ?? self.rowGap,
            display ?? self.display
        );
    }
}