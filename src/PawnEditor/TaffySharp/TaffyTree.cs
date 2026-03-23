// Port of taffy/src/tree/taffy_tree.rs
//
// TaffyTree is the main public entry point for the layout engine.
// It stores the node tree (styles + computed layouts + cache) and
// exposes the API for building UI trees and computing layout.
//
// Phase 2 stub: tree-management and dirty-tracking are complete;
// layout dispatch to compute algorithms will be wired in Phases 3–5.

using System;
using System.Collections.Generic;

namespace PawnEditor.TaffySharp
{
    /// <summary>An error that can occur while accessing or modifying the tree.</summary>
    public class TaffyException : Exception
    {
        public TaffyException(string message) : base(message)
        {
        }
    }

    /// <summary>Per-node data stored inside the tree.</summary>
    internal sealed class NodeData
    {
        public Style Style;
        public Layout UnroundedLayout = Layout.New();
        public Layout FinalLayout = Layout.New();
        public Cache Cache = new Cache();
        public bool IsDirty = true;

        /// <summary>Optional user-attached context object (measure function, etc.).</summary>
        public object? Context;

        public NodeData(Style style)
        {
            Style = style;
        }
    }

    /// <summary>
    /// A node tree that owns <see cref="Style"/> data and computes CSS layout.
    /// <para>
    /// Usage:
    /// <code>
    /// var tree = new TaffyTree();
    /// var root = tree.NewLeaf(new Style { ... });
    /// var child = tree.NewLeaf(new Style { ... });
    /// tree.AppendChild(root, child);
    /// tree.ComputeLayout(root, new Size&lt;AvailableSpace&gt;(
    ///     AvailableSpace.Definite(800), AvailableSpace.Definite(600)));
    /// var layout = tree.Layout(root);
    /// </code>
    /// </para>
    /// </summary>
    public sealed class TaffyTree
    {
        // ── Internal storage ──────────────────────────────────────────────────

        private readonly List<NodeData> _nodes = [];
        private readonly List<List<NodeId>> _children = [];
        private readonly List<NodeId?> _parents = [];

        // Slot-reuse for removed nodes
        private readonly Stack<uint> _freeSlots = new();

        public bool UseRounding = true;

        // ── Node creation ─────────────────────────────────────────────────────

        /// <summary>Creates a new leaf node (no children) with the given style.</summary>
        public NodeId NewLeaf(Style style) => AllocNode(style, null);

        /// <summary>Creates a new leaf node with an attached context object (e.g. a measure function).</summary>
        public NodeId NewLeafWithContext(Style style, object context) => AllocNode(style, context);

        /// <summary>Creates a new container node with the given children.</summary>
        public NodeId NewWithChildren(Style style, IReadOnlyList<NodeId> children)
        {
            var node = AllocNode(style, null);
            foreach (var child in children)
                AppendChild(node, child);
            return node;
        }

        private NodeId AllocNode(Style style, object? context)
        {
            uint id;
            if (_freeSlots.Count > 0)
            {
                id = _freeSlots.Pop();
                _nodes[(int)id] = new NodeData(style) { Context = context };
                _children[(int)id] = [];
                _parents[(int)id] = null;
            }
            else
            {
                id = (uint)_nodes.Count;
                _nodes.Add(new NodeData(style) { Context = context });
                _children.Add([]);
                _parents.Add(null);
            }

            return NodeId.From(id);
        }

        // ── Node removal ──────────────────────────────────────────────────────

        /// <summary>Removes a node and all its children from the tree.</summary>
        public void Remove(NodeId node)
        {
            var id = node.Value;
            // Detach from parent
            if (_parents[(int)id] is NodeId parent)
            {
                _children[(int)parent.Value].Remove(node);
            }

            // Recursively remove children
            var children = new List<NodeId>(_children[(int)id]);
            foreach (var child in children)
                Remove(child);
            // Free slot
            _nodes[(int)id] = null!;
            _children[(int)id] = null!;
            _parents[(int)id] = null;
            _freeSlots.Push(id);
        }

        // ── Child management ──────────────────────────────────────────────────

