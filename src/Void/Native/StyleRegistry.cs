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
        var key = style.Merge(new Style());
        key.gridTemplateColumns = (TaffyTrackSizingFunction[]?)style.gridTemplateColumns?.Clone();
        key.gridTemplateRows = (TaffyTrackSizingFunction[]?)style.gridTemplateRows?.Clone();
        key.gridAutoColumns = (TaffyTrackSizingFunction[]?)style.gridAutoColumns?.Clone();
        key.gridAutoRows = (TaffyTrackSizingFunction[]?)style.gridAutoRows?.Clone();

        handle = Register(key);
        Handles[key] = handle;
        return handle;
    }

    private static uint Register(Style style)
    {
        var d = style.desc;

        // RimWorld scrollbars have one width, so it is only reserved when the style actually scrolls.
        if (style.overflowX == TaffyOverflow.Scroll || style.overflowY == TaffyOverflow.Scroll)
        {
            d.set_mask |= STYLE_SET_SCROLLBAR_WIDTH;
            d.scrollbar_width = Style.ScrollbarGutter;
        }

        uint handle;
        fixed (TaffyTrackSizingFunction* templateColumns = style.gridTemplateColumns)
        fixed (TaffyTrackSizingFunction* templateRows = style.gridTemplateRows)
        fixed (TaffyTrackSizingFunction* autoColumns = style.gridAutoColumns)
        fixed (TaffyTrackSizingFunction* autoRows = style.gridAutoRows)
        {
            Attach(ref d.set_mask, STYLE_SET_GRID_TEMPLATE_COLUMNS, style.gridTemplateColumns, templateColumns,
                out d.grid_template_columns, out d.grid_template_columns_len);
            Attach(ref d.set_mask, STYLE_SET_GRID_TEMPLATE_ROWS, style.gridTemplateRows, templateRows,
                out d.grid_template_rows, out d.grid_template_rows_len);
            Attach(ref d.set_mask, STYLE_SET_GRID_AUTO_COLUMNS, style.gridAutoColumns, autoColumns,
                out d.grid_auto_columns, out d.grid_auto_columns_len);
            Attach(ref d.set_mask, STYLE_SET_GRID_AUTO_ROWS, style.gridAutoRows, autoRows,
                out d.grid_auto_rows, out d.grid_auto_rows_len);

            Check(void_style_register(&d, &handle), nameof(void_style_register));
        }

        return handle;
    }

    private static void Attach(ref ulong mask, ulong bit, TaffyTrackSizingFunction[]? tracks,
        TaffyTrackSizingFunction* pinned, out TaffyTrackSizingFunction* pointer, out uint length)
    {
        pointer = pinned;
        length = (uint)(tracks?.Length ?? 0);
        if (tracks != null) mask |= bit;
    }
}
