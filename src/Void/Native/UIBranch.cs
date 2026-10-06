using System.Runtime.CompilerServices;
using UnityEngine;
using RenderContext = Void.Taffy.RenderContext;

namespace Void.Native;

/// <summary>
///     Builder for creating a branch in a tree.
/// </summary>
public class UIBranch
{
    private static readonly Dictionary<(string file, int line), string> CallerKeys = [];
    private readonly ulong _key;
    private readonly UITree _tree;

    internal UIBranch(UITree tree, ulong key, RenderContext? context = null)
    {
        _tree = tree;
        _key = key;
        Context = context ?? RenderContext.Empty;
    }

    private RenderContext Context { get; }

    /// <summary>
    ///     State stored on the branch node, saved across frames.
    ///     Should only store light component data, e.g. scroll position & input buffers.
    /// </summary>
    public T UpsertState<T>(string key, Func<T> init) where T : class
    {
        return _tree.State.Upsert(_key, key, init);
    }

    /// <summary>
    ///     Get state for the branch node by key. Instead of calling an initializer when state isnt found, this returns null.
    /// </summary>
    public T? GetState<T>(string key) where T : class
    {
        return _tree.State.Get<T>(_key, key);
    }

    /// <summary>
    ///     Extend the context for a subtree.
    ///     Note that while it creates a new UIBranch instance it does not create any new nodes on the native tree.
    /// </summary>
    public void Provide<T>(string key, T value, Action<UIBranch> builder)
    {
        builder(new UIBranch(_tree, _key, Context.With(key, value)));
    }

    /// <summary>
    ///     Read the context for a subtree.
    /// </summary>
    public T Inject<T>(string key)
    {
        return Context.Get<T>(key);
    }

    /// <summary>
    ///     Read the context for a subtree when a component can render without it.
    /// </summary>
    public bool TryInject<T>(string key, out T value)
    {
        return Context.TryGet(key, out value);
    }

    public UINode Div(Action<UIBranch>? builder = null, Action<Rect>? draw = null, LeafContext? context = null,
        Style? style = null, string? id = null, [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        var key = _tree.Keys.Child(_key, ResolveKey(id, file, line));
        var index = _tree.Frame.Open(key, draw, StyleRegistry.Get(style), MeasureRegistry.Get(context));

        // Children append themselves after this node; Close records where its subtree ends.
        // Context rides along to children; the tree itself never stores it.
        builder?.Invoke(new UIBranch(_tree, key, Context));

        _tree.Frame.Close(index);
        return new UINode(key);
    }

    /// <summary>
    ///     Explicit id if given, otherwise a key for the call site, built once and reused.
    /// </summary>
    public static string ResolveKey(string? id, string? file, int line)
    {
        if (id != null) return id;
        if (file == null) throw new ArgumentNullException(nameof(file), "No id and no caller file path.");
        if (CallerKeys.TryGetValue((file, line), out var key)) return key;
        return CallerKeys[(file, line)] = $"{file}_{line}";
    }
}
