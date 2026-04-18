// Taffy integration layer for RimWorld.
//
// Provides a callback-style fluent API backed by Taffy's Taffy layout engine.
//
// Usage:
//   Taffy.Column(inRect, gap: 4f, col =>
//   {
//       col.Item(height: 30f, draw: r => Widgets.Label(r, "Title"));
//       col.Row(grow: 1f, row =>
//       {
//           row.Item(grow: 1f,    draw: r => DrawContent(r));
//           row.Item(width: 200f, draw: r => DrawSidebar(r));
//       });
//   });
//
// CSS Grid usage:
//   Taffy.Grid(inRect, [Taffy.Fr(), Taffy.Fr(2)], gap: 8f, grid =>
//   {
//       grid.GridItem(draw: r => Widgets.Label(r, "Left"));
//       grid.GridItem(draw: r => DrawContent(r));
//       grid.GridItem(colSpan: 2, draw: r => DrawFooter(r));
//   });

using Taffy;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;
using Display = Taffy.Display;
using SizeF = Taffy.SizeF;

namespace Void;

/// <summary>
///     Fluent layout builder passed to <see cref="Taffy.Row" /> / <see cref="UnityEngine.UIElements.Column" /> lambdas.
/// </summary>
public sealed class TaffyBuilder(TaffyTree tree, List<(NodeId id, Action<Rect>? draw)> callbacks)
{
    // Memoizes Text.CalcSize(word).x per (word, font) pair — populated once, reused every frame.
    public static readonly Dictionary<(string word, GameFont font), float> WordWidthCache = [];
    public readonly List<(NodeId id, Action<Rect>? draw)> callbacks = callbacks;
    public readonly List<NodeId> children = [];

    public readonly TaffyTree tree = tree;

    /// <summary>
    ///     Stable identifier for the current context, used by stateful extensions like
    ///     <c>TaffyExtensions.Input(ref string)</c> to key their per-widget persistent state.
    ///     Set by <see cref="SectionWorker.BuildSection" /> before entering section content.
    /// </summary>
    public string? ContextKey { get; set; }

    // ── Grid items ──────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a grid item with optional column/row placement.
    ///     All parameters use CSS Grid 1-based line indices.
    ///     TODO: deprecate and remove
    /// </summary>
    /// <param name="draw">Draw callback invoked with the item's computed rect.</param>
    /// <param name="colSpan">Number of columns to span (default 1).</param>
    /// <param name="rowSpan">Number of rows to span (default 1).</param>
    /// <param name="colStart">Explicit column-start line (1-based). Null = auto-placed.</param>
    /// <param name="rowStart">Explicit row-start line (1-based). Null = auto-placed.</param>
    public void GridItem(Action<Rect>? draw = null,
        int colSpan = 1, int rowSpan = 1,
        int? colStart = null, int? rowStart = null)
    {
        var style = new Style();

        if (colStart.HasValue || colSpan > 1)
        {
            var start = colStart.HasValue ? GridPlacement.Line(colStart.Value) : GridPlacement.Auto;
            var end = colSpan > 1 ? GridPlacement.Span(colSpan) : GridPlacement.Auto;
            style.gridColumn = new Line<GridPlacement>(start, end);
        }

        if (rowStart.HasValue || rowSpan > 1)
        {
            var start = rowStart.HasValue ? GridPlacement.Line(rowStart.Value) : GridPlacement.Auto;
            var end = rowSpan > 1 ? GridPlacement.Span(rowSpan) : GridPlacement.Auto;
            style.gridRow = new Line<GridPlacement>(start, end);
        }

        AddLeaf(style, draw);
    }

    #region LEAF ITEMS

    public void Item(Action<Rect>? draw = null, StyleOverride? style = null)
    {
        AddLeaf((style ?? new StyleOverride()).Resolve(), draw);
    }

    public void Div(Action<TaffyBuilder>? builder = null, StyleOverride? style = null)
    {
        AddContainer((style ?? new StyleOverride()).Resolve(), null, builder);
    }

    /// <summary>
    ///     Adds a container that runs <paramref name="draw" /> on its own rect (e.g. highlight/tooltip) before drawing
    ///     children.
    /// </summary>
    public void Div(Action<Rect>? draw = null, Action<TaffyBuilder>? builder = null, StyleOverride? style = null)
    {
        AddContainer((style ?? new StyleOverride()).Resolve(), draw, builder);
    }

    #endregion

    #region HELPERS

    private void AddLeaf(Style style, Action<Rect>? draw)
    {
        var node = tree.NewLeaf(style);
        children.Add(node);
        callbacks.Add((node, draw));
    }

    private void AddContainer(Style style, Action<Rect>? draw, Action<TaffyBuilder>? build)
    {
        var inner = new TaffyBuilder(tree, callbacks) { ContextKey = ContextKey };
        build?.Invoke(inner);
        var node = tree.NewWithChildren(style, inner.children);
        children.Add(node);
        callbacks.Add((node, draw));
    }

