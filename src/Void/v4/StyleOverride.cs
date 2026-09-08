using Taffy;
using UnityEngine;
using Verse;

namespace Void.v4;

/// <summary>
///     A near 1:1 copy of <see cref="TaffyStyleRef" /> with every field nullable, so callers can
///     override only the fields they care about and fall back to defaults for the rest.
///     Fields irrelevant to RimWorld usage are removed. (TODO: Which ones?)
///     TODO: Find better name
/// </summary>
public class StyleOverride : IEquatable<StyleOverride>
{
    // Not used by Taffy — stored here so UI code can read colors alongside layout.
    public Color? color;
    public Color? backgroundColor;
    public GameFont? fontSize;

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
    public TaffyDimension? width;

    /// <summary>
    ///     Returns a new <see cref="StyleOverride" /> where each field is taken from this instance
    ///     when non-null, or from <paramref name="fallback" /> otherwise.
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
            gridRow = gridRow ?? fallback.gridRow,

            color = color ?? fallback.color,
            backgroundColor = backgroundColor ?? fallback.backgroundColor,
            fontSize = fontSize ?? fallback.fontSize
        };
    }

    /// <summary>
    ///     Applies all non-null fields to <paramref name="s" />.
    /// </summary>
    public unsafe void Push(TaffyStyleRef s)
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
            // None unit = "not specified by caller" — skip to preserve the native default.
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
        if (gridAutoFlow.HasValue) s.GridAutoFlow = gridAutoFlow.Value;
        if (gridColumn.HasValue) s.GridColumn = gridColumn.Value;
        if (gridRow.HasValue) s.GridRow = gridRow.Value;
        if (gridTemplateColumns != null) s.SetGridTemplateColumns(gridTemplateColumns);
        if (gridTemplateRows != null) s.SetGridTemplateRows(gridTemplateRows);
        if (gridAutoColumns != null) s.SetGridAutoColumns(gridAutoColumns);
        if (gridAutoRows != null) s.SetGridAutoRows(gridAutoRows);
    }

    /// <summary>
    /// Only compares values that may influence layout
    /// </summary>
    public bool Equals(StyleOverride other)
    {
        return false;
    }
}
