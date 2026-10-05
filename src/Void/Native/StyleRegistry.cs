using Taffy;
using Void.Taffy;

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

        if (s.display.HasValue) { d.setMask |= StyleSet.Display; d.display = (uint)s.display.Value; }
        if (s.flexDirection.HasValue) { d.setMask |= StyleSet.FlexDirection; d.flexDirection = (uint)s.flexDirection.Value; }
        if (s.flexWrap.HasValue) { d.setMask |= StyleSet.FlexWrap; d.flexWrap = (uint)s.flexWrap.Value; }
        if (s.alignItems.HasValue) { d.setMask |= StyleSet.AlignItems; d.alignItems = (uint)s.alignItems.Value; }
        if (s.alignSelf.HasValue) { d.setMask |= StyleSet.AlignSelf; d.alignSelf = (uint)s.alignSelf.Value; }
        if (s.alignContent.HasValue) { d.setMask |= StyleSet.AlignContent; d.alignContent = (uint)s.alignContent.Value; }
        if (s.justifyContent.HasValue) { d.setMask |= StyleSet.JustifyContent; d.justifyContent = (uint)s.justifyContent.Value; }
        if (s.justifyItems.HasValue) { d.setMask |= StyleSet.JustifyItems; d.justifyItems = (uint)s.justifyItems.Value; }
        if (s.justifySelf.HasValue) { d.setMask |= StyleSet.JustifySelf; d.justifySelf = (uint)s.justifySelf.Value; }
        if (s.overflowX.HasValue) { d.setMask |= StyleSet.OverflowX; d.overflowX = (uint)s.overflowX.Value; }
        if (s.overflowY.HasValue) { d.setMask |= StyleSet.OverflowY; d.overflowY = (uint)s.overflowY.Value; }
        if (s.gridAutoFlow.HasValue) { d.setMask |= StyleSet.GridAutoFlow; d.gridAutoFlow = (uint)s.gridAutoFlow.Value; }
        if (s.flexGrow.HasValue) { d.setMask |= StyleSet.FlexGrow; d.flexGrow = s.flexGrow.Value; }
        if (s.flexShrink.HasValue) { d.setMask |= StyleSet.FlexShrink; d.flexShrink = s.flexShrink.Value; }
        if (s.flexBasis.HasValue) { d.setMask |= StyleSet.FlexBasis; d.flexBasis = s.flexBasis.Value; }
        if (s.width.HasValue) { d.setMask |= StyleSet.Width; d.width = s.width.Value; }
        if (s.height.HasValue) { d.setMask |= StyleSet.Height; d.height = s.height.Value; }
        if (s.minWidth.HasValue) { d.setMask |= StyleSet.MinWidth; d.minWidth = s.minWidth.Value; }
        if (s.minHeight.HasValue) { d.setMask |= StyleSet.MinHeight; d.minHeight = s.minHeight.Value; }
        if (s.maxWidth.HasValue) { d.setMask |= StyleSet.MaxWidth; d.maxWidth = s.maxWidth.Value; }
        if (s.maxHeight.HasValue) { d.setMask |= StyleSet.MaxHeight; d.maxHeight = s.maxHeight.Value; }
        // Components whose unit is None are skipped natively, keeping Taffy's default for that edge or axis.
        if (s.gap.HasValue) { d.setMask |= StyleSet.Gap; d.gap = s.gap.Value; }
        if (s.padding.HasValue) { d.setMask |= StyleSet.Padding; d.padding = s.padding.Value; }
        if (s.margin.HasValue) { d.setMask |= StyleSet.Margin; d.margin = s.margin.Value; }
        if (s.gridColumn.HasValue) { d.setMask |= StyleSet.GridColumn; d.gridColumn = s.gridColumn.Value; }
        if (s.gridRow.HasValue) { d.setMask |= StyleSet.GridRow; d.gridRow = s.gridRow.Value; }

        // RimWorld scrollbars have one width, so it is only reserved when the style actually scrolls.
        if (s.overflowX == TaffyOverflow.Scroll || s.overflowY == TaffyOverflow.Scroll)
        {
            d.setMask |= StyleSet.ScrollbarWidth;
            d.scrollbarWidth = Style.ScrollbarGutter;
        }

        if (s.gridTemplateColumns != null) { d.setMask |= StyleSet.GridTemplateColumns; d.gridTemplateColumnsLen = (uint)s.gridTemplateColumns.Length; }
        if (s.gridTemplateRows != null) { d.setMask |= StyleSet.GridTemplateRows; d.gridTemplateRowsLen = (uint)s.gridTemplateRows.Length; }
        if (s.gridAutoColumns != null) { d.setMask |= StyleSet.GridAutoColumns; d.gridAutoColumnsLen = (uint)s.gridAutoColumns.Length; }
        if (s.gridAutoRows != null) { d.setMask |= StyleSet.GridAutoRows; d.gridAutoRowsLen = (uint)s.gridAutoRows.Length; }

        uint handle;
        fixed (TaffyTrackSizingFunction* templateColumns = s.gridTemplateColumns)
        fixed (TaffyTrackSizingFunction* templateRows = s.gridTemplateRows)
        fixed (TaffyTrackSizingFunction* autoColumns = s.gridAutoColumns)
        fixed (TaffyTrackSizingFunction* autoRows = s.gridAutoRows)
        {
            d.gridTemplateColumns = templateColumns;
            d.gridTemplateRows = templateRows;
            d.gridAutoColumns = autoColumns;
            d.gridAutoRows = autoRows;
            VoidNative.Check(VoidNative.void_style_register(&d, &handle), nameof(VoidNative.void_style_register));
        }

        return handle;
    }
}