    #endregion
}

/// <summary>
///     Static entry points for Taffy-backed layout in RimWorld.
///     Creates a fresh layout tree per call; the layout is computed and draws callbacks invoked before returning.
/// </summary>
public static class Taffy
{
    #region ENTRY POINTS

    /// <summary>
    ///     RimWorld entry point for the Taffy layout engine.
    /// </summary>
    public static void Div(Rect rect, Action<TaffyBuilder> build, StyleOverride? style = null)
    {
        Execute(rect, (style ?? new StyleOverride()).Resolve(), build);
    }

    #endregion

    /// <summary>Grid layout with separate column/row gaps and fixed auto-row height.</summary>
    public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
        float gapX, float gapY, float autoRowHeight, Action<TaffyBuilder> build)
    {
        Execute(rect, MakeGridStyle(columns, null, gapX, gapY, autoRowHeight), build);
    }

    // ── Content-measured entry points ───────────────────────────────────────
    //
    // These lay out children with width=rect.width and height=MaxContent (unconstrained),
    // draw the result, and return the computed root height.  This is the idiomatic Taffy
    // way to measure natural content height: pass AvailableSpace::MaxContent on the height
    // axis to compute_layout, just as the Rust library does in its own test suite.
    // Used by TabWorker.DoTabContents to size the scroll view's viewRect each frame.

    /// <summary>
    ///     Like <see cref="Div" /> but lays out with unconstrained height, draws all content,
    ///     and returns the computed content height. Used for scrollable containers where
    ///     the natural content height drives the scroll view size.
    /// </summary>
    public static float DivMeasured(Rect rect, Action<TaffyBuilder> build, StyleOverride? style = null)
    {
        return ExecuteMeasured(rect, (style ?? new StyleOverride()).Resolve(), build);
    }

    /// <summary>
    ///     Grid layout with separate column/row gaps and unconstrained height. Returns the computed content height.
    ///     TODO: deprecate and remove.
    /// </summary>
    public static float MeasuredGrid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
        float gapX, float gapY, float autoRowHeight, Action<TaffyBuilder> build)
    {
        return ExecuteMeasured(rect, MakeGridStyle(columns, null, gapX, gapY, autoRowHeight), build);
    }

    private static float ExecuteMeasured(Rect rect, Style rootStyle, Action<TaffyBuilder> build)
    {
        var tree = new TaffyTree();
        var callbacks = new List<(NodeId id, Action<Rect>? draw)>();
        // Width is definite; height is AUTO so the engine sizes to content
        // (equivalent to passing Size::MAX_CONTENT on the height axis in Rust Taffy).
        rootStyle.size = new Size<Dimension>(Dimension.Length(rect.width), Dimension.AUTO);
        var builder = new TaffyBuilder(tree, callbacks);
        build(builder);
        var root = tree.NewWithChildren(rootStyle, builder.children);
        tree.ComputeLayoutWithMeasure(root,
            new Size<AvailableSpace>(AvailableSpace.Definite(rect.width), AvailableSpace.MaxContent),
            (known, available, _, ctx, _) =>
                ctx is Func<Size<float?>, Size<AvailableSpace>, Size<float>> measure
                    ? measure(known, available)
                    : SizeF.ZERO);
        var lookup = new Dictionary<NodeId, Action<Rect>?>(callbacks.Count);
        foreach (var entry in callbacks)
            lookup[entry.id] = entry.draw;
        DrawTree(tree, root, rect.x, rect.y, lookup);
        return tree.Layout(root).Size.Height;
    }

    private static Style MakeGridStyle(IReadOnlyList<TrackSizingFunction> columns,
        IReadOnlyList<TrackSizingFunction>? rows,
        float gapX, float gapY, float autoRowHeight)
    {
        var s = new Style
        {
            display = Display.Grid,
            gridTemplateColumns = [..columns],
            gridTemplateRows = rows != null ? [..rows] : null,
            gap = new Size<LengthPercentage>(
                LengthPercentage.Length(gapX),
                LengthPercentage.Length(gapY))
        };
        // When items are leaf nodes with no intrinsic size, CSS auto rows collapse to 0.
        // An explicit autoRowHeight overrides gridAutoRows to give each row a fixed height.
        if (autoRowHeight > 0f)
            s.gridAutoRows = [TrackSizingFunction.Px(autoRowHeight)];
        return s;
    }

    #region TRACK SIZING SHORTHANDS

    /// <summary>A flexible track that takes the given fraction of remaining space (default 1fr).</summary>
    public static TrackSizingFunction Fr(float fr = 1f)
    {
        return TrackSizingFunction.Fr(fr);
    }

    /// <summary>A fixed-size track of <paramref name="px" /> pixels.</summary>
    public static TrackSizingFunction Px(float px)
    {
        return TrackSizingFunction.Px(px);
    }

    /// <summary>An auto-sized track (sized to content, then stretched to fill).</summary>
    public static TrackSizingFunction AutoTrack()
    {
        return TrackSizingFunction.Auto();
    }

    /// <summary>A percent-sized track relative to the grid container.</summary>
    public static TrackSizingFunction PercentTrack(float pct)
    {
        return TrackSizingFunction.Percent(pct);
    }

    /// <summary>A fit-content track capped at <paramref name="px" /> pixels.</summary>
    public static TrackSizingFunction FitContent(float px)
    {
        return TrackSizingFunction.FitContentPx(px);
    }

    #endregion

    #region STYLE HELPERS

    /// <summary>Creates uniform padding on all four sides.</summary>
    public static Rect<LengthPercentage> Padding(float all)
    {
        var v = LengthPercentage.Length(all);
        return new Rect<LengthPercentage>(v, v, v, v);
    }

    /// <summary>Creates asymmetric padding: <paramref name="lr" /> on left/right, <paramref name="tb" /> on top/bottom.</summary>
    public static Rect<LengthPercentage> Padding(float lr, float tb)
    {
        var h = LengthPercentage.Length(lr);
        var v = LengthPercentage.Length(tb);
        return new Rect<LengthPercentage>(h, h, v, v);
    }

    public static Rect<LengthPercentageAuto> Margin(float all)
    {
        var v = LengthPercentageAuto.Length(all);
        return new Rect<LengthPercentageAuto>(v, v, v, v);
    }

    public static Rect<LengthPercentageAuto> Margin(float lr, float tb)
    {
        var h = LengthPercentageAuto.Length(lr);
        var v = LengthPercentageAuto.Length(tb);
        return new Rect<LengthPercentageAuto>(h, h, v, v);
    }

    /// <summary>Creates uniform gap on both axes.</summary>
    public static Size<LengthPercentage> Gap(float all)
    {
        return new Size<LengthPercentage>(LengthPercentage.Length(all), LengthPercentage.Length(all));
    }

    /// <summary>Creates asymmetric gap: <paramref name="column" /> between columns, <paramref name="row" /> between rows.</summary>
    public static Size<LengthPercentage> Gap(float column, float row)
    {
        return new Size<LengthPercentage>(LengthPercentage.Length(column), LengthPercentage.Length(row));
    }

    #endregion

    #region CORE

    private static void Execute(Rect rect, Style rootStyle, Action<TaffyBuilder> build)
    {
        var tree = new TaffyTree();
        var callbacks = new List<(NodeId id, Action<Rect>? draw)>();

        // Give the root container a definite width from the rect so that fr columns resolve
        // correctly. Without this, inner_node_size.Width is None, which causes ExpandFlexibleTracks
        // to use MaxContent semantics: fr fraction = max content of items = 0 for leaf nodes,
        // making all fr columns 0px wide. Height is left Auto so the container shrinks to content.
        rootStyle.size = new Size<Dimension>(Dimension.Length(rect.width), Dimension.Length(rect.height));

        var builder = new TaffyBuilder(tree, callbacks);
        build(builder);
        var root = tree.NewWithChildren(rootStyle, builder.children);

        tree.ComputeLayoutWithMeasure(root, new Size<AvailableSpace>(
                AvailableSpace.Definite(rect.width),
                AvailableSpace.Definite(rect.height)),
            (known, available, _, ctx, _) =>
                ctx is Func<Size<float?>, Size<AvailableSpace>, Size<float>> measure
                    ? measure(known, available)
                    : SizeF.ZERO);

        var lookup = new Dictionary<NodeId, Action<Rect>?>(callbacks.Count);
        foreach (var entry in callbacks)
            lookup[entry.id] = entry.draw;

        DrawTree(tree, root, rect.x, rect.y, lookup);
    }

    private static void DrawTree(TaffyTree tree, NodeId node, float originX, float originY,
        Dictionary<NodeId, Action<Rect>?> lookup)
    {
        ref var layout = ref tree.Layout(node);
        var absX = originX + layout.Location.X;
        var absY = originY + layout.Location.Y;
        var r = new Rect(absX, absY, layout.Size.Width, layout.Size.Height);

        if (VoidMod.Settings.drawDebug)
        {
            var h = node.GetHashCode() * 0.618033988f % 1f;
            Verse.Widgets.DrawBoxSolid(r, Color.HSVToRGB(h, 0.6f, 0.9f) with { a = 0.25f });
        }

        if (lookup.TryGetValue(node, out var draw) && draw != null)
        {
            var prevWordWrap = Text.WordWrap;
            draw(r);
            Text.WordWrap = prevWordWrap;
        }

        foreach (var child in tree.Children(node))
            DrawTree(tree, child, absX, absY, lookup);
    }

    #endregion
}