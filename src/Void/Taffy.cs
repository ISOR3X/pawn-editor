// Taffy integration layer for RimWorld.
//
// Provides a callback-style fluent API backed by the ctaffy native layout engine.
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

namespace Void;

/// <summary>
///     A delegate for custom node measurement. Mirrors the ctaffy measure callback signature.
///     <list type="bullet">
///     <item><see cref="TaffyMeasureMode.Exact"/> — the value is a fixed constraint.</item>
///     <item><see cref="TaffyMeasureMode.FitContent"/> — the value is definite available space.</item>
///     <item><see cref="TaffyMeasureMode.MinContent"/> — min-content size query; value is +∞.</item>
///     <item><see cref="TaffyMeasureMode.MaxContent"/> — unconstrained; value is +∞.</item>
///     </list>
/// </summary>
public delegate (float width, float height) TaffyMeasureFunc(
    TaffyMeasureMode widthMode, float width,
    TaffyMeasureMode heightMode, float height);

/// <summary>
///     Fluent layout builder passed to <see cref="Taffy.Div" /> lambdas.
/// </summary>
public sealed class TaffyBuilder
{
    // Memoizes Text.CalcSize(word).x per (word, font) pair — populated once, reused every frame.
    public static readonly Dictionary<(string word, GameFont font), float> WordWidthCache = [];
    public readonly List<(TaffyNode id, Action<Rect>? draw)> callbacks;
    public readonly List<TaffyNode> children = [];
    public readonly TaffyTree tree;

    public TaffyBuilder(TaffyTree tree, List<(TaffyNode id, Action<Rect>? draw)> callbacks)
    {
        this.tree = tree;
        this.callbacks = callbacks;
    }

    /// <summary>
    ///     Stable identifier for the current context, used by stateful extensions like
    ///     <c>TaffyExtensions.Input(ref string)</c> to key their per-widget persistent state.
    /// </summary>
    public string? ContextKey { get; set; }

    // ── Grid items ──────────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a grid item with optional column/row placement (1-based CSS Grid indices).
    ///     TODO: deprecate and remove
    /// </summary>
    public void GridItem(Action<Rect>? draw = null,
        int colSpan = 1, int rowSpan = 1,
        int? colStart = null, int? rowStart = null)
    {
        var style = new StyleOverride();

        if (colStart.HasValue || colSpan > 1)
        {
            var p = new TaffyGridPlacement();
            if (colStart.HasValue) p.start = (short)colStart.Value;
            if (colSpan > 1) p.span = (ushort)colSpan;
            style.gridColumn = p;
        }

        if (rowStart.HasValue || rowSpan > 1)
        {
            var p = new TaffyGridPlacement();
            if (rowStart.HasValue) p.start = (short)rowStart.Value;
            if (rowSpan > 1) p.span = (ushort)rowSpan;
            style.gridRow = p;
        }

        AddNode(style, draw);
    }

    #region LEAF ITEMS

    public void Item(Action<Rect>? draw = null, StyleOverride? style = null)
    {
        AddNode(style ?? new StyleOverride(), draw);
    }

    public void Div(Action<TaffyBuilder>? builder = null, StyleOverride? style = null)
    {
        AddContainer(style ?? new StyleOverride(), null, builder);
    }

    /// <summary>
    ///     Adds a container that runs <paramref name="draw" /> on its own rect before drawing children.
    /// </summary>
    public void Div(Action<Rect>? draw = null, Action<TaffyBuilder>? builder = null, StyleOverride? style = null)
    {
        AddContainer(style ?? new StyleOverride(), draw, builder);
    }

    #endregion

    #region HELPERS

    internal void AddNode(StyleOverride style, Action<Rect>? draw, TaffyMeasureFunc? measure = null)
    {
        var node = tree.NewNode();
        style.Apply(tree.GetStyle(node));
        if (measure != null)
        {
            tree.SetMeasureFunction(node, (wm, w, hm, h) =>
            {
                var (width, height) = measure(wm, w, hm, h);
                return new TaffySize { width = width, height = height };
            });
        }
        children.Add(node);
        callbacks.Add((node, draw));
    }

    private void AddContainer(StyleOverride style, Action<Rect>? draw, Action<TaffyBuilder>? build)
    {
        var inner = new TaffyBuilder(tree, callbacks) { ContextKey = ContextKey };
        build?.Invoke(inner);
        var node = tree.NewNode();
        style.Apply(tree.GetStyle(node));
        foreach (var child in inner.children) tree.AppendChild(node, child);
        children.Add(node);
        callbacks.Add((node, draw));
    }

    #endregion
}

/// <summary>
///     Static entry points for ctaffy-backed layout in RimWorld.
///     Creates a fresh layout tree per call; layout is computed and draw callbacks invoked before returning.
/// </summary>
public static class Taffy
{
    #region ENTRY POINTS

    /// <summary>
    ///     RimWorld entry point for the Taffy layout engine.
    /// </summary>
    public static void Div(Rect rect, Action<TaffyBuilder> build, StyleOverride? style = null)
    {
        Execute(rect, style ?? new StyleOverride(), build);
    }

