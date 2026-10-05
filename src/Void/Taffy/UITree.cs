using UnityEngine;
using Verse;
using Void.Native;

namespace Void.Taffy;

public class ScrollState
{
    public Vector2 pos;
    public Rect VisibleRect;
}

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
///     A node as seen from C#: its key, which stays the same across frames as long as the node keeps its place.
/// </summary>
public readonly record struct UINode(ulong Key);

/// <summary>
///     A tree of UI nodes, laid out by <c>void_native</c>.
///     <para>
///         <see cref="Build" /> only writes a description of the frame: one <see cref="NodeRecord" /> per node, in
///         pre-order, with the draw callback at the same index in <see cref="_draws" />. <see cref="Draw" /> hands
///         the whole description over in a single native call, which diffs it against the previous frame, lays it
///         out when anything changed and fills <see cref="_out" /> with one rect per record. Drawing is then a flat
///         loop over the three arrays.
///     </para>
/// </summary>
public class UITree : IDisposable
{
    private const ulong RootKey = 0x56_4F_49_44_52_4F_4F_54; // "VOIDROOT"

    private readonly List<int> _openScrollViews = [];

    /// <summary>
    ///     Keys of the states each node owns, so they can be dropped when the node disappears.
    /// </summary>
    private readonly Dictionary<ulong, HashSet<ulong>> _ownedState = [];

    /// <summary>
    ///     Component state, keyed by <see cref="NodeKey.Child" /> of the owning node and the state key. A component
    ///     that stores state on its parent under its own id therefore uses its node's key, and loses the state
    ///     together with the node, as before.
    /// </summary>
    private readonly Dictionary<ulong, object> _state = [];

    /// <summary>Keys handed out this frame, used to give repeated sibling keys a unique suffix.</summary>
    private readonly HashSet<ulong> _usedKeys = [];

    private int _count;
    private bool _disposed;
    private Action<Rect>?[] _draws = new Action<Rect>?[64];
    private IntPtr _native;
    private NodeOut[] _out = new NodeOut[64];
    private NodeRecord[] _records = new NodeRecord[64];
    private ulong[] _removed = new ulong[64];

    public UITree()
    {
        _native = VoidNative.void_tree_new();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        VoidNative.void_tree_free(_native);
        _native = IntPtr.Zero;
    }

    public void Build(Action<UIBranch> builder, Style? style = null)
    {
        if (_disposed) return;

        var previousCount = _count;
        _count = 0;
        _usedKeys.Clear();
        _usedKeys.Add(RootKey);

        var root = Open(RootKey, null, null, style);
        try
        {
            builder(new UIBranch(this, RootKey));
        }
        catch
        {
            // A half-built frame has unclosed nodes; draw nothing rather than send it.
            _count = 0;
            throw;
        }

        Close(root);

        // Drop callbacks left over from a larger previous frame so they can be collected.
        if (previousCount > _count) Array.Clear(_draws, _count, previousCount - _count);
    }

    public void Draw(Rect rect)
    {
        Frame(rect, rect.height);
    }

    /// <summary>
    ///     Draws with an unconstrained height and returns the height the content actually needed,
    ///     for windows that resize to fit their contents.
    /// </summary>
    public float DrawMeasured(Rect rect)
    {
        return Frame(rect, float.PositiveInfinity);
    }

    private unsafe float Frame(Rect rect, float availableHeight)
    {
        if (_disposed || _count == 0) return 0f;

        if (_out.Length < _count) _out = new NodeOut[_records.Length];

        FrameResult result;
        fixed (NodeRecord* records = _records)
        fixed (NodeOut* output = _out)
        {
            VoidNative.Check(
                VoidNative.void_frame(_native, records, (uint)_count, rect.width, availableHeight, rect.x, rect.y,
                    output, &result), nameof(VoidNative.void_frame));
        }

        if (result.removedCount > 0) DropRemovedState();

        DrawNodes();
        return result.rootHeight;
    }