        /// <summary>Appends <paramref name="child"/> as the last child of <paramref name="parent"/>.</summary>
        public void AppendChild(NodeId parent, NodeId child)
        {
            SetParent(child, parent);
            _children[(int)parent.Value].Add(child);
            MarkDirty(parent);
        }

        /// <summary>Inserts <paramref name="child"/> at <paramref name="index"/> under <paramref name="parent"/>.</summary>
        public void InsertChildAt(NodeId parent, int index, NodeId child)
        {
            SetParent(child, parent);
            _children[(int)parent.Value].Insert(index, child);
            MarkDirty(parent);
        }

        /// <summary>Removes the child at <paramref name="index"/> from <paramref name="parent"/>.</summary>
        public NodeId RemoveChildAt(NodeId parent, int index)
        {
            var children = _children[(int)parent.Value];
            var child = children[index];
            children.RemoveAt(index);
            _parents[(int)child.Value] = null;
            MarkDirty(parent);
            return child;
        }

        /// <summary>Removes a specific child node from <paramref name="parent"/>.</summary>
        public void RemoveChild(NodeId parent, NodeId child)
        {
            _children[(int)parent.Value].Remove(child);
            _parents[(int)child.Value] = null;
            MarkDirty(parent);
        }

        /// <summary>Replaces all children of <paramref name="parent"/> with the given list.</summary>
        public void SetChildren(NodeId parent, IReadOnlyList<NodeId> children)
        {
            // Detach old children
            foreach (var old in _children[(int)parent.Value])
                _parents[(int)old.Value] = null;
            _children[(int)parent.Value].Clear();
            // Attach new children
            foreach (var child in children)
            {
                SetParent(child, parent);
                _children[(int)parent.Value].Add(child);
            }

            MarkDirty(parent);
        }

        private void SetParent(NodeId child, NodeId parent)
        {
            // Detach from previous parent if any
            if (_parents[(int)child.Value] is NodeId oldParent)
                _children[(int)oldParent.Value].Remove(child);
            _parents[(int)child.Value] = parent;
        }

        // ── Style accessors ───────────────────────────────────────────────────

        /// <summary>Returns the style of a node (read-only reference; clone to modify).</summary>
        public Style GetStyle(NodeId node) => _nodes[(int)node.Value].Style;

        /// <summary>Sets the style of a node and marks it dirty.</summary>
        public void SetStyle(NodeId node, Style style)
        {
            _nodes[(int)node.Value].Style = style;
            MarkDirty(node);
        }

        // ── Context accessors ─────────────────────────────────────────────────

        public object? GetContext(NodeId node) => _nodes[(int)node.Value].Context;
        public void SetContext(NodeId node, object? context) => _nodes[(int)node.Value].Context = context;

        // ── Layout accessors ──────────────────────────────────────────────────

        /// <summary>Returns the computed layout of a node. Must call <see cref="ComputeLayout"/> first.</summary>
        public ref Layout Layout(NodeId node) => ref _nodes[(int)node.Value].FinalLayout;

        // ── Tree traversal helpers ────────────────────────────────────────────

        public IReadOnlyList<NodeId> Children(NodeId node) => _children[(int)node.Value];
        public int ChildCount(NodeId node) => _children[(int)node.Value].Count;
        public NodeId ChildAt(NodeId node, int index) => _children[(int)node.Value][index];
        public NodeId? Parent(NodeId node) => _parents[(int)node.Value];

        // ── Dirty flag management ─────────────────────────────────────────────

        /// <summary>Marks <paramref name="node"/> and all its ancestors as dirty (needing re-layout).</summary>
        public void MarkDirty(NodeId node)
        {
            var data = _nodes[(int)node.Value];
            if (data.IsDirty) return; // already dirty — ancestors are also dirty
            data.IsDirty = true;
            data.Cache.Clear();
            if (_parents[(int)node.Value] is NodeId parent)
                MarkDirty(parent);
        }

        public bool IsDirty(NodeId node) => _nodes[(int)node.Value].IsDirty;

