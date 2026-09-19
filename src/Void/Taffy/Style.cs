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
///
///     TODO: Find better name
/// </summary>
public class Style : IEquatable<Style>
{
    public Color? backgroundColor;
    public Color? color;
    public GameFont? fontSize;
    public bool? wordWrap;

    public TaffyOverflow? overflowX;
    public TaffyOverflow? overflowY;
    public TaffyAlignContent? alignContent;
    public TaffyAlignItems? alignItems;
    public TaffyAlignItems? alignSelf;
    public TaffyDisplay? display;
    public TaffyDimension? flexBasis;
    public TaffyFlexDirection? flexDirection;
    public float? flexGrow;
    public float? flexShrink;
    public TaffyFlexWrap? flexWrap;
    public TaffyAxes? gap;
    public TaffyTrackSizingFunction[]? gridAutoColumns;
    public TaffyGridAutoFlow? gridAutoFlow;
    public TaffyTrackSizingFunction[]? gridAutoRows;
    public TaffyGridPlacement? gridColumn;
    public TaffyGridPlacement? gridRow;
    public TaffyTrackSizingFunction[]? gridTemplateColumns;
    public TaffyTrackSizingFunction[]? gridTemplateRows;
    public TaffyDimension? height;
    public TaffyAlignContent? justifyContent;
    public TaffyAlignItems? justifyItems;
    public TaffyAlignItems? justifySelf;
    public TaffyEdges? margin;
    public TaffyDimension? maxHeight;
    public TaffyDimension? maxWidth;
    public TaffyDimension? minHeight;
    public TaffyDimension? minWidth;
    public TaffyEdges? padding;
    public TextAnchor? textAnchor;
    public TaffyDimension? width;

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
            s.ScrollbarWidth = UIUtility.ScrollBarWidth + GenUI.GapTiny;

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
