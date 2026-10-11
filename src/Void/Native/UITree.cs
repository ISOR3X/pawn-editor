using UnityEngine;
using static Void.Native.NativeMethods;

namespace Void.Native;

public class ScrollState
{
    public Vector2 pos;
    public Rect VisibleRect;
}

/// <summary>
///     A tree of UI nodes, laid out by <c>void_native</c>.
///     <para>
///         <see cref="Build" /> only describes the frame into <see cref="Frame" />. <see cref="Draw" /> hands that
///         description over in a single native call, which diffs it against the previous frame, lays it out when
///         anything changed and writes back one rect per node. Drawing is then a loop over the frame.
///     </para>
///     <para>
///         IMGUI calls a window once per event, several times a frame. The tree is built once per frame and reused
///         by the frame's other events, and the Layout event is skipped altogether: nothing here uses GUILayout.
///         A change made while handling an event (a click) therefore shows up the next frame.
///     </para>
/// </summary>
public unsafe class UITree : IDisposable
{
    internal readonly FrameBuffer Frame = new();
    internal readonly NodeHandles Handles = new();
    internal readonly NodeKeys Keys = new();
    internal readonly StateStore State = new();

    /// <summary>The scroll views currently open while drawing: the index at which each closes, and its state.</summary>
    private readonly List<(int end, ScrollState state)> _openScrollViews = [];

    private readonly ulong[] _removed = new ulong[64];

    /// <summary>The frame (<see cref="Time.frameCount" />) the tree was last built in.</summary>
    private int _builtFrame = -1;

    /// <summary>How many times the tree was built, so a rebuild within a frame is not mistaken for the same build.</summary>
    private int _buildCount;

    private bool _disposed;

    /// <summary>
    ///     Whether an input event was handled since the last build. IMGUI sends several events in a frame (Layout, input,
    ///     Repaint) but a build is only repeated within a frame after one of these: the callbacks of a build hold the
    ///     values it was given, which an input event can change, so the events after it need a fresh build.
    /// </summary>
    private bool _inputSinceBuild;

    /// <summary>The area and build the layouts in <see cref="Frame" /> were computed for, to reuse them if neither changed.</summary>
    private (Rect rect, float availableHeight, int build)? _laidOutFor;

    private float _rootHeight;
    private VoidTree* _native = void_tree_new();

    /// <summary>Nodes in the most recently built frame.</summary>
    public int NodeCount => Frame.Count;

    /// <summary>Whether the most recent native call recomputed layout, rather than reusing the previous one.</summary>
    public bool LastFrameLaidOut { get; private set; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        void_tree_free(_native);
        _native = null;
    }

    public void Build(Action<UIBranch> builder, Style? style = null)
    {
        // Once per frame, unless an input event came in since: see _inputSinceBuild.
        if (_disposed || (_builtFrame == Time.frameCount && !_inputSinceBuild)) return;

        Frame.Clear();
        Keys.BeginFrame();

        var root = Frame.Open(NodeKeys.Root, null, Handles.Style(NodeKeys.Root, style), 0);
        try
        {
            builder(new UIBranch(this, NodeKeys.Root));
        }
        catch
        {
            // A half-built frame has unclosed nodes; draw nothing rather than send it.
            Frame.Clear();
            throw;
        }

        Frame.Close(root);
        _builtFrame = Time.frameCount;
        _buildCount++;
        _inputSinceBuild = false;
    }

    public void Draw(Rect rect)
    {
        LayOutAndDraw(rect, rect.height);
    }

    /// <summary>
    ///     Draws with an unconstrained height and returns the height the content actually needed,
    ///     for windows that resize to fit their contents.
    /// </summary>
    public float DrawMeasured(Rect rect)
    {
        return LayOutAndDraw(rect, float.PositiveInfinity);
    }

    private float LayOutAndDraw(Rect rect, float availableHeight)
    {
        // Read before drawing: a callback can mark the event as used.
        var type = Event.current.type;
        if (_disposed || Frame.Count == 0 || type == EventType.Layout) return _rootHeight;

        if (_laidOutFor != (rect, availableHeight, _buildCount)) LayOut(rect, availableHeight);

        DrawNodes();

        if (CanChangeState(type)) _inputSinceBuild = true;
        return _rootHeight;
    }