        // ── Layout computation ────────────────────────────────────────────────

        /// <summary>
        /// Computes layout for the subtree rooted at <paramref name="root"/>.
        /// Pass the viewport/container size via <paramref name="availableSpace"/>.
        /// </summary>
        public void ComputeLayout(NodeId root, Size<AvailableSpace> availableSpace)
        {
            ComputeLayoutInternal(root, availableSpace);
            if (UseRounding)
                RoundLayout(root, 0f, 0f);
        }

        private void ComputeLayoutInternal(NodeId root, Size<AvailableSpace> availableSpace)
        {
            var input = new LayoutInput
            {
                KnownDimensions = new Size<float?>(null, null),
                ParentSize = availableSpace.IntoOptions(),
                availableSpace = availableSpace,
                SizingMode = SizingMode.InherentSize,
                Axis = RequestedAxis.Both,
                RunMode = RunMode.PerformLayout,
                VerticalMarginsAreCollapsible = new Line<bool>(false, false),
            };

            var output = PerformLayout(root, input);

            // The root node has no parent algorithm to call SetNodeLayout on it,
            // so we apply the computed size directly (mirrors compute_root_layout in Taffy).
            var rootLayout = new TaffySharp.Layout
            {
                Order = 0,
                Size = output.Size,
                ContentSize = output.ContentSize,
            };
            SetNodeLayout(root, rootLayout);
        }

        internal LayoutOutput PerformLayout(NodeId node, LayoutInput input)
        {
            var data = _nodes[(int)node.Value];
            var style = data.Style;

            // Hidden nodes
            if (style.display == Display.None || input.RunMode == RunMode.PerformHiddenLayout)
                return ComputeHiddenLayout(node, 0);

            // Check cache
            if (input.RunMode != RunMode.PerformHiddenLayout)
            {
                var cached = data.Cache.Get(input.KnownDimensions, input.availableSpace, input.RunMode);
                if (cached.HasValue)
                {
                    if (input.RunMode == RunMode.PerformLayout)
                        ApplyCachedLayout(node, cached.Value);
                    return cached.Value;
                }
            }

            // Dispatch to layout algorithm
            LayoutOutput output;
            if (IsLeaf(node))
            {
                output = ComputeLeafLayout(node, input);
            }
            else
            {
                output = style.display switch
                {
                    Display.Flex => ComputeFlexLayout(node, input),
                    Display.Grid => ComputeGridLayout(node, input),
                    Display.Block => ComputeBlockLayout(node, input),
                    _ => ComputeHiddenLayout(node, 0),
                };
            }

            // Store in cache
            data.Cache.Store(input.KnownDimensions, input.availableSpace, input.RunMode, output);

            if (input.RunMode == RunMode.PerformLayout)
            {
                data.UnroundedLayout = data.FinalLayout; // save for rounding step
                data.IsDirty = false;
            }

            return output;
        }

        private bool IsLeaf(NodeId node) => _children[(int)node.Value].Count == 0;

        private void ApplyCachedLayout(NodeId node, LayoutOutput cached)
        {
            // Sizes from cache; location was already set by the parent algorithm
            _nodes[(int)node.Value].FinalLayout.Size = cached.Size;
            _nodes[(int)node.Value].FinalLayout.ContentSize = cached.ContentSize;
        }

        // ── Rounding ──────────────────────────────────────────────────────────

        private void RoundLayout(NodeId node, float cumulativeX, float cumulativeY)
        {
            ref var layout = ref _nodes[(int)node.Value].FinalLayout;
            var absX = cumulativeX + layout.Location.X;
            var absY = cumulativeY + layout.Location.Y;

            layout.Location.X = MathF.Round(absX) - MathF.Round(cumulativeX);
            layout.Location.Y = MathF.Round(absY) - MathF.Round(cumulativeY);
            layout.Size.Width = MathF.Round(absX + layout.Size.Width) - MathF.Round(absX);
            layout.Size.Height = MathF.Round(absY + layout.Size.Height) - MathF.Round(absY);

            foreach (var child in _children[(int)node.Value])
                RoundLayout(child, absX, absY);
        }

