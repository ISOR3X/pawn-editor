using Taffy;
using UnityEngine;
using Verse;

namespace Void.Taffy;

/// <summary>
///     A near 1:1 copy of <see cref="TaffyStyleRef" /> with every field nullable, so callers can
///     override only the fields they care about and fall back to defaults for the rest.
///     Also includes some RimWorld specific fields for general styling, usch as fontSize and color.
///
///     Fields irrelevant to RimWorld usage are removed. (TODO: Which ones?)
/// </summary>
public class Style : IEquatable<Style>
{
    /// <summary>
    ///     Width a scroll container keeps free for its scrollbar while it scrolls.
    /// </summary>
    public const float ScrollbarGutter = UIUtility.ScrollBarWidth + GenUI.GapTiny;

    /// <summary>
    ///     Bumped by every change to a property, so a style that is the same instance with the same version is known to
    ///     be unchanged without comparing its fields.
    /// </summary>
    internal int version;

    private Color? _backgroundColor;
    public Color? backgroundColor { get => _backgroundColor; set { _backgroundColor = value; version++; } }
    private Color? _color;
    public Color? color { get => _color; set { _color = value; version++; } }
    private GameFont? _fontSize;
    public GameFont? fontSize { get => _fontSize; set { _fontSize = value; version++; } }
    private bool? _wordWrap;
    public bool? wordWrap { get => _wordWrap; set { _wordWrap = value; version++; } }
    private TaffyOverflow? _overflowX;
    public TaffyOverflow? overflowX { get => _overflowX; set { _overflowX = value; version++; } }
    private TaffyOverflow? _overflowY;
    public TaffyOverflow? overflowY { get => _overflowY; set { _overflowY = value; version++; } }
    private TaffyAlignContent? _alignContent;
    public TaffyAlignContent? alignContent { get => _alignContent; set { _alignContent = value; version++; } }
    private TaffyAlignItems? _alignItems;
    public TaffyAlignItems? alignItems { get => _alignItems; set { _alignItems = value; version++; } }
    private TaffyAlignItems? _alignSelf;
    public TaffyAlignItems? alignSelf { get => _alignSelf; set { _alignSelf = value; version++; } }
    private TaffyDisplay? _display;
    public TaffyDisplay? display { get => _display; set { _display = value; version++; } }
    private TaffyDimension? _flexBasis;
    public TaffyDimension? flexBasis { get => _flexBasis; set { _flexBasis = value; version++; } }
    private TaffyFlexDirection? _flexDirection;
    public TaffyFlexDirection? flexDirection { get => _flexDirection; set { _flexDirection = value; version++; } }
    private float? _flexGrow;
    public float? flexGrow { get => _flexGrow; set { _flexGrow = value; version++; } }
    private float? _flexShrink;
    public float? flexShrink { get => _flexShrink; set { _flexShrink = value; version++; } }
    private TaffyFlexWrap? _flexWrap;
    public TaffyFlexWrap? flexWrap { get => _flexWrap; set { _flexWrap = value; version++; } }
    private TaffyAxes? _gap;
    public TaffyAxes? gap { get => _gap; set { _gap = value; version++; } }
    private TaffyTrackSizingFunction[]? _gridAutoColumns;
    public TaffyTrackSizingFunction[]? gridAutoColumns { get => _gridAutoColumns; set { _gridAutoColumns = value; version++; } }
    private TaffyGridAutoFlow? _gridAutoFlow;
    public TaffyGridAutoFlow? gridAutoFlow { get => _gridAutoFlow; set { _gridAutoFlow = value; version++; } }
    private TaffyTrackSizingFunction[]? _gridAutoRows;
    public TaffyTrackSizingFunction[]? gridAutoRows { get => _gridAutoRows; set { _gridAutoRows = value; version++; } }
    private TaffyGridPlacement? _gridColumn;
    public TaffyGridPlacement? gridColumn { get => _gridColumn; set { _gridColumn = value; version++; } }
    private TaffyGridPlacement? _gridRow;
    public TaffyGridPlacement? gridRow { get => _gridRow; set { _gridRow = value; version++; } }
    private TaffyTrackSizingFunction[]? _gridTemplateColumns;
    public TaffyTrackSizingFunction[]? gridTemplateColumns { get => _gridTemplateColumns; set { _gridTemplateColumns = value; version++; } }
    private TaffyTrackSizingFunction[]? _gridTemplateRows;
    public TaffyTrackSizingFunction[]? gridTemplateRows { get => _gridTemplateRows; set { _gridTemplateRows = value; version++; } }
    private TaffyDimension? _height;
    public TaffyDimension? height { get => _height; set { _height = value; version++; } }
    private TaffyAlignContent? _justifyContent;
    public TaffyAlignContent? justifyContent { get => _justifyContent; set { _justifyContent = value; version++; } }
    private TaffyAlignItems? _justifyItems;
    public TaffyAlignItems? justifyItems { get => _justifyItems; set { _justifyItems = value; version++; } }
    private TaffyAlignItems? _justifySelf;
    public TaffyAlignItems? justifySelf { get => _justifySelf; set { _justifySelf = value; version++; } }
    private TaffyEdges? _margin;
    public TaffyEdges? margin { get => _margin; set { _margin = value; version++; } }
    private TaffyDimension? _maxHeight;
    public TaffyDimension? maxHeight { get => _maxHeight; set { _maxHeight = value; version++; } }
    private TaffyDimension? _maxWidth;
    public TaffyDimension? maxWidth { get => _maxWidth; set { _maxWidth = value; version++; } }
    private TaffyDimension? _minHeight;
    public TaffyDimension? minHeight { get => _minHeight; set { _minHeight = value; version++; } }
    private TaffyDimension? _minWidth;
    public TaffyDimension? minWidth { get => _minWidth; set { _minWidth = value; version++; } }
    private TaffyEdges? _padding;
    public TaffyEdges? padding { get => _padding; set { _padding = value; version++; } }
    private TextAnchor? _textAnchor;
    public TextAnchor? textAnchor { get => _textAnchor; set { _textAnchor = value; version++; } }
    private TaffyDimension? _width;
    public TaffyDimension? width { get => _width; set { _width = value; version++; } }

