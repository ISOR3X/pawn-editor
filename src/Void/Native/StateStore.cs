namespace Void.Native;

/// <summary>
///     Component state, kept per node across frames until the node disappears.
/// </summary>
internal sealed class StateStore
{
    private readonly Dictionary<ulong, Dictionary<string, object>> _byNode = [];

    /// <summary>
    ///     The state stored on <paramref name="node" /> under <paramref name="key" />, created with
    ///     <paramref name="init" /> the first time. State must be a reference type so components can change it.
    /// </summary>
    public T Upsert<T>(ulong node, string key, Func<T> init) where T : class
    {
        if (!_byNode.TryGetValue(node, out var states)) _byNode[node] = states = [];
        if (states.TryGetValue(key, out var existing)) return (T)existing;

        var created = init();
        states[key] = created;
        return created;
    }

    public T? Get<T>(ulong node, string key) where T : class
    {
        return _byNode.TryGetValue(node, out var states) && states.TryGetValue(key, out var existing)
            ? (T)existing
            : null;
    }

    /// <summary>
    ///     Drops the state of a removed node. Components keep their state on their parent under their own sibling key,
    ///     so that is dropped as well.
    /// </summary>
    public void Remove(ulong node, NodePath path)
    {
        _byNode.Remove(node);
        if (path.Occurrence == 0 && _byNode.TryGetValue(path.Parent, out var parentStates))
            parentStates.Remove(path.Sibling);
    }
}