    /// <summary>
    ///     Whether handling an event can change what the caller's values are, as opposed to events that only measure or
    ///     paint. Everything else counts: if a kind of event were missing from a list of inputs, its changes would not
    ///     show until the next frame.
    /// </summary>
    private static bool CanChangeState(EventType type)
    {
        return type is not (EventType.Layout or EventType.Repaint or EventType.MouseMove or EventType.Used
            or EventType.Ignore);
    }

    private void LayOut(Rect rect, float availableHeight)
    {
        FrameResult result;
        fixed (NodeRecord* records = Frame.Records)
        fixed (NodeOut* layouts = Frame.Layouts)
        {
            Check(void_frame(_native, records, (uint)Frame.Count, rect.width, availableHeight, rect.x, rect.y,
                layouts, &result), nameof(void_frame));
        }

        _laidOutFor = (rect, availableHeight, _buildCount);
        _rootHeight = result.root_height;
        LastFrameLaidOut = result.laid_out != 0;
        if (result.removed_count > 0) ReleaseRemovedNodes();
    }

    private void ReleaseRemovedNodes()
    {
        uint taken;
        do
        {
            fixed (ulong* buffer = _removed)
            {
                taken = void_take_removed(_native, buffer, (uint)_removed.Length);
            }

            for (var i = 0; i < taken; i++)
            {
                var key = _removed[i];
                State.Remove(key, Keys.Release(key));
                Handles.Forget(key);
            }
        } while (taken == _removed.Length);
    }

    private void DrawNodes()
    {
        var debug = VoidMod.Settings.drawDebug;
        _openScrollViews.Clear();

        // Closes the scroll views even if a draw callback throws, so IMGUI's clip stack stays balanced.
        try
        {
            for (var i = 0; i < Frame.Count; i++)
            {
                // Close the scroll views whose content ended before this node.
                while (_openScrollViews.Count > 0 && i >= _openScrollViews[_openScrollViews.Count - 1].end)
                {
                    Verse.Widgets.EndScrollView();
                    _openScrollViews.RemoveAt(_openScrollViews.Count - 1);
                }

                var layout = Frame.Layouts[i];
                var rect = new Rect(layout.x, layout.y, layout.width, layout.height);

                // Inside a scroll view, skip nodes that are scrolled out of sight, along with their children.
                if (_openScrollViews.Count > 0 && !_openScrollViews[_openScrollViews.Count - 1].state.VisibleRect.Overlaps(rect))
                {
                    i = (int)Frame.Records[i].subtree_end - 1;
                    continue;
                }

                Frame.Draws[i]?.Invoke(rect);
                if (debug) Verse.Widgets.DrawRectFast(rect, Color.red with { a = 0.25f });

                if ((layout.flags & (NODE_SCROLL_X | NODE_SCROLL_Y)) != 0)
                {
                    var state = BeginScrollView(Frame.Records[i].key, rect, layout);
                    _openScrollViews.Add(((int)Frame.Records[i].subtree_end, state));
                }
            }
        }
        finally
        {
            foreach (var _ in _openScrollViews) Verse.Widgets.EndScrollView();
            _openScrollViews.Clear();
        }
    }

    /// <summary>
    ///     Opens a scroll view for a scrolling node. Its children were laid out relative to its content, which is the
    ///     coordinate space inside the scroll view.
    /// </summary>
    private ScrollState BeginScrollView(ulong key, Rect rect, NodeOut layout)
    {
        var scrollX = (layout.flags & NODE_SCROLL_X) != 0;
        var scrollY = (layout.flags & NODE_SCROLL_Y) != 0;
        var state = State.Upsert(key, "scroll", () => new ScrollState());

        var viewRect = new Rect(0f, 0f,
            scrollX ? layout.content_width : layout.width - layout.scrollbar_width,
            scrollY ? layout.content_height : layout.height - layout.scrollbar_height);

        Verse.Widgets.BeginScrollView(rect, ref state.pos, viewRect);
        state.VisibleRect = new Rect(state.pos.x, state.pos.y, rect.width - layout.scrollbar_width,
            rect.height - layout.scrollbar_height);
        return state;
    }
}
