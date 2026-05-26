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
using SizeF = Taffy.SizeF;

namespace Void;

/// <summary>
///     Fluent layout builder passed to <see cref="Taffy.Row" /> / <see cref="UnityEngine.UIElements.Column" /> lambdas.
/// </summary>
public sealed class TaffyBuilder(TaffyTree tree, List<(NodeId id, Action<Rect>? draw)> callbacks)
{
    // Memoizes Text.CalcSize(word).x per (word, font) pair - populated once, reused every frame.
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
    private record MeasuredLayoutCacheEntry(
        TaffyTree Tree,
        NodeId Root,
        Dictionary<NodeId, Action<Rect>?> Lookup,
        float Height,
        float Width,
        string StyleKey);

    private static readonly Dictionary<int, MeasuredLayoutCacheEntry> LayoutCache = [];

    #region ENTRY POINTS

    /// <summary>
    ///     RimWorld entry point for the Taffy layout engine.
    /// </summary>
    public static void Div(int uniqueId, Rect rect, Action<TaffyBuilder> build, StyleOverride? style = null, bool forceRecache = false)
    {
        Execute(rect, (style ?? new StyleOverride()).Resolve(), build, uniqueId, forceRecache);
    }

    #endregion

    /// <summary>
    ///     Like <see cref="Div" /> but lays out with unconstrained height, draws all content,
    ///     and returns the computed content height. Used for scrollable containers where
    ///     the natural content height drives the scroll view size.
    /// </summary>
    public static float DivMeasured(int uniqueId, Rect rect, Action<TaffyBuilder> build, StyleOverride? style = null, bool forceRecache = false)
    {
        return ExecuteMeasured(rect, (style ?? new StyleOverride()).Resolve(), build, uniqueId, forceRecache);
    }

    private static float ExecuteMeasured(Rect rect, Style rootStyle, Action<TaffyBuilder> build, int uniqueId,
        bool forceRecache)
    {
        var styleKey = DescribeStyle(rootStyle);
        if (!forceRecache &&
            LayoutCache.TryGetValue(uniqueId, out var cached) &&
            Mathf.Approximately(cached.Width, rect.width) &&
            cached.StyleKey == styleKey)
        {
            DrawTree(cached.Tree, cached.Root, rect.x, rect.y, cached.Lookup);
            if (GUI.changed) 
                LayoutCache.Clear();
            return cached.Height;
        }

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
        foreach (var (id, draw) in callbacks) lookup[id] = draw;

        var height = tree.Layout(root).Size.Height;
        LayoutCache[uniqueId] = new MeasuredLayoutCacheEntry(
            tree,
            root,
            lookup,
            height,
            rect.width,
            styleKey);

        DrawTree(tree, root, rect.x, rect.y, lookup);
        if (GUI.changed) 
            LayoutCache.Clear();
        return height;
    }

    #region STYLE HELPERS

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

    private static void Execute(Rect rect, Style rootStyle, Action<TaffyBuilder> build, int uniqueId, bool forceRecache)
    {

        var styleKey = DescribeStyle(rootStyle);
        if (!forceRecache &&
            LayoutCache.TryGetValue(uniqueId, out var cached) &&
            Mathf.Approximately(cached.Width, rect.width) &&
            cached.StyleKey == styleKey)
        {
            DrawTree(cached.Tree, cached.Root, rect.x, rect.y, cached.Lookup);
            if (GUI.changed) 
                LayoutCache.Clear();
            return;
        }
        else
        {
            Log.Message($"UniqueId: {uniqueId}\nWidth: {rect.width}\nStyle: {styleKey}");
        }
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

        LayoutCache[uniqueId] = new MeasuredLayoutCacheEntry(
            tree,
            root,
            lookup,
            0,
            rect.width,
            styleKey);

        DrawTree(tree, root, rect.x, rect.y, lookup);
        if (GUI.changed)
            LayoutCache.Clear();
    }

    private static string DescribeStyle(Style style)
    {
        return string.Join(";", [
            style.display.ToString(),
            style.flexDirection.ToString(),
            style.flexWrap.ToString(),
            style.flexBasis.ToString(),
            style.flexGrow.ToString(),
            style.flexShrink.ToString(),
            style.size.Width.ToString(),
            style.size.Height.ToString(),
            style.minSize.Width.ToString(),
            style.minSize.Height.ToString(),
            style.maxSize.Width.ToString(),
            style.maxSize.Height.ToString(),
            style.margin.Left.ToString(),
            style.margin.Right.ToString(),
            style.margin.Top.ToString(),
            style.margin.Bottom.ToString(),
            style.padding.Left.ToString(),
            style.padding.Right.ToString(),
            style.padding.Top.ToString(),
            style.padding.Bottom.ToString(),
            style.alignItems?.ToString() ?? "-",
            style.alignSelf?.ToString() ?? "-",
            style.justifyItems?.ToString() ?? "-",
            style.justifySelf?.ToString() ?? "-",
            style.alignContent?.ToString() ?? "-",
            style.justifyContent?.ToString() ?? "-",
            style.gap.Width.ToString(),
            style.gap.Height.ToString(),
            DescribeSequence(style.gridTemplateColumns),
            DescribeSequence(style.gridTemplateRows),
            DescribeSequence(style.gridAutoColumns),
            DescribeSequence(style.gridAutoRows),
            style.gridAutoFlow.ToString(),
            style.gridColumn.Start.ToString(),
            style.gridColumn.End.ToString(),
            style.gridRow.Start.ToString(),
            style.gridRow.End.ToString()
        ]);
    }

    private static string DescribeSequence<T>(IEnumerable<T>? values)
    {
        return values == null ? "-" : string.Join("|", values);
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
            var c = Color.HSVToRGB(h, 0.6f, 0.9f);
            Verse.Widgets.DrawBoxSolid(r, c with { a = 0.25f });

            if (Mouse.IsOver(r))
            {
                Verse.Widgets.DrawBox(r, 6, SolidColorMaterials.NewSolidColorTexture(c));
                var s = tree.GetStyle(node);
                var tip =
                    "color:" + $" #{ColorUtility.ToHtmlStringRGB(c)}".Colorize(c) + "\n" +
                    $"rect: {r.width:F0}×{r.height:F0} @ ({r.x:F0},{r.y:F0})\n" +
                    $"display: {s.display}  dir: {s.flexDirection}  wrap: {s.flexWrap}\n" +
                    $"size: {s.size.Width}×{s.size.Height}" +
                    $"  min: {s.minSize.Width}×{s.minSize.Height}" +
                    $"  max: {s.maxSize.Width}×{s.maxSize.Height}" +
                    $"  basis: {s.flexBasis}\n" +
                    $"grow: {s.flexGrow}  shrink: {s.flexShrink}" +
                    $"  gap: {s.gap.Width}×{s.gap.Height}" +
                    $"  padding: {s.padding.ToStringSimple()}, margin: {s.margin.ToStringSimple()}" +
                    $"\nalign-items: {s.alignItems?.ToString() ?? "-"},  justify: {s.justifyContent?.ToString() ?? "-"}";
                TooltipHandler.TipRegion(r, tip);
            }
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

    private static string ToStringSimple<T>(this Rect<T> rect)
    {
        return $"({rect.Left}, {rect.Right}, {rect.Top}, {rect.Bottom})";
    }

    #endregion
}