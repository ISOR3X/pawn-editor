using static Void.Native.NativeMethods;

namespace Void.Native;

/// <summary>
///     Interns layout styles: each distinct style (by <see cref="Style.Equals(Style)" />, so layout fields only)
///     is sent to the native side once and referred to by a handle afterwards. Handle 0 is the default style.
/// </summary>
internal static unsafe class StyleRegistry
{
    private static readonly Dictionary<Style, uint> Handles = [];

    public static int Count => Handles.Count;

    public static uint Get(Style? style)
    {
        if (style == null) return 0;
        if (Handles.TryGetValue(style, out var handle)) return handle;

        // The key must never change while it is in the dictionary, and callers are free to mutate their
        // instance (or the arrays it shares) later, so store a private copy.
        var key = Copy(style);
        handle = Register(key);
        Handles[key] = handle;
        return handle;
    }

    private static Style Copy(Style style)
    {
        var copy = style.Merge(new Style());
        copy.gridTemplateColumns = (TaffyTrackSizingFunction[]?)style.gridTemplateColumns?.Clone();
        copy.gridTemplateRows = (TaffyTrackSizingFunction[]?)style.gridTemplateRows?.Clone();
        copy.gridAutoColumns = (TaffyTrackSizingFunction[]?)style.gridAutoColumns?.Clone();
        copy.gridAutoRows = (TaffyTrackSizingFunction[]?)style.gridAutoRows?.Clone();
        return copy;
    }

    private static uint Register(Style s)
    {
        var d = new StyleDesc();

        if (s.display.HasValue) { d.set_mask |= STYLE_SET_DISPLAY; d.display = (uint)s.display.Value; }
        if (s.flexDirection.HasValue) { d.set_mask |= STYLE_SET_FLEX_DIRECTION; d.flex_direction = (uint)s.flexDirection.Value; }
        if (s.flexWrap.HasValue) { d.set_mask |= STYLE_SET_FLEX_WRAP; d.flex_wrap = (uint)s.flexWrap.Value; }
        if (s.alignItems.HasValue) { d.set_mask |= STYLE_SET_ALIGN_ITEMS; d.align_items = (uint)s.alignItems.Value; }
        if (s.alignSelf.HasValue) { d.set_mask |= STYLE_SET_ALIGN_SELF; d.align_self = (uint)s.alignSelf.Value; }
        if (s.alignContent.HasValue) { d.set_mask |= STYLE_SET_ALIGN_CONTENT; d.align_content = (uint)s.alignContent.Value; }
        if (s.justifyContent.HasValue) { d.set_mask |= STYLE_SET_JUSTIFY_CONTENT; d.justify_content = (uint)s.justifyContent.Value; }
        if (s.justifyItems.HasValue) { d.set_mask |= STYLE_SET_JUSTIFY_ITEMS; d.justify_items = (uint)s.justifyItems.Value; }
        if (s.justifySelf.HasValue) { d.set_mask |= STYLE_SET_JUSTIFY_SELF; d.justify_self = (uint)s.justifySelf.Value; }
        if (s.overflowX.HasValue) { d.set_mask |= STYLE_SET_OVERFLOW_X; d.overflow_x = (uint)s.overflowX.Value; }
        if (s.overflowY.HasValue) { d.set_mask |= STYLE_SET_OVERFLOW_Y; d.overflow_y = (uint)s.overflowY.Value; }
        if (s.gridAutoFlow.HasValue) { d.set_mask |= STYLE_SET_GRID_AUTO_FLOW; d.grid_auto_flow = (uint)s.gridAutoFlow.Value; }
        if (s.flexGrow.HasValue) { d.set_mask |= STYLE_SET_FLEX_GROW; d.flex_grow = s.flexGrow.Value; }
        if (s.flexShrink.HasValue) { d.set_mask |= STYLE_SET_FLEX_SHRINK; d.flex_shrink = s.flexShrink.Value; }
        if (s.flexBasis.HasValue) { d.set_mask |= STYLE_SET_FLEX_BASIS; d.flex_basis = s.flexBasis.Value; }
        if (s.width.HasValue) { d.set_mask |= STYLE_SET_WIDTH; d.width = s.width.Value; }
        if (s.height.HasValue) { d.set_mask |= STYLE_SET_HEIGHT; d.height = s.height.Value; }
        if (s.minWidth.HasValue) { d.set_mask |= STYLE_SET_MIN_WIDTH; d.min_width = s.minWidth.Value; }
        if (s.minHeight.HasValue) { d.set_mask |= STYLE_SET_MIN_HEIGHT; d.min_height = s.minHeight.Value; }
        if (s.maxWidth.HasValue) { d.set_mask |= STYLE_SET_MAX_WIDTH; d.max_width = s.maxWidth.Value; }
        if (s.maxHeight.HasValue) { d.set_mask |= STYLE_SET_MAX_HEIGHT; d.max_height = s.maxHeight.Value; }
        // Components whose unit is None are skipped natively, keeping Taffy's default for that edge or axis.
        if (s.gap.HasValue) { d.set_mask |= STYLE_SET_GAP; d.gap = s.gap.Value; }
        if (s.padding.HasValue) { d.set_mask |= STYLE_SET_PADDING; d.padding = s.padding.Value; }
        if (s.margin.HasValue) { d.set_mask |= STYLE_SET_MARGIN; d.margin = s.margin.Value; }
        if (s.gridColumn.HasValue) { d.set_mask |= STYLE_SET_GRID_COLUMN; d.grid_column = s.gridColumn.Value; }
        if (s.gridRow.HasValue) { d.set_mask |= STYLE_SET_GRID_ROW; d.grid_row = s.gridRow.Value; }

        // RimWorld scrollbars have one width, so it is only reserved when the style actually scrolls.
        if (s.overflowX == TaffyOverflow.Scroll || s.overflowY == TaffyOverflow.Scroll)
        {
            d.set_mask |= STYLE_SET_SCROLLBAR_WIDTH;
            d.scrollbar_width = Style.ScrollbarGutter;
        }

        if (s.gridTemplateColumns != null) { d.set_mask |= STYLE_SET_GRID_TEMPLATE_COLUMNS; d.grid_template_columns_len = (uint)s.gridTemplateColumns.Length; }
        if (s.gridTemplateRows != null) { d.set_mask |= STYLE_SET_GRID_TEMPLATE_ROWS; d.grid_template_rows_len = (uint)s.gridTemplateRows.Length; }
        if (s.gridAutoColumns != null) { d.set_mask |= STYLE_SET_GRID_AUTO_COLUMNS; d.grid_auto_columns_len = (uint)s.gridAutoColumns.Length; }
        if (s.gridAutoRows != null) { d.set_mask |= STYLE_SET_GRID_AUTO_ROWS; d.grid_auto_rows_len = (uint)s.gridAutoRows.Length; }

        uint handle;
        fixed (TaffyTrackSizingFunction* templateColumns = s.gridTemplateColumns)
        fixed (TaffyTrackSizingFunction* templateRows = s.gridTemplateRows)
        fixed (TaffyTrackSizingFunction* autoColumns = s.gridAutoColumns)
        fixed (TaffyTrackSizingFunction* autoRows = s.gridAutoRows)
        {
            d.grid_template_columns = templateColumns;
            d.grid_template_rows = templateRows;
            d.grid_auto_columns = autoColumns;
            d.grid_auto_rows = autoRows;
            Check(void_style_register(&d, &handle), nameof(void_style_register));
        }

        return handle;
    }
}
