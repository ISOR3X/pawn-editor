using Taffy;
using UnityEngine;
using Verse;

namespace Void.Taffy;

/// <summary>
///     Context used for calculating the size of a leaf.
///     Modifying context marks a leaf dirty.
///     TODO: Rename to something more befitting of the function, e.g. MeasureSpec.
/// </summary>
public record LeafContext
{
    public GameFont fontSize = GameFont.Small;

    public string? text;
    public TextAnchor textAnchor = TextAnchor.UpperLeft;
    public bool textWrap = true;
}

/// <summary>
///     Stores relevant data across frames about a branch on the C# side to prevent a large amount of FFI calls.
///     Used for diff-checking and storing the actual draw callback.
///     Note that context is tracked internally in the TaffyTree as its also required for content measuring.
/// </summary>
public class BranchRecord
{
    /// <summary>
    ///     The child branches/ leaves of a branch
    /// </summary>
    public Dictionary<string, TaffyNode> childrenByKey = [];

    /// <summary>
    ///     For keeping track of state of children across frames.
    /// </summary>
    public Dictionary<string, object>? childState;


    public Action<Rect>? draw;

    /// <summary>
    ///     The pending children by key. This is build up every frame and cleared at the end.
    ///     It is used to check if the children have changed across frames.
    /// </summary>
    public Dictionary<string, TaffyNode> pendingChildrenByKey = [];

    /// <summary>
    ///     Track the previous style of a branch so we can diff-check it.
    /// </summary>
    public Style? prevStyle;
}

public class UITree : IDisposable
{
    /// <summary>
    ///     Records keyed by their branch node.
    /// </summary>
    private readonly Dictionary<TaffyNode, BranchRecord> _branchRecordsByNode = [];

    /// <summary>
    ///     The node (id) of the root branch.
    /// </summary>
    private readonly TaffyNode _rootBranchNode;

    /// <summary>
    ///     The native tree. We use a private version of the tree instead of subclassing it to control the public API.
    /// </summary>
    private readonly TaffyTree<LeafContext> _tree = new();

    /// <summary>
    // Tracks whether anything changed since last frame
    /// </summary>
    private bool _dirtyThisFrame = true;

    /// <summary>
    ///     For some reason <c>DoWindowContents</c> calls <c>UITree.Draw</c> even after <c>PostClose</c> is called.
    ///     This is a fix for that to properly dispose the tree.
    /// </summary>
    private bool _disposed;

    private float _lastAvailableHeight = -1f;
    private float _lastAvailableWidth = -1f;

    public UITree()
    {
        _rootBranchNode = _tree.NewNode();
        _branchRecordsByNode[_rootBranchNode] = new BranchRecord();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tree.Dispose();
    }

    public void Build(Action<UIBranch> builder, Style? style = null)
    {
        if (_disposed) return;

        var branch = new UIBranch(this, _rootBranchNode);
        var rootRecord = _branchRecordsByNode[_rootBranchNode];

        // If the styles are not equal, and the new style isn't null, update the native style.
        if (!Utility.NullableEquals(style, rootRecord.prevStyle, false) && style != null)
        {
            _dirtyThisFrame = true;
            var _branchStyle = _tree.GetStyle(_rootBranchNode);
            style.Push(_branchStyle);
            _tree.SetStyle(_rootBranchNode, _branchStyle);
        }

        // Update the style history regardless of if its null or not.
        rootRecord.prevStyle = style;

        builder(branch);
        CompareChildren(_rootBranchNode);
    }

    public void Draw(Rect rect)
    {
        if (_disposed) return;

        var sizeChanged = !Mathf.Approximately(rect.width, _lastAvailableWidth)
                          || !Mathf.Approximately(rect.height, _lastAvailableHeight);

        if (_dirtyThisFrame || sizeChanged)
        {
            _tree.ComputeLayoutWithMeasure(_rootBranchNode, rect.width, rect.height, DefaultMeasure);

            _dirtyThisFrame = false;
            _lastAvailableWidth = rect.width;
            _lastAvailableHeight = rect.height;
        }

        DrawNode(_rootBranchNode, rect.position);
    }

