using System.Runtime.CompilerServices;
using Taffy;
using UnityEngine;

namespace Void.Taffy;

/// <summary>
///     Builder for creating a branch in a tree.
/// </summary>
public class UIBranch(UITree tree, TaffyNode parentBranch, RenderContext? context = null)
{
    private static readonly Dictionary<(string file, int line), string> CallerKeys = [];
    private readonly TaffyNode _parentBranch = parentBranch;
    private readonly UITree _tree = tree;
    private RenderContext Context { get; } = context ?? RenderContext.Empty;

    /// <summary>
    ///     State stored on the branch node, saved across frames.
    ///     Should only store light component data, e.g. scroll position & input buffers.
    /// </summary>
    public T State<T>(string key, Func<T> init) where T : class
    {
        return _tree.UpsertState(_parentBranch, key, init);
    }

    /// <summary>
    ///     Extend the context for a subtree.
    ///     Note that while it creates a new UIBranch instance it does not create any new nodes on the native tree.
    /// </summary>
    public void Provide<T>(T value, Action<UIBranch> builder)
    {
        builder(new UIBranch(_tree, _parentBranch, Context.With(value)));
    }

    /// <summary>
    ///     Read the context for a subtree. Throws when no <typeparamref name="T" /> was provided.
    /// </summary>
    public T Inject<T>()
    {
        return Context.Get<T>();
    }

    /// <summary>
    ///     Read the context for a subtree when a component can render without it.
    /// </summary>
    public bool TryInject<T>(out T value)
    {
        return Context.TryGet(out value);
    }


    public TaffyNode Div(Action<UIBranch>? builder = null, Action<Rect>? draw = null, LeafContext? context = null,
        Style? style = null, string? id = null, [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = ResolveKey(id, file, line);
        var node = _tree.UpsertBranch(_parentBranch, key, draw, context, style);

        if (builder != null)
        {
            // Create a new branch to which the children can attach.
            // Context rides along to children; the tree itself never stores it.
            var childBranch = new UIBranch(_tree, node, Context);
            builder?.Invoke(childBranch);
        }

        // After the builder is called, all (this frame's) children should be attached.
        // Compare them to last frame and update the snapshot accordingly.
        // Also marks the native tree dirty when differences are found.
        _tree.CompareChildren(node);

        return node;
    }

    /// <summary>
    ///     Explicit id if given, otherwise a key for the call site, built once and reused.
    /// </summary>
    internal static string ResolveKey(string? id, string? file, int line)
    {
        if (id != null) return id;
        if (file == null) throw new ArgumentNullException(nameof(file), "No id and no caller file path.");
        if (CallerKeys.TryGetValue((file, line), out var key)) return key;
        return CallerKeys[(file, line)] = $"{file}_{line}";
    }
}