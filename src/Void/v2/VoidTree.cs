using System.Linq;
using Taffy;
using UnityEngine;
using Verse;

namespace Void.v2;

/// <summary>
///     Per-node bookkeeping kept on the C# side: what was last pushed natively (so we can skip
///     pushing it again), plus keyed-reconciliation state for this node's own children.
/// </summary>
internal sealed class NodeRecord(TaffyNode id)
{
    public readonly TaffyNode Id = id;
    public VStyle LastStyle;
    public TreeContext? LastContext;
    public Action<Rect>? Draw;

    // Keyed reconciliation bookkeeping for this node's children.
    public Dictionary<string, TaffyNode> ChildrenByKey = [];
    public List<(string Key, TaffyNode Id)> LastChildOrder = [];
    public List<(string Key, TaffyNode Id)> PendingOrder = [];
}

/// <summary>
///     A persistent Taffy tree with keyed-upsert reconciliation, instead of the "rebuild the whole
///     native tree every frame" pattern the original <c>Void.Taffy.Div</c> uses. Nodes are
///     identified by a caller-supplied key that stays stable across frames; style and context are
///     only pushed to native memory when they actually differ from what was applied last frame, so
///     a frame where nothing changed costs no native calls beyond <see cref="ComputeLayout" />
///     itself.
///     <para>
///         The public surface is deliberately just <see cref="Build" />: the raw
///         <c>Upsert</c>/<c>FinishChildren</c> primitives are <c>internal</c> so a caller can't
///         forget to commit a parent's children — <see cref="ChildBuilder" /> always calls
///         <c>FinishChildren</c> when its scope exits, including on exception.
///     </para>
/// </summary>
public sealed class VoidTree : IDisposable
{
    private readonly TaffyTree<TreeContext> _tree = new();
    private readonly Dictionary<TaffyNode, NodeRecord> _records = [];

    public TaffyNode Root { get; }

    public VoidTree()
    {
        Root = _tree.NewNode();
        _records[Root] = new NodeRecord(Root);
    }

    /// <summary>
    ///     Sets the root's style and runs <paramref name="children" /> against a
    ///     <see cref="ChildBuilder" /> scoped to the root, committing them when it returns (or
    ///     throws) — the single entry point for describing a frame's UI.
    /// </summary>
    public void Build(VStyle rootStyle, Action<ChildBuilder> children)
    {
        SetRootStyle(rootStyle);
        var builder = new ChildBuilder(this, Root);
        try
        {
            children(builder);
        }
        finally
        {
            FinishChildren(Root);
        }
    }

    internal void SetRootStyle(VStyle style) => ApplyStyleIfChanged(Root, style);

    /// <summary>
    ///     Creates or reuses the node for <paramref name="key" /> under <paramref name="parent" />.
    ///     Style/context are only pushed natively if they changed since last frame. Must be
    ///     followed by <see cref="FinishChildren" /> on <paramref name="parent" /> once all of its
    ///     children for this frame have been upserted — <see cref="ChildBuilder" /> is the intended
    ///     caller, which guarantees that.
    /// </summary>
    internal TaffyNode Upsert(TaffyNode parent, string key, VStyle style, TreeContext? context = null,
        bool isLeaf = false, Action<Rect>? draw = null)
    {
        var parentRecord = _records[parent];
        var reused = parentRecord.ChildrenByKey.TryGetValue(key, out var node);
        var resolvedContext = context ?? new TreeContext();

        if (!reused)
        {
            node = isLeaf ? _tree.NewLeafWithContext(resolvedContext) : _tree.NewNode();
            _records[node] = new NodeRecord(node) { LastContext = resolvedContext };
        }
        else if (isLeaf && resolvedContext != _records[node].LastContext)
        {
            _tree.SetNodeContext(node, resolvedContext);
            _records[node].LastContext = resolvedContext;
        }

        ApplyStyleIfChanged(node, style);
        var record = _records[node];
        record.Draw = draw;
        parentRecord.PendingOrder.Add((key, node));
        return node;
    }

