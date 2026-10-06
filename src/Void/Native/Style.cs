using UnityEngine;
using Verse;
using static Void.Native.NativeMethods;

namespace Void.Native;

/// <summary>
///     A near 1:1 copy of Taffy's style with every field nullable, so callers can
///     override only the fields they care about and fall back to defaults for the rest.
///     Also includes some RimWorld specific fields for general styling, such as fontSize and color.
///     <para>
///         The layout fields are stored directly in the <see cref="StyleDesc" /> that is sent to the native side:
///         setting one writes its value and marks it in <c>set_mask</c>, clearing it (null) resets both.
///     </para>
/// </summary>
public unsafe class Style : IEquatable<Style>
{
    /// <summary>
    ///     Width a scroll container keeps free for its scrollbar while it scrolls.
    /// </summary>
    public const float ScrollbarGutter = UIUtility.ScrollBarWidth + GenUI.GapTiny;

    // Visual fields, only read by the components.
    public Color? backgroundColor;
    public Color? color;
    public GameFont? fontSize;
    public TextAnchor? textAnchor;
    public bool? wordWrap;

    private TaffyTrackSizingFunction[]? _gridAutoColumns;
    private TaffyTrackSizingFunction[]? _gridAutoRows;
    private TaffyTrackSizingFunction[]? _gridTemplateColumns;
    private TaffyTrackSizingFunction[]? _gridTemplateRows;

    internal StyleDesc desc;

    /// <summary>
    ///     Incremented by every layout setter, so a node that saw this instance last frame can tell whether it was
    ///     changed since (see <see cref="NodeHandles" />). Changing a grid track array in place is not noticed.
    /// </summary>
    internal int version;

    // Grid tracks are arrays, which the description can only point to, so they are kept here and attached when
    // the style is registered.
    public TaffyTrackSizingFunction[]? gridAutoColumns { get => _gridAutoColumns; set { _gridAutoColumns = value; version++; } }
    public TaffyTrackSizingFunction[]? gridAutoRows { get => _gridAutoRows; set { _gridAutoRows = value; version++; } }
    public TaffyTrackSizingFunction[]? gridTemplateColumns { get => _gridTemplateColumns; set { _gridTemplateColumns = value; version++; } }
    public TaffyTrackSizingFunction[]? gridTemplateRows { get => _gridTemplateRows; set { _gridTemplateRows = value; version++; } }

    public TaffyDisplay? display { get => Get(STYLE_SET_DISPLAY, (TaffyDisplay)desc.display); set => desc.display = (uint)Set(STYLE_SET_DISPLAY, value); }
    public TaffyFlexDirection? flexDirection { get => Get(STYLE_SET_FLEX_DIRECTION, (TaffyFlexDirection)desc.flex_direction); set => desc.flex_direction = (uint)Set(STYLE_SET_FLEX_DIRECTION, value); }
    public TaffyFlexWrap? flexWrap { get => Get(STYLE_SET_FLEX_WRAP, (TaffyFlexWrap)desc.flex_wrap); set => desc.flex_wrap = (uint)Set(STYLE_SET_FLEX_WRAP, value); }
    public TaffyAlignItems? alignItems { get => Get(STYLE_SET_ALIGN_ITEMS, (TaffyAlignItems)desc.align_items); set => desc.align_items = (uint)Set(STYLE_SET_ALIGN_ITEMS, value); }
    public TaffyAlignItems? alignSelf { get => Get(STYLE_SET_ALIGN_SELF, (TaffyAlignItems)desc.align_self); set => desc.align_self = (uint)Set(STYLE_SET_ALIGN_SELF, value); }
    public TaffyAlignContent? alignContent { get => Get(STYLE_SET_ALIGN_CONTENT, (TaffyAlignContent)desc.align_content); set => desc.align_content = (uint)Set(STYLE_SET_ALIGN_CONTENT, value); }
    public TaffyAlignContent? justifyContent { get => Get(STYLE_SET_JUSTIFY_CONTENT, (TaffyAlignContent)desc.justify_content); set => desc.justify_content = (uint)Set(STYLE_SET_JUSTIFY_CONTENT, value); }
    public TaffyAlignItems? justifyItems { get => Get(STYLE_SET_JUSTIFY_ITEMS, (TaffyAlignItems)desc.justify_items); set => desc.justify_items = (uint)Set(STYLE_SET_JUSTIFY_ITEMS, value); }
    public TaffyAlignItems? justifySelf { get => Get(STYLE_SET_JUSTIFY_SELF, (TaffyAlignItems)desc.justify_self); set => desc.justify_self = (uint)Set(STYLE_SET_JUSTIFY_SELF, value); }
    public TaffyOverflow? overflowX { get => Get(STYLE_SET_OVERFLOW_X, (TaffyOverflow)desc.overflow_x); set => desc.overflow_x = (uint)Set(STYLE_SET_OVERFLOW_X, value); }
    public TaffyOverflow? overflowY { get => Get(STYLE_SET_OVERFLOW_Y, (TaffyOverflow)desc.overflow_y); set => desc.overflow_y = (uint)Set(STYLE_SET_OVERFLOW_Y, value); }
    public TaffyGridAutoFlow? gridAutoFlow { get => Get(STYLE_SET_GRID_AUTO_FLOW, (TaffyGridAutoFlow)desc.grid_auto_flow); set => desc.grid_auto_flow = (uint)Set(STYLE_SET_GRID_AUTO_FLOW, value); }
    public float? flexGrow { get => Get(STYLE_SET_FLEX_GROW, desc.flex_grow); set => desc.flex_grow = Set(STYLE_SET_FLEX_GROW, value); }
    public float? flexShrink { get => Get(STYLE_SET_FLEX_SHRINK, desc.flex_shrink); set => desc.flex_shrink = Set(STYLE_SET_FLEX_SHRINK, value); }
    public TaffyDimension? flexBasis { get => Get(STYLE_SET_FLEX_BASIS, desc.flex_basis); set => desc.flex_basis = Set(STYLE_SET_FLEX_BASIS, value); }
    public TaffyDimension? width { get => Get(STYLE_SET_WIDTH, desc.width); set => desc.width = Set(STYLE_SET_WIDTH, value); }
    public TaffyDimension? height { get => Get(STYLE_SET_HEIGHT, desc.height); set => desc.height = Set(STYLE_SET_HEIGHT, value); }
    public TaffyDimension? minWidth { get => Get(STYLE_SET_MIN_WIDTH, desc.min_width); set => desc.min_width = Set(STYLE_SET_MIN_WIDTH, value); }
    public TaffyDimension? minHeight { get => Get(STYLE_SET_MIN_HEIGHT, desc.min_height); set => desc.min_height = Set(STYLE_SET_MIN_HEIGHT, value); }
    public TaffyDimension? maxWidth { get => Get(STYLE_SET_MAX_WIDTH, desc.max_width); set => desc.max_width = Set(STYLE_SET_MAX_WIDTH, value); }
    public TaffyDimension? maxHeight { get => Get(STYLE_SET_MAX_HEIGHT, desc.max_height); set => desc.max_height = Set(STYLE_SET_MAX_HEIGHT, value); }
    public TaffyAxes? gap { get => Get(STYLE_SET_GAP, desc.gap); set => desc.gap = Set(STYLE_SET_GAP, value); }
    public TaffyEdges? padding { get => Get(STYLE_SET_PADDING, desc.padding); set => desc.padding = Set(STYLE_SET_PADDING, value); }
    public TaffyEdges? margin { get => Get(STYLE_SET_MARGIN, desc.margin); set => desc.margin = Set(STYLE_SET_MARGIN, value); }
    public TaffyGridPlacement? gridColumn { get => Get(STYLE_SET_GRID_COLUMN, desc.grid_column); set => desc.grid_column = Set(STYLE_SET_GRID_COLUMN, value); }
    public TaffyGridPlacement? gridRow { get => Get(STYLE_SET_GRID_ROW, desc.grid_row); set => desc.grid_row = Set(STYLE_SET_GRID_ROW, value); }