    /// <summary>
    ///     Draws with an unconstrained height and returns the height the content actually needed,
    ///     for windows that resize to fit their contents.
    /// </summary>
    public float DrawMeasured(Rect rect)
    {
        if (_disposed) return 0f;

        // Mathf.Approximately(inf, inf) is false, so the height term is an explicit infinity check.
        var sizeChanged = !Mathf.Approximately(rect.width, _lastAvailableWidth)
                          || !float.IsPositiveInfinity(_lastAvailableHeight);

        if (_dirtyThisFrame || sizeChanged)
        {
            _tree.ComputeLayoutWithMeasure(_rootBranchNode, rect.width, float.PositiveInfinity, DefaultMeasure);

            _dirtyThisFrame = false;
            _lastAvailableWidth = rect.width;
            _lastAvailableHeight = float.PositiveInfinity;
        }

        DrawNode(_rootBranchNode, rect.position);
        return _tree.GetLayout(_rootBranchNode).height;
    }

    private void DrawNode(TaffyNode node, Vector2 origin)
    {
        var layout = _tree.GetLayout(node);
        var rect = new Rect(origin.x + layout.x, origin.y + layout.y, layout.width, layout.height);

        if (!_branchRecordsByNode.TryGetValue(node, out var record)) return;

        record.draw?.Invoke(rect);
        if (VoidMod.Settings.drawDebug) Verse.Widgets.DrawRectFast(rect, Color.red with { a = 0.25f });

        foreach (var (_, childId) in record.childrenByKey)
            DrawNode(childId, rect.position);
    }

    /// <summary>
    ///     Update or insert new state into the parent node.
    ///     Note that state must be a reference type
    /// </summary>
    public T UpsertState<T>(TaffyNode parentNode, string key, Func<T> init) where T : class
    {
        var parentRecord = _branchRecordsByNode[parentNode];
        parentRecord.childState ??= [];

        if (parentRecord.childState.TryGetValue(key, out var existing)) return (T)existing;

        var created = init();
        parentRecord.childState[key] = created;
        return created;
    }

    /// <summary>
    ///     Update or insert a new branch into the parent node.
    /// </summary>
    public TaffyNode UpsertBranch(TaffyNode parentNode, string key, Action<Rect>? draw = null,
        LeafContext? context = null, Style? style = null)
    {
        var parentRecord = _branchRecordsByNode[parentNode];
        var exists = parentRecord.childrenByKey.TryGetValue(key, out var branchNode);

        // Check if the branch exists according to the parent record
        if (!exists)
        {
            // Create a new node and record
            branchNode = context != null ? _tree.NewLeafWithContext(context) : _tree.NewNode();
            _branchRecordsByNode[branchNode] = new BranchRecord();

            // Update the parent
            parentRecord.childrenByKey[key] = branchNode;
            _tree.AppendChild(parentNode, branchNode);
            _dirtyThisFrame = true;
        }

        var branchRecord = _branchRecordsByNode[branchNode];

        // Apply style and context if they have changed and are not null.
        if (!Utility.NullableEquals(style, branchRecord.prevStyle, false) && style != null)
        {
            _dirtyThisFrame = true;
            var _branchStyle = _tree.GetStyle(branchNode);
            style.Push(_branchStyle);
            _tree.SetStyle(branchNode, _branchStyle);
        }

        if (!Utility.NullableEquals(context, _tree.GetNodeContext(branchNode), false) && context != null)
        {
            _dirtyThisFrame = true;
            _tree.SetNodeContext(branchNode, context);
        }

        // Always update the branchRecord with the values of the current frame.
        branchRecord.prevStyle = style;
        branchRecord.draw = draw;

        parentRecord.pendingChildrenByKey[key] = branchNode;

        return branchNode;
    }