    /// <summary>
    ///     Only compares values that may influence layout.
    /// </summary>
    public bool Equals(Style other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return display == other.display
               && flexDirection == other.flexDirection
               && flexWrap == other.flexWrap
               && flexGrow == other.flexGrow
               && flexShrink == other.flexShrink
               && flexBasis == other.flexBasis
               && alignItems == other.alignItems
               && alignSelf == other.alignSelf
               && alignContent == other.alignContent
               && justifyContent == other.justifyContent
               && justifyItems == other.justifyItems
               && justifySelf == other.justifySelf
               && width == other.width
               && height == other.height
               && minWidth == other.minWidth
               && minHeight == other.minHeight
               && maxWidth == other.maxWidth
               && maxHeight == other.maxHeight
               && gap == other.gap
               && padding == other.padding
               && margin == other.margin
               && gridAutoFlow == other.gridAutoFlow
               && gridColumn == other.gridColumn
               && gridRow == other.gridRow
               && overflowX == other.overflowX
               && overflowY == other.overflowY
               && SequenceEqualNullable(gridTemplateColumns, other.gridTemplateColumns)
               && SequenceEqualNullable(gridTemplateRows, other.gridTemplateRows)
               && SequenceEqualNullable(gridAutoColumns, other.gridAutoColumns)
               && SequenceEqualNullable(gridAutoRows, other.gridAutoRows);
    }

