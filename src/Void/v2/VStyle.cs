using Taffy;
using UnityEngine;
using Verse;

namespace Void.v2;

/// <summary>
///     Value-type counterpart to <see cref="Void.StyleOverride" /> — same field set and the same
///     <c>Apply(TaffyStyleRef)</c> behavior, but a comparable <c>record struct</c> so it can be
///     diffed against what was last pushed to a node instead of pushed unconditionally.
///     <para>
///         A copy rather than a conversion of the original: <see cref="Void.StyleOverride" /> is
///         used throughout the existing <c>Void</c> component library in ways that haven't been
///         audited for struct-safety, so this exists independently in <c>Void.v2</c> for now.
///     </para>
///     <para>
///         Grid template/auto fields are arrays, which the compiler-generated record equality
///         would compare by reference (always "changed" for freshly-built arrays) — this type
///         hand-writes <see cref="Equals(VStyle)" /> to compare their contents instead.
///     </para>
/// </summary>
public readonly record struct VStyle(
    Color? Color = null,
    Color? BackgroundColor = null,
    GameFont? FontSize = null,
    TaffyAlignContent? AlignContent = null,
    TaffyAlignItems? AlignItems = null,
    TaffyAlignItems? AlignSelf = null,
    TaffyDisplay? Display = null,
    TaffyDimension? FlexBasis = null,
    TaffyFlexDirection? FlexDirection = null,
    float? FlexGrow = null,
    float? FlexShrink = null,
    TaffyFlexWrap? FlexWrap = null,
    TaffyAxes? Gap = null,
    TaffyTrackSizingFunction[]? GridAutoColumns = null,
    TaffyGridAutoFlow? GridAutoFlow = null,
    TaffyTrackSizingFunction[]? GridAutoRows = null,
    TaffyGridPlacement? GridColumn = null,
    TaffyGridPlacement? GridRow = null,
    TaffyTrackSizingFunction[]? GridTemplateColumns = null,
    TaffyTrackSizingFunction[]? GridTemplateRows = null,
    TaffyDimension? Width = null,
    TaffyDimension? Height = null,
    TaffyAlignContent? JustifyContent = null,
    TaffyAlignItems? JustifyItems = null,
    TaffyAlignItems? JustifySelf = null,
    TaffyEdges? Margin = null,
    TaffyDimension? MaxHeight = null,
    TaffyDimension? MaxWidth = null,
    TaffyDimension? MinHeight = null,
    TaffyDimension? MinWidth = null,
    TaffyEdges? Padding = null)
{
    /// <summary>Applies all non-null fields to <paramref name="s" />.</summary>
    public void Apply(TaffyStyleRef s)
    {
        if (Display.HasValue) s.Display = Display.Value;
        if (FlexDirection.HasValue) s.FlexDirection = FlexDirection.Value;
        if (FlexWrap.HasValue) s.FlexWrap = FlexWrap.Value;
        if (FlexGrow.HasValue) s.FlexGrow = FlexGrow.Value;
        if (FlexShrink.HasValue) s.FlexShrink = FlexShrink.Value;
        if (FlexBasis.HasValue) s.FlexBasis = FlexBasis.Value;
        if (AlignItems.HasValue) s.AlignItems = AlignItems;
        if (AlignSelf.HasValue) s.AlignSelf = AlignSelf;
        if (AlignContent.HasValue) s.AlignContent = AlignContent;
        if (JustifyContent.HasValue) s.JustifyContent = JustifyContent;
        if (JustifyItems.HasValue) s.JustifyItems = JustifyItems;
        if (JustifySelf.HasValue) s.JustifySelf = JustifySelf;
        if (Width.HasValue) s.Width = Width.Value;
        if (Height.HasValue) s.Height = Height.Value;
        if (MinWidth.HasValue) s.MinWidth = MinWidth.Value;
        if (MinHeight.HasValue) s.MinHeight = MinHeight.Value;
        if (MaxWidth.HasValue) s.MaxWidth = MaxWidth.Value;
        if (MaxHeight.HasValue) s.MaxHeight = MaxHeight.Value;
        if (Gap.HasValue)
        {
            // None unit = "not specified by caller" — skip to preserve the native default.
            if (Gap.Value.Width.unit != TaffyUnit.None) s.ColumnGap = Gap.Value.Width;
            if (Gap.Value.Height.unit != TaffyUnit.None) s.RowGap = Gap.Value.Height;
        }

        if (Padding.HasValue)
        {
            if (Padding.Value.Top.unit != TaffyUnit.None) s.PaddingTop = Padding.Value.Top;
            if (Padding.Value.Right.unit != TaffyUnit.None) s.PaddingRight = Padding.Value.Right;
            if (Padding.Value.Bottom.unit != TaffyUnit.None) s.PaddingBottom = Padding.Value.Bottom;
            if (Padding.Value.Left.unit != TaffyUnit.None) s.PaddingLeft = Padding.Value.Left;
        }

        if (Margin.HasValue)
        {
            if (Margin.Value.Top.unit != TaffyUnit.None) s.MarginTop = Margin.Value.Top;
            if (Margin.Value.Right.unit != TaffyUnit.None) s.MarginRight = Margin.Value.Right;
            if (Margin.Value.Bottom.unit != TaffyUnit.None) s.MarginBottom = Margin.Value.Bottom;
            if (Margin.Value.Left.unit != TaffyUnit.None) s.MarginLeft = Margin.Value.Left;
        }

        if (GridAutoFlow.HasValue) s.GridAutoFlow = GridAutoFlow.Value;
        if (GridColumn.HasValue) s.GridColumn = GridColumn.Value;
        if (GridRow.HasValue) s.GridRow = GridRow.Value;
        if (GridTemplateColumns != null) s.SetGridTemplateColumns(GridTemplateColumns);
        if (GridTemplateRows != null) s.SetGridTemplateRows(GridTemplateRows);
        if (GridAutoColumns != null) s.SetGridAutoColumns(GridAutoColumns);
        if (GridAutoRows != null) s.SetGridAutoRows(GridAutoRows);
    }

    public bool Equals(VStyle other) =>
        Color.Equals(other.Color) &&
        BackgroundColor.Equals(other.BackgroundColor) &&
        FontSize == other.FontSize &&
        AlignContent == other.AlignContent &&
        AlignItems == other.AlignItems &&
        AlignSelf == other.AlignSelf &&
        Display == other.Display &&
        FlexBasis.Equals(other.FlexBasis) &&
        FlexDirection == other.FlexDirection &&
        FlexGrow.Equals(other.FlexGrow) &&
        FlexShrink.Equals(other.FlexShrink) &&
        FlexWrap == other.FlexWrap &&
        Gap.Equals(other.Gap) &&
        SequenceEqualNullable(GridAutoColumns, other.GridAutoColumns) &&
        GridAutoFlow == other.GridAutoFlow &&
        SequenceEqualNullable(GridAutoRows, other.GridAutoRows) &&
        GridColumn.Equals(other.GridColumn) &&
        GridRow.Equals(other.GridRow) &&
        SequenceEqualNullable(GridTemplateColumns, other.GridTemplateColumns) &&
        SequenceEqualNullable(GridTemplateRows, other.GridTemplateRows) &&
        Width.Equals(other.Width) &&
        Height.Equals(other.Height) &&
        JustifyContent == other.JustifyContent &&
        JustifyItems == other.JustifyItems &&
        JustifySelf == other.JustifySelf &&
        Margin.Equals(other.Margin) &&
        MaxHeight.Equals(other.MaxHeight) &&
        MaxWidth.Equals(other.MaxWidth) &&
        MinHeight.Equals(other.MinHeight) &&
        MinWidth.Equals(other.MinWidth) &&
        Padding.Equals(other.Padding);

    // Deliberately excludes the array fields — equal-per-Equals values agree on every field this
    // does hash, so the Equals/GetHashCode contract still holds; it just doesn't need array
    // content hashing since nothing stores VStyle in a hash-based collection today.
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Display.GetHashCode();
            hash = hash * 31 + Width.GetHashCode();
            hash = hash * 31 + Height.GetHashCode();
            hash = hash * 31 + FlexDirection.GetHashCode();
            hash = hash * 31 + FlexGrow.GetHashCode();
            hash = hash * 31 + FlexShrink.GetHashCode();
            return hash;
        }
    }

    private static bool SequenceEqualNullable(TaffyTrackSizingFunction[]? a, TaffyTrackSizingFunction[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return System.Linq.Enumerable.SequenceEqual(a, b);
    }
}
