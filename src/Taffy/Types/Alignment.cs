// Port of taffy/src/style/alignment.rs
//
// Taffy defines several type aliases that all resolve to the same underlying enum:
//   AlignSelf    = AlignItems   (override per-child, cross/block axis)
//   JustifyItems = AlignItems   (grid inline axis)
//   JustifySelf  = AlignItems   (grid inline axis, per-child override)
//   JustifyContent = AlignContent (main axis / grid inline axis distribution)
//
// In C# we use the same enums directly and note the aliases in XML docs.

namespace Taffy;

/// <summary>
///     Controls how child nodes are aligned along the cross axis (Flexbox) or block axis (Grid).
///     <para>
///         Also used as <c>AlignSelf</c> (per-child override), <c>JustifyItems</c> (grid inline axis),
///         and <c>JustifySelf</c> (per-child grid inline override).
///     </para>
///     CSS: <c>align-items</c>, <c>align-self</c>, <c>justify-items</c>, <c>justify-self</c>.
/// </summary>
public enum AlignItems : byte
{
    /// <summary>Items are packed toward the start of the axis.</summary>
    Start,

    /// <summary>Items are packed toward the end of the axis.</summary>
    End,

    /// <summary>
    ///     Flex-relative start. Equivalent to <see cref="End" /> for RowReverse/ColumnReverse,
    ///     <see cref="Start" /> otherwise.
    /// </summary>
    FlexStart,

    /// <summary>
    ///     Flex-relative end. Equivalent to <see cref="Start" /> for RowReverse/ColumnReverse,
    ///     <see cref="End" /> otherwise.
    /// </summary>
    FlexEnd,

    /// <summary>Items are aligned along the center of the axis.</summary>
    Center,

    /// <summary>Items are aligned so their baselines align.</summary>
    Baseline,

    /// <summary>Items are stretched to fill the container.</summary>
    Stretch
}

/// <summary>
///     Controls distribution of space between and around content items.
///     <para>Also used as <c>JustifyContent</c> (Flexbox main axis / Grid inline axis).</para>
///     CSS: <c>align-content</c>, <c>justify-content</c>.
/// </summary>
public enum AlignContent : byte
{
    /// <summary>Items are packed toward the start of the axis.</summary>
    Start,

    /// <summary>Items are packed toward the end of the axis.</summary>
    End,

    /// <summary>Flex-relative start.</summary>
    FlexStart,

    /// <summary>Flex-relative end.</summary>
    FlexEnd,

    /// <summary>Items are centered.</summary>
    Center,

    /// <summary>Items are stretched to fill the container.</summary>
    Stretch,

    /// <summary>First and last items flush with edges; space between items distributed evenly.</summary>
    SpaceBetween,

    /// <summary>Gap between first/last items equals the gap between items.</summary>
    SpaceEvenly,

    /// <summary>Gap between first/last items is half the gap between items.</summary>
    SpaceAround
}

public static class AlignContentExt
{
    /// <summary>Returns the reversed alignment for RTL contexts.</summary>
    public static AlignContent Reversed(this AlignContent a)
    {
        return a switch
        {
            AlignContent.Start => AlignContent.End,
            AlignContent.End => AlignContent.Start,
            AlignContent.FlexStart => AlignContent.FlexEnd,
            AlignContent.FlexEnd => AlignContent.FlexStart,
            AlignContent.Stretch => AlignContent.End,
            _ => a
        };
    }
}