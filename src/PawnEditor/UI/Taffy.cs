// TaffySharp integration layer for RimWorld.
//
// Provides a callback-style fluent API backed by TaffySharp's Taffy layout engine.
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

using System;
using System.Collections.Generic;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor
{
    /// <summary>
    /// Fluent layout builder passed to <see cref="Taffy.Row"/> / <see cref="Taffy.Column"/> lambdas.
    /// </summary>
    public sealed class TaffyBuilder
    {
        // Memoizes Text.CalcSize(word).x per (word, font) pair — populated once, reused every frame.
        private static readonly Dictionary<(string word, GameFont font), float> _wordWidthCache = new();

        internal readonly TaffyTree _tree;
        internal readonly List<(NodeId id, Action<Rect>? draw)> _callbacks;
        internal readonly List<NodeId> _children = [];

        internal TaffyBuilder(TaffyTree tree, List<(NodeId id, Action<Rect>? draw)> callbacks)
        {
            _tree = tree;
            _callbacks = callbacks;
        }

        // ── Leaf items ──────────────────────────────────────────────────────────

        /// <summary>Adds a leaf node with per-axis size/grow convenience parameters.</summary>
        public void Item(float? width = null, float? height = null,
            float grow = 0f, float shrink = 1f,
            Action<Rect>? draw = null)
        {
            var style = new Style { flexGrow = grow, flexShrink = shrink };
            if (width.HasValue)
                style.size = style.size.MapWidth(_ => Dimension.Length(width.Value));
            if (height.HasValue)
                style.size = style.size.MapHeight(_ => Dimension.Length(height.Value));
            AddLeaf(style, draw);
        }

        /// <summary>Adds a leaf node with a full TaffySharp <see cref="Style"/>.</summary>
        public void Item(Style style, Action<Rect>? draw = null) => AddLeaf(style, draw);

        // ── Nested row containers ───────────────────────────────────────────────

        /// <summary>Adds a nested row container (grow + optional build).</summary>
        public void Row(float grow = 0f, Action<TaffyBuilder>? build = null)
            => AddContainer(MakeContainerStyle(FlexDirection.Row, grow, null, FlexWrap.NoWrap), build);

        /// <summary>Adds a nested row container with gap and grow.</summary>
        public void Row(float gap, float grow, Action<TaffyBuilder>? build = null)
            => AddContainer(MakeContainerStyle(FlexDirection.Row, grow, gap, FlexWrap.NoWrap), build);

        /// <summary>Adds a nested row container with a full <see cref="Style"/>.</summary>
        public void Row(Style style, Action<TaffyBuilder>? build = null)
            => AddContainer(style, build);

        // ── Nested column containers ────────────────────────────────────────────

        /// <summary>Adds a nested column container (grow + optional build).</summary>
        public void Column(float grow = 0f, Action<TaffyBuilder>? build = null)
            => AddContainer(MakeContainerStyle(FlexDirection.Column, grow, null, FlexWrap.NoWrap), build);

        /// <summary>Adds a nested column container with gap and grow.</summary>
        public void Column(float gap, float grow, Action<TaffyBuilder>? build = null)
            => AddContainer(MakeContainerStyle(FlexDirection.Column, grow, gap, FlexWrap.NoWrap), build);

        /// <summary>Adds a nested column container with a full <see cref="Style"/>.</summary>
        public void Column(Style style, Action<TaffyBuilder>? build = null)
            => AddContainer(style, build);

        // ── Text leaf items (RimWorld integration) ──────────────────────────────

        /// <summary>
        /// Adds a leaf node that measures its own size using RimWorld's <see cref="Text.CalcSize"/>
        /// and <see cref="Text.CalcHeight"/>.
        /// <para>
        /// When a fixed <paramref name="width"/> is set the node wraps at that width and the height
        /// is computed via <see cref="Text.CalcHeight"/>. Otherwise the natural (unwrapped) size from
        /// <see cref="Text.CalcSize"/> is returned, capped at the available width if the axis is definite.
        /// </para>
        /// The default draw callback renders the text as a label.
        /// </summary>
        public void TextItem(string text, float? width = null, float grow = 0f,
            GameFont font = GameFont.Small, Action<Rect>? draw = null)
        {
            var style = new Style { flexGrow = grow };
            if (width.HasValue)
                style.size = style.size.MapWidth(_ => Dimension.Length(width.Value));

            var node = _tree.NewLeafWithContext(style, (Func<Size<float?>, Size<AvailableSpace>, Size<float>>)Measure);
            _children.Add(node);
            _callbacks.Add((node, draw ?? (r =>
            {
                using (new TextBlock(font))
                {
                    Text.WordWrap = r.width < Text.CalcSize(text).x;
                    Log.Message(Text.WordWrap.ToString() + " " + r.width.ToString() + " " + Text.CalcSize(text).x);
                    Verse.Widgets.Label(r, text);
                }
            })));
            return;

            // Store a per-node measure closure as the node's context object.
            // The tree-level dispatch in Execute will cast it and call it.
            Size<float> Measure(Size<float?> known, Size<AvailableSpace> available)
            {
                using (new TextBlock(font))
                {
                    if (known.Width.HasValue)
                        // Width fully constrained by parent algorithm — wrap and measure height.
                        return new Size<float>(known.Width.Value, Text.CalcHeight(text, known.Width.Value));

                    if (available.Width.IsMinContent)
                    {
                        // Min-content query: return the widest unbreakable word.
                        // This mirrors CSS min-width:auto — text can shrink and wrap, but never
                        // below the width of its longest word (which for single-word labels equals
                        // the full text width, preventing unwanted shrinkage).
                        // Results are cached in _wordWidthCache so Text.CalcSize is called at most
                        // once per (word, font) pair across all frames.
                        var minW = 0f;
                        foreach (var word in text.Split(' '))
                        {
                            var key = (word, font);
                            if (!_wordWidthCache.TryGetValue(key, out var w))
                                _wordWidthCache[key] = w = Text.CalcSize(word).x;
                            if (w > minW) minW = w;
                        }
                        var minH = Text.CalcHeight(text, minW);
                        return new Size<float>(minW, minH);
                    }

                    if (available.Width.IntoOption() is { } aw)
                        // Definite available width — wrap at that width.
                        return new Size<float>(aw, Text.CalcHeight(text, aw));

                    // MaxContent / unconstrained — return natural (unwrapped) size.
                    var sz = Text.CalcSize(text);
                    return new Size<float>(sz.x, sz.y);
                }
            }
        }

        // ── Button items ────────────────────────────────────────────────────────

        private const float ButtonIconSize = 24f;
        private const float ButtonIconGap = 4f;

        /// <summary>
        /// Adds a button with auto-computed width.
        /// Width = <see cref="UIUtility.LabelPadding"/> × 2 + label width + gap + icon width (24 px).
        /// Height is always <see cref="UIUtility.ButtonHeight"/>.
        /// Both <paramref name="label"/> and <paramref name="icon"/> are optional.
        /// </summary>
        public void ButtonItem(string? label = null, Texture2D? icon = null,
            Color? iconColor = null, Action? onClick = null)
        {
            // Measure label width at build time (cached across frames).
            var labelW = 0f;
            if (label != null)
            {
                using (new TextBlock(GameFont.Small))
                {
                    var key = (label, GameFont.Small);
                    if (!_wordWidthCache.TryGetValue(key, out labelW))
                        _wordWidthCache[key] = labelW = Text.CalcSize(label).x;
                }
            }

            var totalW = UIUtility.LabelPadding
                       + labelW
                       + (label != null && icon != null ? ButtonIconGap : 0f)
                       + (icon != null ? ButtonIconSize : 0f)
                       + UIUtility.LabelPadding;

            var style = new Style();
            style.size = new Size<Dimension>(
                Dimension.Length(totalW),
                Dimension.Length(UIUtility.ButtonHeight));

            // Capture for closure.
            var capturedLabel = label;
            var capturedIcon = icon;
            var capturedColor = iconColor;
            var capturedLabelW = labelW;

            AddLeaf(style, r =>
            {
                var clicked = Verse.Widgets.ButtonInvisible(r);
                Verse.Widgets.DrawButtonGraphic(r);

                if (capturedLabel != null)
                {
                    using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
                        Verse.Widgets.Label(
                            new Rect(r.x + UIUtility.LabelPadding, r.y, capturedLabelW, r.height),
                            capturedLabel);
                }

                if (capturedIcon != null)
                {
                    var ix = capturedLabel != null
                        ? r.x + UIUtility.LabelPadding + capturedLabelW + ButtonIconGap
                        : r.x + (r.width - ButtonIconSize) / 2f;
                    var iy = r.y + (r.height - ButtonIconSize) / 2f;
                    using (new GUIColor(capturedColor ?? Color.white))
                        GUI.DrawTexture(new Rect(ix, iy, ButtonIconSize, ButtonIconSize), capturedIcon);
                }

                if (clicked) onClick?.Invoke();
            });
        }

        // ── Grid items ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a grid item with optional column/row placement.
        /// All parameters use CSS Grid 1-based line indices.
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

        // ── Internals ───────────────────────────────────────────────────────────

        private void AddLeaf(Style style, Action<Rect>? draw)
        {
            var node = _tree.NewLeaf(style);
            _children.Add(node);
            _callbacks.Add((node, draw));
        }

        private void AddContainer(Style style, Action<TaffyBuilder>? build)
        {
            var inner = new TaffyBuilder(_tree, _callbacks);
            build?.Invoke(inner);
            var node = _tree.NewWithChildren(style, inner._children);
            _children.Add(node);
            _callbacks.Add((node, null));
        }

        private static Style MakeContainerStyle(FlexDirection dir, float grow, float? gap, FlexWrap wrap)
        {
            var s = new Style { flexDirection = dir, flexGrow = grow, flexWrap = wrap };
            if (gap.HasValue)
                s.gap = new Size<LengthPercentage>(
                    LengthPercentage.Length(gap.Value),
                    LengthPercentage.Length(gap.Value));
            return s;
        }
    }

    /// <summary>
    /// Static entry points for TaffySharp-backed layout in RimWorld.
    /// Creates a fresh layout tree per call; layout is computed and draw callbacks invoked before returning.
    /// </summary>
    public static class Taffy
    {
        // ── Entry points ────────────────────────────────────────────────────────

        /// <summary>Lays out children in a row inside <paramref name="rect"/>.</summary>
        public static void Row(Rect rect, Action<TaffyBuilder> build)
            => Execute(rect, new Style { flexDirection = FlexDirection.Row }, build);

        /// <summary>Lays out children in a row with the given gap inside <paramref name="rect"/>.</summary>
        public static void Row(Rect rect, float gap, Action<TaffyBuilder> build)
            => Execute(rect, new Style { flexDirection = FlexDirection.Row, gap = UniformGap(gap) }, build);

        /// <summary>Lays out children in a column inside <paramref name="rect"/>.</summary>
        public static void Column(Rect rect, Action<TaffyBuilder> build)
            => Execute(rect, new Style { flexDirection = FlexDirection.Column }, build);

        /// <summary>Lays out children in a column with the given gap inside <paramref name="rect"/>.</summary>
        public static void Column(Rect rect, float gap, Action<TaffyBuilder> build)
            => Execute(rect, new Style { flexDirection = FlexDirection.Column, gap = UniformGap(gap) }, build);

        // ── Grid entry points ───────────────────────────────────────────────────
        //
        // The autoRowHeight parameter sets gridAutoRows so that implicitly-created rows
        // have a fixed pixel height. This is required when grid items are leaf nodes with
        // no intrinsic size (i.e. draw callbacks) — without it CSS auto rows collapse to 0.
        // Pass 0 only when you supply explicit gridTemplateRows or items with a set size.

        /// <summary>
        /// Lays out children in a CSS Grid with the given column template inside <paramref name="rect"/>.
        /// <paramref name="autoRowHeight"/> sets the height of each auto row in pixels (required when
        /// items have no intrinsic size; otherwise auto rows collapse to 0).
        /// </summary>
        public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
            float autoRowHeight, Action<TaffyBuilder> build)
            => Execute(rect, MakeGridStyle(columns, null, 0f, 0f, autoRowHeight), build);

        /// <summary>Grid layout with uniform gap and fixed auto-row height.</summary>
        public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
            float gap, float autoRowHeight, Action<TaffyBuilder> build)
            => Execute(rect, MakeGridStyle(columns, null, gap, gap, autoRowHeight), build);

        /// <summary>Grid layout with separate column/row gaps and fixed auto-row height.</summary>
        public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
            float gapX, float gapY, float autoRowHeight, Action<TaffyBuilder> build)
            => Execute(rect, MakeGridStyle(columns, null, gapX, gapY, autoRowHeight), build);

        /// <summary>Grid layout with explicit column and row templates (no auto-row height needed).</summary>
        public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
            IReadOnlyList<TrackSizingFunction> rows,
            Action<TaffyBuilder> build)
            => Execute(rect, MakeGridStyle(columns, rows, 0f, 0f, 0f), build);

        /// <summary>Grid layout with explicit templates and uniform gap.</summary>
        public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
            IReadOnlyList<TrackSizingFunction> rows, float gap,
            Action<TaffyBuilder> build)
            => Execute(rect, MakeGridStyle(columns, rows, gap, gap, 0f), build);

        /// <summary>Grid layout with explicit templates and separate column/row gaps.</summary>
        public static void Grid(Rect rect, IReadOnlyList<TrackSizingFunction> columns,
            IReadOnlyList<TrackSizingFunction> rows, float gapX, float gapY,
            Action<TaffyBuilder> build)
            => Execute(rect, MakeGridStyle(columns, rows, gapX, gapY, 0f), build);

        // ── Track sizing shorthands ─────────────────────────────────────────────

        /// <summary>A flexible track that takes the given fraction of remaining space (default 1fr).</summary>
        public static TrackSizingFunction Fr(float fr = 1f) => TrackSizingFunction.Fr(fr);

        /// <summary>A fixed-size track of <paramref name="px"/> pixels.</summary>
        public static TrackSizingFunction Px(float px) => TrackSizingFunction.Px(px);

        /// <summary>An auto-sized track (sized to content, then stretched to fill).</summary>
        public static TrackSizingFunction AutoTrack() => TrackSizingFunction.Auto();

        /// <summary>A percent-sized track relative to the grid container.</summary>
        public static TrackSizingFunction PercentTrack(float pct) => TrackSizingFunction.Percent(pct);

        /// <summary>A fit-content track capped at <paramref name="px"/> pixels.</summary>
        public static TrackSizingFunction FitContent(float px) => TrackSizingFunction.FitContentPx(px);

        // ── Style helpers ───────────────────────────────────────────────────────

        /// <summary>Creates uniform padding on all four sides.</summary>
        public static Rect<LengthPercentage> Padding(float all)
        {
            var v = LengthPercentage.Length(all);
            return new Rect<LengthPercentage>(v, v, v, v);
        }

        /// <summary>Creates asymmetric padding: <paramref name="lr"/> on left/right, <paramref name="tb"/> on top/bottom.</summary>
        public static Rect<LengthPercentage> Padding(float lr, float tb)
        {
            var h = LengthPercentage.Length(lr);
            var v = LengthPercentage.Length(tb);
            return new Rect<LengthPercentage>(h, h, v, v);
        }

        /// <summary>Creates uniform gap on both axes.</summary>
        public static Size<LengthPercentage> Gap(float all) => UniformGap(all);

        /// <summary>Creates asymmetric gap: <paramref name="column"/> between columns, <paramref name="row"/> between rows.</summary>
        public static Size<LengthPercentage> Gap(float column, float row) =>
            new(LengthPercentage.Length(column), LengthPercentage.Length(row));

        // ── Core ────────────────────────────────────────────────────────────────

        private static void Execute(Rect rect, Style rootStyle, Action<TaffyBuilder> build)
        {
            var tree = new TaffyTree();
            var callbacks = new List<(NodeId id, Action<Rect>? draw)>();

            // Give the root container a definite width from the rect so that fr columns resolve
            // correctly. Without this, inner_node_size.Width is None, which causes ExpandFlexibleTracks
            // to use MaxContent semantics: fr fraction = max content of items = 0 for leaf nodes,
            // making all fr columns 0px wide. Height is left Auto so the container shrinks to content.
            rootStyle.size = rootStyle.size.MapWidth(_ => Dimension.Length(rect.width));

            var builder = new TaffyBuilder(tree, callbacks);
            build(builder);
            var root = tree.NewWithChildren(rootStyle, builder._children);

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

            if (lookup.TryGetValue(node, out var draw) && draw != null)
            {
                var prevWordWrap = Text.WordWrap;
                draw(r);
                Text.WordWrap = prevWordWrap;
            }

            foreach (var child in tree.Children(node))
                DrawTree(tree, child, absX, absY, lookup);
        }

        private static Size<LengthPercentage> UniformGap(float v) =>
            new(LengthPercentage.Length(v), LengthPercentage.Length(v));

        private static Style MakeGridStyle(IReadOnlyList<TrackSizingFunction> columns,
            IReadOnlyList<TrackSizingFunction>? rows,
            float gapX, float gapY, float autoRowHeight)
        {
            var s = new Style
            {
                display = TaffySharp.Display.Grid,
                gridTemplateColumns = [..columns],
                gridTemplateRows = rows != null ? [..rows] : null,
                gap = new Size<LengthPercentage>(
                    LengthPercentage.Length(gapX),
                    LengthPercentage.Length(gapY)),
            };
            // When items are leaf nodes with no intrinsic size, CSS auto rows collapse to 0.
            // An explicit autoRowHeight overrides gridAutoRows to give each row a fixed height.
            if (autoRowHeight > 0f)
                s.gridAutoRows = [TrackSizingFunction.Px(autoRowHeight)];
            return s;
        }
    }
}