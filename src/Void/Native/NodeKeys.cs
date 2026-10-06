namespace Void.Native;

/// <summary>
///     A node as seen from C#: its key, which stays the same across frames as long as the node keeps its place.
/// </summary>
public readonly record struct UINode(ulong Key);

/// <summary>
///     Where a node sits in the tree: its parent, its sibling key (call site or explicit id) and, when siblings share
///     that key, which of them it is in call order.
/// </summary>
internal readonly record struct NodePath(ulong Parent, string Sibling, int Occurrence);

/// <summary>
///     Hands out node keys: small numbers that stay the same for as long as a node keeps its <see cref="NodePath" />.
///     Keys of removed nodes are reused, so they stay dense and the native side can keep its nodes in a plain array
///     indexed by key.
/// </summary>
internal sealed class NodeKeys
{
    public const ulong Root = 0;

    private readonly Stack<ulong> _free = [];
    private readonly Dictionary<NodePath, ulong> _keys = [];

    /// <summary>How often each parent/sibling key pair was used this frame, to tell repeated siblings apart.</summary>
    private readonly Dictionary<(ulong parent, string sibling), int> _occurrences = [];

    /// <summary>The path of each key in use, indexed by key. The root has no path.</summary>
    private readonly List<NodePath> _paths = [default];

    public void BeginFrame()
    {
        _occurrences.Clear();
    }

    public ulong Child(ulong parent, string sibling)
    {
        _occurrences.TryGetValue((parent, sibling), out var occurrence);
        _occurrences[(parent, sibling)] = occurrence + 1;

        var path = new NodePath(parent, sibling, occurrence);
        if (_keys.TryGetValue(path, out var key)) return key;

        if (_free.Count > 0)
        {
            key = _free.Pop();
            _paths[(int)key] = path;
        }
        else
        {
            key = (ulong)_paths.Count;
            _paths.Add(path);
        }

        _keys[path] = key;
        return key;
    }

    /// <summary>
    ///     Makes the key of a node the native side reported removed available again.
    /// </summary>
    /// <returns>Where the node was.</returns>
    public NodePath Release(ulong key)
    {
        var path = _paths[(int)key];
        _keys.Remove(path);
        _free.Push(key);
        return path;
    }
}