    /// <summary>
    ///     Returns a new <see cref="Style" /> where each field is taken from this instance
    ///     when non-null, or from <paramref name="fallback" /> otherwise.
    /// </summary>
    public Style Merge(Style fallback)
    {
        return new Style
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
            gridRow = gridRow ?? fallback.gridRow,
            overflowX  = overflowX ?? fallback.overflowX,
            overflowY = overflowY ?? fallback.overflowY,
            color = color ?? fallback.color,
            backgroundColor = backgroundColor ?? fallback.backgroundColor,
            fontSize = fontSize ?? fallback.fontSize
        };
    }

    /// <summary>
    ///     Applies all non-null fields to <paramref name="s" />.
    /// </summary>
    public void Push(TaffyStyleRef s)
    {
        if (display.HasValue) s.Display = display.Value;
        if (flexDirection.HasValue) s.FlexDirection = flexDirection.Value;
        if (flexWrap.HasValue) s.FlexWrap = flexWrap.Value;
        if (flexGrow.HasValue) s.FlexGrow = flexGrow.Value;
        if (flexShrink.HasValue) s.FlexShrink = flexShrink.Value;
        if (flexBasis.HasValue) s.FlexBasis = flexBasis.Value;
        if (alignItems.HasValue) s.AlignItems = alignItems;
        if (alignSelf.HasValue) s.AlignSelf = alignSelf;
        if (alignContent.HasValue) s.AlignContent = alignContent;
        if (justifyContent.HasValue) s.JustifyContent = justifyContent;
        if (justifyItems.HasValue) s.JustifyItems = justifyItems;
        if (justifySelf.HasValue) s.JustifySelf = justifySelf;
        if (width.HasValue) s.Width = width.Value;
        if (height.HasValue) s.Height = height.Value;
        if (minWidth.HasValue) s.MinWidth = minWidth.Value;
        if (minHeight.HasValue) s.MinHeight = minHeight.Value;
        if (maxWidth.HasValue) s.MaxWidth = maxWidth.Value;
        if (maxHeight.HasValue) s.MaxHeight = maxHeight.Value;
        if (gap.HasValue)
        {
            // None unit = "not specified by caller". Skip to preserve the native default.
            if (gap.Value.Width.unit != TaffyUnit.None) s.ColumnGap = gap.Value.Width;
            if (gap.Value.Height.unit != TaffyUnit.None) s.RowGap = gap.Value.Height;
        }

        if (padding.HasValue)
        {
            if (padding.Value.Top.unit != TaffyUnit.None) s.PaddingTop = padding.Value.Top;
            if (padding.Value.Right.unit != TaffyUnit.None) s.PaddingRight = padding.Value.Right;
            if (padding.Value.Bottom.unit != TaffyUnit.None) s.PaddingBottom = padding.Value.Bottom;
            if (padding.Value.Left.unit != TaffyUnit.None) s.PaddingLeft = padding.Value.Left;
        }

        if (margin.HasValue)
        {
            if (margin.Value.Top.unit != TaffyUnit.None) s.MarginTop = margin.Value.Top;
            if (margin.Value.Right.unit != TaffyUnit.None) s.MarginRight = margin.Value.Right;
            if (margin.Value.Bottom.unit != TaffyUnit.None) s.MarginBottom = margin.Value.Bottom;
            if (margin.Value.Left.unit != TaffyUnit.None) s.MarginLeft = margin.Value.Left;
        }

        if (overflowX.HasValue) s.OverflowX = overflowX.Value;
        if (overflowY.HasValue) s.OverflowY = overflowY.Value;

        // We do not expose scrollbar width as in RimWorld it only has one width.
        // So we only apply the value when scrolling is required.
        if (overflowX == TaffyOverflow.Scroll || overflowY == TaffyOverflow.Scroll)
            s.ScrollbarWidth = ScrollbarGutter;

        if (gridAutoFlow.HasValue) s.GridAutoFlow = gridAutoFlow.Value;
        if (gridColumn.HasValue) s.GridColumn = gridColumn.Value;
        if (gridRow.HasValue) s.GridRow = gridRow.Value;
        if (gridTemplateColumns != null) s.SetGridTemplateColumns(gridTemplateColumns);
        if (gridTemplateRows != null) s.SetGridTemplateRows(gridTemplateRows);
        if (gridAutoColumns != null) s.SetGridAutoColumns(gridAutoColumns);
        if (gridAutoRows != null) s.SetGridAutoRows(gridAutoRows);
    }

    private static bool SequenceEqualNullable(TaffyTrackSizingFunction[]? a, TaffyTrackSizingFunction[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }
}