    private void DrawNodes()
    {
        var debug = VoidMod.Settings.drawDebug;
        _openScrollViews.Clear();

        // Closes the scroll views even if a draw callback throws, so IMGUI's clip stack stays balanced.
        try
        {
            for (var i = 0; i < _count; i++)
            {
                while (_openScrollViews.Count > 0 && i >= _openScrollViews[_openScrollViews.Count - 1])
                {
                    Verse.Widgets.EndScrollView();
                    _openScrollViews.RemoveAt(_openScrollViews.Count - 1);
                }

                var o = _out[i];
                var rect = new Rect(o.x, o.y, o.width, o.height);

                _draws[i]?.Invoke(rect);
                if (debug) Verse.Widgets.DrawRectFast(rect, Color.red with { a = 0.25f });

                if ((o.flags & (NodeOut.ScrollX | NodeOut.ScrollY)) == 0) continue;

                var scrollX = (o.flags & NodeOut.ScrollX) != 0;
                var scrollY = (o.flags & NodeOut.ScrollY) != 0;
                var s = UpsertState(_records[i].key, "scroll", () => new ScrollState { pos = Vector2.zero });

                var viewRect = new Rect(0f, 0f,
                    scrollX ? o.contentWidth : o.width - o.scrollbarWidth,
                    scrollY ? o.contentHeight : o.height - o.scrollbarHeight);

                Verse.Widgets.BeginScrollView(rect, ref s.pos, viewRect);
                s.VisibleRect = new Rect(s.pos.x, s.pos.y, rect.width - o.scrollbarWidth,
                    rect.height - o.scrollbarHeight);
                _openScrollViews.Add((int)_records[i].subtreeEnd);
            }
        }
        finally
        {
            for (var n = _openScrollViews.Count; n > 0; n--) Verse.Widgets.EndScrollView();
            _openScrollViews.Clear();
        }
    }

    private unsafe void DropRemovedState()
    {
        while (true)
        {
            uint taken;
            fixed (ulong* buffer = _removed)
            {
                taken = VoidNative.void_take_removed(_native, buffer, (uint)_removed.Length);
            }

            for (var i = 0; i < taken; i++)
            {
                var key = _removed[i];
                _state.Remove(key);
                if (!_ownedState.TryGetValue(key, out var owned)) continue;
                foreach (var stateKey in owned) _state.Remove(stateKey);
                _ownedState.Remove(key);
            }

            if (taken < _removed.Length) return;
        }
    }

    /// <summary>
    ///     Appends a node to this frame and returns its index. Its children are the nodes appended until the
    ///     matching <see cref="Close" />.
    /// </summary>
    internal int Open(ulong key, Action<Rect>? draw, LeafContext? context, Style? style)
    {
        if (_count == _records.Length)
        {
            Array.Resize(ref _records, _records.Length * 2);
            Array.Resize(ref _draws, _draws.Length * 2);
        }

        _records[_count] = new NodeRecord
        {
            key = key,
            style = StyleRegistry.Get(style),
            measure = MeasureRegistry.Get(context)
        };
        _draws[_count] = draw;
        return _count++;
    }

    internal void Close(int index)
    {
        _records[index].subtreeEnd = (uint)_count;
    }

    /// <summary>
    ///     The key of a child of <paramref name="parentKey" />. Repeated sibling keys (e.g. a component called in a
    ///     loop without an id) get a stable suffix in call order, like <c>key#1</c>, <c>key#2</c>.
    /// </summary>
    internal ulong ResolveChildKey(ulong parentKey, string siblingKey)
    {
        var key = NodeKey.Child(parentKey, siblingKey);
        if (_usedKeys.Add(key)) return key;

        for (ulong n = 1; ; n++)
        {
            var candidate = NodeKey.Mix(key + n);
            if (_usedKeys.Add(candidate)) return candidate;
        }
    }

    /// <summary>
    ///     Update or insert state owned by <paramref name="ownerKey" />. Note that state must be a reference type.
    /// </summary>
    internal T UpsertState<T>(ulong ownerKey, string key, Func<T> init) where T : class
    {
        var stateKey = NodeKey.Child(ownerKey, key);
        if (_state.TryGetValue(stateKey, out var existing)) return (T)existing;

        var created = init();
        _state[stateKey] = created;
        if (!_ownedState.TryGetValue(ownerKey, out var owned)) _ownedState[ownerKey] = owned = [];
        owned.Add(stateKey);
        return created;
    }

    internal T? GetState<T>(ulong ownerKey, string key) where T : class
    {
        return _state.TryGetValue(NodeKey.Child(ownerKey, key), out var existing) ? (T)existing : null;
    }
}

/// <summary>
///     Node keys: 64-bit hashes of the path from the root, so the native side can match nodes across frames
///     without strings.
/// </summary>
internal static class NodeKey
{
    public static ulong Child(ulong parentKey, string siblingKey)
    {
        return Mix(Mix(parentKey) ^ Hash(siblingKey));
    }

    /// <summary>FNV-1a over the UTF-16 code units.</summary>
    public static ulong Hash(string s)
    {
        var h = 14695981039346656037UL;
        foreach (var c in s)
        {
            h ^= c;
            h *= 1099511628211UL;
        }

        return h;
    }

    /// <summary>splitmix64 finalizer.</summary>
    public static ulong Mix(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }
}