        // ── Hidden layout ─────────────────────────────────────────────────────

        private LayoutOutput ComputeHiddenLayout(NodeId node, uint order)
        {
            ref var layout = ref _nodes[(int)node.Value].FinalLayout;
            layout = TaffySharp.Layout.WithOrder(order);

            uint childOrder = 0;
            foreach (var child in _children[(int)node.Value])
                ComputeHiddenLayout(child, childOrder++);

            return LayoutOutput.Hidden;
        }

        // ── Algorithm stubs (wired in Phases 3–5) ────────────────────────────

        private LayoutOutput ComputeLeafLayout(NodeId node, LayoutInput input)
        {
            // Port of taffy/src/compute/leaf.rs: compute_leaf_layout
            // Resolves style size, combines with known dimensions, then calls the measure function if present.
            var style = _nodes[(int)node.Value].Style;

            var parentWidth = input.ParentSize.Width;
            var padding = style.padding.ResolveOrZero(parentWidth);
            var border = style.border.ResolveOrZero(parentWidth);
            var pbSum = SizeF.Add(RectF.SumAxes(padding), RectF.SumAxes(border));
            var boxAdj = style.boxSizing == BoxSizing.ContentBox ? pbSum : SizeF.ZERO;

            Size<float?> nodeSize;
            Size<float?> nodeMinSize;
            Size<float?> nodeMaxSize;

            if (input.SizingMode == SizingMode.ContentSize)
            {
                nodeSize = input.KnownDimensions;
                nodeMinSize = SizeF.NONE;
                nodeMaxSize = SizeF.NONE;
            }
            else
            {
                var ar = style.aspectRatio;
                var styleSize = SizeF.MaybeApplyAspectRatio(
                    style.size.MaybeResolve(input.ParentSize).MaybeAdd(boxAdj), ar);
                var styleMinSize = SizeF.MaybeApplyAspectRatio(
                    style.minSize.MaybeResolve(input.ParentSize).MaybeAdd(boxAdj), ar);
                var styleMaxSize = style.maxSize.MaybeResolve(input.ParentSize).MaybeAdd(boxAdj);

                nodeSize = input.KnownDimensions.Or(styleSize);
                nodeMinSize = styleMinSize;
                nodeMaxSize = styleMaxSize;
            }

            // Call measure function if present; otherwise measured size is zero.
            var measuredSize = SizeF.ZERO;
            if (_nodes[(int)node.Value].Context is Func<Size<float?>, Size<AvailableSpace>, Size<float>> measure)
                measuredSize = measure(nodeSize, input.availableSpace);

            // Combine: prefer known/style size, fall back to measured + padding/border.
            var fallback = new Size<float?>(measuredSize.Width + pbSum.Width, measuredSize.Height + pbSum.Height);
            var clamped = nodeSize.Or(fallback).MaybeClamp(nodeMinSize, nodeMaxSize).MaybeMax(pbSum);
            return LayoutOutput.FromOuterSize(new Size<float>(clamped.Width ?? 0f, clamped.Height ?? 0f));
        }

        private LayoutOutput ComputeFlexLayout(NodeId node, LayoutInput input) =>
            FlexCompute.Compute(this, node, input);

        private LayoutOutput ComputeGridLayout(NodeId node, LayoutInput input)
        {
            // TODO Phase 5: wire to Grid.Compute(...)
            throw new NotImplementedException("CSS Grid layout not yet implemented (Phase 5).");
        }

        private LayoutOutput ComputeBlockLayout(NodeId node, LayoutInput input) =>
            BlockCompute.Compute(this, node, input, null);

        /// <summary>Directly sets a node's layout (used by layout algorithms when placing children).</summary>
        internal void SetNodeLayout(NodeId node, in Layout layout)
        {
            var data = _nodes[(int)node.Value];
            data.FinalLayout = layout;
            data.UnroundedLayout = layout;
        }

        // ── Internal accessor used by compute algorithms ───────────────────────

        internal NodeData GetNodeData(NodeId node) => _nodes[(int)node.Value];
    }
}