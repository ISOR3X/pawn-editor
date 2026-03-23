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

using System;
using System.Collections.Generic;
using PawnEditor.TaffySharp;
using UnityEngine;

namespace PawnEditor
{
    /// <summary>
    /// Fluent layout builder passed to <see cref="Taffy.Row"/> / <see cref="Taffy.Column"/> lambdas.
    /// </summary>
    public sealed class TaffyBuilder
    {
        internal readonly TaffyTree _tree;
        internal readonly List<(NodeId id, Action<Rect>? draw)> _callbacks;
        internal readonly List<NodeId> _children = new List<NodeId>();

        internal TaffyBuilder(TaffyTree tree, List<(NodeId id, Action<Rect>? draw)> callbacks)
        {
            _tree     = tree;
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
            new Size<LengthPercentage>(LengthPercentage.Length(column), LengthPercentage.Length(row));

        // ── Core ────────────────────────────────────────────────────────────────

        private static void Execute(Rect rect, Style rootStyle, Action<TaffyBuilder> build)
        {
            var tree      = new TaffyTree();
            var callbacks = new List<(NodeId id, Action<Rect>? draw)>();

            var builder = new TaffyBuilder(tree, callbacks);
            build(builder);
            var root = tree.NewWithChildren(rootStyle, builder._children);

            tree.ComputeLayout(root, new Size<AvailableSpace>(
                AvailableSpace.Definite(rect.width),
                AvailableSpace.Definite(rect.height)));

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

            if (lookup.TryGetValue(node, out var draw))
                draw?.Invoke(r);

            foreach (var child in tree.Children(node))
                DrawTree(tree, child, absX, absY, lookup);
        }

        private static Size<LengthPercentage> UniformGap(float v) =>
            new Size<LengthPercentage>(LengthPercentage.Length(v), LengthPercentage.Length(v));
    }
}