    private T? Get<T>(ulong bit, T value) where T : struct
    {
        return (desc.set_mask & bit) != 0 ? value : null;
    }

    /// <returns>The value to store: the given one, or the default when cleared, so unset fields are always zero.</returns>
    private T Set<T>(ulong bit, T? value) where T : struct
    {
        version++;
        if (value.HasValue) desc.set_mask |= bit;
        else desc.set_mask &= ~bit;
        return value.GetValueOrDefault();
    }

    /// <summary>
    ///     Only compares values that may influence layout.
    ///     <para>
    ///         Unset fields are always zero, so two styles with the same layout have identical descriptions, which are
    ///         compared as raw memory. (A <c>-0f</c> and a <c>0f</c> count as different; that only costs a duplicate
    ///         native style.)
    ///     </para>
    /// </summary>
    public bool Equals(Style? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        fixed (StyleDesc* a = &desc)
        fixed (StyleDesc* b = &other.desc)
        {
            var x = (ulong*)a;
            var y = (ulong*)b;
            for (var i = 0; i < sizeof(StyleDesc) / sizeof(ulong); i++)
                if (x[i] != y[i])
                    return false;
        }

        return TracksEqual(gridTemplateColumns, other.gridTemplateColumns)
               && TracksEqual(gridTemplateRows, other.gridTemplateRows)
               && TracksEqual(gridAutoColumns, other.gridAutoColumns)
               && TracksEqual(gridAutoRows, other.gridAutoRows);
    }

    public override bool Equals(object? obj)
    {
        return obj is Style other && Equals(other);
    }

    /// <summary>
    ///     Covers exactly what <see cref="Equals(Style)" /> compares, so equal styles share a hash and a native style.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        fixed (StyleDesc* d = &desc)
        {
            var words = (ulong*)d;
            for (var i = 0; i < sizeof(StyleDesc) / sizeof(ulong); i++) hash.Add(words[i]);
        }

        AddTracks(ref hash, gridTemplateColumns);
        AddTracks(ref hash, gridTemplateRows);
        AddTracks(ref hash, gridAutoColumns);
        AddTracks(ref hash, gridAutoRows);
        return hash.ToHashCode();
    }

    private static void AddTracks(ref HashCode hash, TaffyTrackSizingFunction[]? tracks)
    {
        hash.Add(tracks?.Length ?? -1);
        if (tracks == null) return;
        foreach (var track in tracks) hash.Add(track);
    }

    private static bool TracksEqual(TaffyTrackSizingFunction[]? a, TaffyTrackSizingFunction[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        return a != null && b != null && a.SequenceEqual(b);
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
            overflowX = overflowX ?? fallback.overflowX,
            overflowY = overflowY ?? fallback.overflowY,
            color = color ?? fallback.color,
            backgroundColor = backgroundColor ?? fallback.backgroundColor,
            fontSize = fontSize ?? fallback.fontSize,
            wordWrap = wordWrap ?? fallback.wordWrap,
            textAnchor = textAnchor ?? fallback.textAnchor
        };
    }
}