    /// <summary>
    ///     Commits this frame's children for <paramref name="parent" />: frees anything that
    ///     disappeared since last frame, and only touches the native children list at all if the
    ///     set or order actually changed.
    /// </summary>
    internal void FinishChildren(TaffyNode parent)
    {
        var record = _records[parent];
        var newByKey = new Dictionary<string, TaffyNode>(record.PendingOrder.Count);
        foreach (var (key, id) in record.PendingOrder) newByKey[key] = id;

        // Anything not re-upserted this frame is gone. RemoveNode also detaches it from its parent,
        // but is shallow, so we recurse ourselves first to free the rest of the removed subtree.
        foreach (var (oldKey, oldId) in record.LastChildOrder)
            if (!newByKey.ContainsKey(oldKey))
                RemoveSubtree(oldId);

        if (!SameOrder(record.LastChildOrder, record.PendingOrder))
        {
            // Detach survivors first so re-appending doesn't create duplicate child entries.
            foreach (var (oldKey, oldId) in record.LastChildOrder)
                if (newByKey.ContainsKey(oldKey))
                    _tree.RemoveChild(parent, oldId);

            foreach (var (_, id) in record.PendingOrder)
                _tree.AppendChild(parent, id);
        }

        record.ChildrenByKey = newByKey;
        record.LastChildOrder = record.PendingOrder;
        record.PendingOrder = [];
    }

    public void ComputeLayout(float width, float height,
        Func<TaffyMeasureMode, float, TaffyMeasureMode, float, TreeContext?, TaffySize> measure)
    {
        _tree.ComputeLayoutWithMeasure(Root, width, height, measure);
    }

    public TaffyLayout GetLayout(TaffyNode node) => _tree.GetLayout(node);

    /// <summary>Walks the tree from the root, invoking each node's draw callback with its computed rect.</summary>
    public void Draw(Rect inRect) => DrawNode(Root, inRect.position);

    /// <summary>
    ///     Default text measurement, mirroring <c>TaffyHelper.MeasureText</c> from
    ///     <c>Window_Benchmark</c> but reading from <see cref="TreeContext" /> and without the
    ///     stray per-call log line.
    /// </summary>
    public static TaffySize DefaultMeasure(
        TaffyMeasureMode widthMode, float width,
        TaffyMeasureMode heightMode, float height,
        TreeContext? context)
    {
        if (context is not { Text: { } text }) return new TaffySize();
        using (new TextBlock(context.Font ?? GameFont.Small))
        {
            switch (widthMode)
            {
                case TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent:
                    return new TaffySize { width = width, height = Text.CalcHeight(text, width) };
                case TaffyMeasureMode.MinContent:
                    var minW = text.Split(' ').Select(w => Text.CalcSize(w).x).Prepend(0f).Max();
                    return new TaffySize { width = minW, height = Text.CalcHeight(text, minW) };
                default:
                    var sz = Text.CalcSize(text);
                    return new TaffySize { width = sz.x, height = sz.y };
            }
        }
    }

    private void DrawNode(TaffyNode node, Vector2 origin)
    {
        var layout = _tree.GetLayout(node);
        var rect = new Rect(origin.x + layout.x, origin.y + layout.y, layout.width, layout.height);

        if (!_records.TryGetValue(node, out var record)) return;

        record.Draw?.Invoke(rect);
        foreach (var (_, childId) in record.LastChildOrder)
            DrawNode(childId, rect.position);
    }

    private void ApplyStyleIfChanged(TaffyNode node, VStyle style)
    {
        var record = _records[node];
        if (style.Equals(record.LastStyle)) return;
        style.Apply(_tree.GetStyle(node));
        record.LastStyle = style;
    }

    private void RemoveSubtree(TaffyNode node)
    {
        if (!_records.TryGetValue(node, out var record)) return;
        foreach (var (_, childId) in record.LastChildOrder)
            RemoveSubtree(childId);
        _tree.RemoveNode(node); // shallow natively — we've already recursed into children ourselves
        _records.Remove(node);
    }

    private static bool SameOrder(List<(string Key, TaffyNode Id)> a, List<(string Key, TaffyNode Id)> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
            if (!a[i].Id.Equals(b[i].Id))
                return false;
        return true;
    }

    public void Dispose() => _tree.Dispose();
}