    /// <summary>
    ///     Compare the children of the previous and current frame.
    ///     First compare if the length and order are the same,
    ///     then check for added/ removed items.
    ///     If differences are found, free the removed branches.
    /// </summary>
    public void CompareChildren(TaffyNode node)
    {
        var branchRecord = _branchRecordsByNode[node];
        var prev = branchRecord.childrenByKey;
        var pending = branchRecord.pendingChildrenByKey;

        // Early return for leaves and empty branches
        if (prev.Count == 0 && pending.Count == 0) return;

        if (!SameOrder(prev, pending))
        {
            _dirtyThisFrame = true;

            // Remove all branches not present since last frame.
            foreach (var (key, childNode) in prev)
                if (!pending.ContainsKey(key))
                {
                    // Detach first. Taffy's remove_child marks the parent dirty, but remove alone
                    // does not, and the parent's cached layout would keep the child's size.
                    _tree.RemoveChild(node, childNode);
                    RemoveSubtree(childNode);
                    branchRecord.childState?.Remove(key);
                }
        }

        branchRecord.childrenByKey = pending;
        branchRecord.pendingChildrenByKey = prev;
        prev.Clear();
    }

    /// <summary>
    ///     While SameOrder checks for the order of children, if just the order of the array has changed and not the length,
    ///     No changes are actually pushed to the native tree. In the future we could use <c>set-children</c> to fix this.
    /// </summary>
    private static bool SameOrder(Dictionary<string, TaffyNode> a, Dictionary<string, TaffyNode> b)
    {
        if (a.Count != b.Count) return false;

        // Struct enumerators, so this walks both without allocating.
        using var ea = a.Keys.GetEnumerator();
        using var eb = b.Keys.GetEnumerator();
        while (ea.MoveNext() && eb.MoveNext())
            if (ea.Current != eb.Current)
                return false;
        return true;
    }

    private void RemoveSubtree(TaffyNode node)
    {
        if (_branchRecordsByNode.TryGetValue(node, out var record))
        {
            foreach (var childNode in record.childrenByKey.Values)
                RemoveSubtree(childNode);
            _branchRecordsByNode.Remove(node);
        }

        _tree.RemoveNode(node);
    }

    /// <summary>
    ///     Measure function for nodes with context.
    ///     This calculates the needed size for items with a yet unknown size.
    ///     <summary />
    private static TaffySize DefaultMeasure(
        TaffyMeasureMode widthMode, float width,
        TaffyMeasureMode heightMode, float height,
        LeafContext? context)
    {
        if (context is null || context.text == null) return new TaffySize();
        using (new TextBlock(context.fontSize))
        {
            // Wrapping
            if (!context.textWrap)
            {
                var sz = Text.CalcSize(context.text);
                var w = widthMode is TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent
                    ? Mathf.Min(width, sz.x)
                    : sz.x;
                return new TaffySize { width = w, height = sz.y };
            }

            // No wrapping
            switch (widthMode)
            {
                case TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent:
                    return new TaffySize { width = width, height = Text.CalcHeight(context.text, width) };
                case TaffyMeasureMode.MinContent:
                {
                    var minW = context.text.Split(' ').Select(w => Text.CalcSize(w).x).Prepend(0f).Max();
                    return new TaffySize { width = minW, height = Text.CalcHeight(context.text, minW) };
                }
                default:
                {
                    var sz = Text.CalcSize(context.text);
                    return new TaffySize { width = sz.x, height = sz.y };
                }
            }
        }
    }
}

internal static class Utility
{
    public static bool NullableEquals<T>(T? x, T? y, bool equalIfNull = true)
    {
        if (x is null && y is null) return equalIfNull;
        return EqualityComparer<T>.Default.Equals(x!, y!);
    }
}
