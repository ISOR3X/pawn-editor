// Port of taffy/src/style/mod.rs (enum types)
// Display, BoxSizing, BoxGenerationMode, Position, Overflow, Direction

namespace Taffy;

/// <summary>
///     Sets the layout algorithm used for this node's children.
/// </summary>
public enum Display : byte
{
    /// <summary>Children follow the Flexbox layout algorithm.</summary>
    Flex,

    /// <summary>Children follow the CSS Grid layout algorithm.</summary>
    Grid,

    /// <summary>Children follow the Block layout algorithm.</summary>
    Block,

    /// <summary>The node and all its descendants are hidden (generate no boxes).</summary>
    None
}

/// <summary>Whether a node generates a box in the normal flow.</summary>
public enum BoxGenerationMode : byte
{
    /// <summary>The node generates a box normally.</summary>
    Normal,

    /// <summary>The node and its descendants generate no boxes (hidden).</summary>
    None
}

/// <summary>
///     Specifies whether size styles apply to the content box or border box.
///     CSS: <c>box-sizing</c>.
/// </summary>
public enum BoxSizing : byte
{
    /// <summary>Size styles include padding and border (excludes margin). Default.</summary>
    BorderBox,

    /// <summary>Size styles apply to the content box only (excludes padding, border, margin).</summary>
    ContentBox
}

/// <summary>
///     Controls whether the item is positioned in the normal flow or absolutely.
///     CSS: <c>position</c>.
/// </summary>
public enum Position : byte
{
    /// <summary>
    ///     Offset relative to the item's normal layout position.
    ///     Other items are not affected by the offset.
    /// </summary>
    Relative,

    /// <summary>
    ///     Offset relative to the nearest positioned ancestor (or the root).
    ///     No space is reserved for this item in the flow.
    ///     Use <see cref="Display.None" /> to remove an item entirely.
    /// </summary>
    Absolute
}

/// <summary>
///     Controls how children that overflow the container affect layout.
///     CSS: <c>overflow</c>.
/// </summary>
public enum Overflow : byte
{
    /// <summary>
    ///     Min-size is content-based. Overflowing content contributes to parent's scroll region.
    /// </summary>
    Visible,

    /// <summary>
    ///     Min-size is content-based. Overflowing content does NOT contribute to parent's scroll region.
    /// </summary>
    Clip,

    /// <summary>
    ///     Min-size is 0. Overflowing content does NOT contribute to parent's scroll region.
    /// </summary>
    Hidden,

    /// <summary>
    ///     Min-size is 0. Space for a scrollbar is reserved (<c>scrollbar_width</c>).
    /// </summary>
    Scroll
}

public static class OverflowExt
{
    /// <summary>Returns true for Hidden/Scroll (scroll containers), false for Visible/Clip.</summary>
    public static bool IsScrollContainer(this Overflow o)
    {
        return o == Overflow.Hidden || o == Overflow.Scroll;
    }

    /// <summary>Returns 0 if this overflow would force automatic min-size to 0, else null.</summary>
    internal static float? MaybeIntoAutomaticMinSize(this Overflow o)
    {
        return o.IsScrollContainer() ? 0f : null;
    }
}

/// <summary>
///     Text direction — affects column ordering and horizontal overflow.
///     CSS: <c>direction</c>.
/// </summary>
public enum Direction : byte
{
    /// <summary>Left-to-right.</summary>
    Ltr,

    /// <summary>Right-to-left.</summary>
    Rtl
}

public static class DirectionExt
{
    public static bool IsRtl(this Direction d)
    {
        return d == Direction.Rtl;
    }
}

/// <summary>
///     Used by block layout to implement the legacy <c><center></c> / <c>align=</c> behaviour.
///     CSS: <c>text-align</c> (legacy subset only).
/// </summary>
public enum TextAlign : byte
{
    /// <summary>No special legacy text-align behaviour. Default.</summary>
    Auto,

    /// <summary>Legacy left-align (corresponds to <c>-webkit-left</c>).</summary>
    LegacyLeft,

    /// <summary>Legacy right-align (corresponds to <c>-webkit-right</c>).</summary>
    LegacyRight,

    /// <summary>Legacy center-align (corresponds to <c>-webkit-center</c>).</summary>
    LegacyCenter
}

// ── Axis extension helpers ────────────────────────────────────────────────

public static class AbstractAxisExt
{
    /// <summary>Returns the opposite axis (Inline ↔ Block).</summary>
    public static AbstractAxis OtherAxis(this AbstractAxis axis)
    {
        return axis == AbstractAxis.Inline ? AbstractAxis.Block : AbstractAxis.Inline;
    }

    /// <summary>Maps AbstractAxis to AbsoluteAxis (Inline = Horizontal, Block = Vertical).</summary>
    public static AbsoluteAxis AsAbsNaive(this AbstractAxis axis)
    {
        return axis == AbstractAxis.Inline ? AbsoluteAxis.Horizontal : AbsoluteAxis.Vertical;
    }

    /// <summary>Maps AbsoluteAxis to AbstractAxis (Horizontal = Inline, Vertical = Block).</summary>
    public static AbstractAxis AsAbstract(this AbsoluteAxis axis)
    {
        return axis == AbsoluteAxis.Horizontal ? AbstractAxis.Inline : AbstractAxis.Block;
    }
}

public static class AbsoluteAxisExt
{
    /// <summary>Returns the opposite axis (Horizontal ↔ Vertical).</summary>
    public static AbsoluteAxis OtherAxis(this AbsoluteAxis axis)
    {
        return axis == AbsoluteAxis.Horizontal ? AbsoluteAxis.Vertical : AbsoluteAxis.Horizontal;
    }
}