    #endregion

    /// <summary>
    ///     Like <see cref="Div" /> but lays out with unconstrained height, draws all content,
    ///     and returns the computed content height. Used for scrollable containers.
    /// </summary>
    public static float DivMeasured(Rect rect, Action<TaffyBuilder> build, StyleOverride? style = null)
    {
        return ExecuteMeasured(rect, style ?? new StyleOverride(), build);
    }

    private static float ExecuteMeasured(Rect rect, StyleOverride rootStyle, Action<TaffyBuilder> build)
    {
        using var tree = new TaffyTree();
        var callbacks = new List<(TaffyNode id, Action<Rect>? draw)>();

        var builder = new TaffyBuilder(tree, callbacks);
        build(builder);

        var root = tree.NewNode();
        var s = tree.GetStyle(root);
        rootStyle.Apply(s);
        // Width is definite; +∞ maps to MaxContent in ctaffy, giving unconstrained height.
        s.Width = Dimension.Px(rect.width);
        s.Height = Dimension.Auto();
        foreach (var child in builder.children) tree.AppendChild(root, child);

        tree.ComputeLayout(root, rect.width, float.PositiveInfinity);

        var lookup = BuildLookup(callbacks);
        DrawTree(tree, root, rect.x, rect.y, lookup);
        return tree.GetLayout(root).height;
    }
    #region CORE

    private static void Execute(Rect rect, StyleOverride rootStyle, Action<TaffyBuilder> build)
    {
        using var tree = new TaffyTree();
        var callbacks = new List<(TaffyNode id, Action<Rect>? draw)>();

        // Give the root a definite size so fr columns resolve correctly.
        var builder = new TaffyBuilder(tree, callbacks);
        build(builder);

        var root = tree.NewNode();
        var s = tree.GetStyle(root);
        rootStyle.Apply(s);
        s.Width = Dimension.Px(rect.width);
        s.Height = Dimension.Px(rect.height);
        foreach (var child in builder.children) tree.AppendChild(root, child);

        tree.ComputeLayout(root, rect.width, rect.height);

        var lookup = BuildLookup(callbacks);
        DrawTree(tree, root, rect.x, rect.y, lookup);
    }

    private static Dictionary<TaffyNode, Action<Rect>?> BuildLookup(List<(TaffyNode id, Action<Rect>? draw)> callbacks)
    {
        var lookup = new Dictionary<TaffyNode, Action<Rect>?>(callbacks.Count);
        foreach (var (id, draw) in callbacks) lookup[id] = draw;
        return lookup;
    }

    private static void DrawTree(TaffyTree tree, TaffyNode node, float originX, float originY,
        Dictionary<TaffyNode, Action<Rect>?> lookup)
    {
        var layout = tree.GetLayout(node);
        var absX = originX + layout.x;
        var absY = originY + layout.y;
        var r = new Rect(absX, absY, layout.width, layout.height);

        if (VoidMod.Settings.drawDebug)
        {
            var h = node.GetHashCode() * 0.618033988f % 1f;
            var c = Color.HSVToRGB(h, 0.6f, 0.9f);
            Verse.Widgets.DrawBoxSolid(r, c with { a = 0.25f });

            if (Mouse.IsOver(r))
            {
                Verse.Widgets.DrawBox(r, 6, SolidColorMaterials.NewSolidColorTexture(c));
                var style = tree.GetStyle(node);
                var tip =
                    "color:" + $" #{ColorUtility.ToHtmlStringRGB(c)}".Colorize(c) + "\n" +
                    $"rect: {r.width:F0}×{r.height:F0} @ ({r.x:F0},{r.y:F0})\n" +
                    $"display: {style.Display}  dir: {style.FlexDirection}  wrap: {style.FlexWrap}\n" +
                    $"size: {style.Width}×{style.Height}" +
                    $"  min: {style.MinWidth}×{style.MinHeight}" +
                    $"  max: {style.MaxWidth}×{style.MaxHeight}" +
                    $"  basis: {style.FlexBasis}\n" +
                    $"grow: {style.FlexGrow}  shrink: {style.FlexShrink}" +
                    $"  gap: {style.ColumnGap}×{style.RowGap}" +
                    $"  padding: T{style.PaddingTop} R{style.PaddingRight} B{style.PaddingBottom} L{style.PaddingLeft}" +
                    $"  margin: T{style.MarginTop} R{style.MarginRight} B{style.MarginBottom} L{style.MarginLeft}" +
                    $"\nalign-items: {style.AlignItems?.ToString() ?? "-"},  justify: {style.JustifyContent?.ToString() ?? "-"}";
                TooltipHandler.TipRegion(r, tip);
            }
        }

        if (lookup.TryGetValue(node, out var draw) && draw != null)
        {
            var prevWordWrap = Text.WordWrap;
            draw(r);
            Text.WordWrap = prevWordWrap;
        }

        var childCount = tree.ChildCount(node);
        for (var i = 0; i < childCount; i++)
            DrawTree(tree, tree.ChildAt(node, i), absX, absY, lookup);
    }

    #endregion
}
