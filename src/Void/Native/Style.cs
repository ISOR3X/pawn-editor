using UnityEngine;
using Verse;

namespace Void.Native;

/// <summary>
///     A near 1:1 copy of Taffy's style with every field nullable, so callers can
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

    public override bool Equals(object? obj)
    {
        return obj is Style other && Equals(other);
    }

    /// <summary>
    ///     Covers exactly the fields <see cref="Equals(Style)" /> compares, so equal styles share a hash and a
    ///     native style handle.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var h = 17;

            void Add(int value)
            {
                h = h * 31 + value;
            }

            Add(display.GetHashCode());
            Add(flexDirection.GetHashCode());
            Add(flexWrap.GetHashCode());
            Add(flexGrow.GetHashCode());
            Add(flexShrink.GetHashCode());
            Add(Hash(flexBasis));
            Add(alignItems.GetHashCode());
            Add(alignSelf.GetHashCode());
            Add(alignContent.GetHashCode());
            Add(justifyContent.GetHashCode());
            Add(justifyItems.GetHashCode());
            Add(justifySelf.GetHashCode());
            Add(Hash(width));
            Add(Hash(height));
            Add(Hash(minWidth));
            Add(Hash(minHeight));
            Add(Hash(maxWidth));
            Add(Hash(maxHeight));
            Add(gap is { } g ? Hash(g.Width) * 31 + Hash(g.Height) : 0);
            Add(Hash(padding));
            Add(Hash(margin));
            Add(gridAutoFlow.GetHashCode());
            Add(Hash(gridColumn));
            Add(Hash(gridRow));
            Add(overflowX.GetHashCode());
            Add(overflowY.GetHashCode());
            Add(Hash(gridTemplateColumns));
            Add(Hash(gridTemplateRows));
            Add(Hash(gridAutoColumns));
            Add(Hash(gridAutoRows));
            return h;
        }
    }

    private static int Hash(TaffyDimension d)
    {
        return unchecked(d.value.GetHashCode() * 31 + (int)d.unit);
    }

    private static int Hash(TaffyDimension? d)
    {
        return d is { } v ? Hash(v) : 0;
    }

    private static int Hash(TaffyEdges? e)
    {
        if (e is not { } v) return 0;
        return unchecked(((Hash(v.Top) * 31 + Hash(v.Right)) * 31 + Hash(v.Bottom)) * 31 + Hash(v.Left));
    }

    private static int Hash(TaffyGridPlacement? p)
    {
        return p is { } v ? unchecked((v.start * 31 + v.end) * 31 + v.span) : 0;
    }

    private static int Hash(TaffyTrackSizingFunction[]? tracks)
    {
        if (tracks is null) return 0;
        unchecked
        {
            var h = tracks.Length;
            foreach (var t in tracks) h = (h * 31 + Hash(t.min)) * 31 + Hash(t.max);
            return h;
        }
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
            fontSize = fontSize ?? fallback.fontSize,
            wordWrap = wordWrap ?? fallback.wordWrap,
            textAnchor = textAnchor ?? fallback.textAnchor
        };
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
