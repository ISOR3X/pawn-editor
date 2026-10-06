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
/// </summary>
public unsafe class UITree : IDisposable
{
    internal readonly FrameBuffer Frame = new();
    internal readonly NodeKeys Keys = new();
    internal readonly StateStore State = new();

    /// <summary>For each scroll view currently open while drawing, the index at which it closes.</summary>
    private readonly List<int> _openScrollViews = [];

    private readonly ulong[] _removed = new ulong[64];
    private bool _disposed;
    private VoidTree* _native = void_tree_new();

    /// <summary>Nodes in the most recently built frame.</summary>
    public int NodeCount => Frame.Count;

    /// <summary>Whether the most recent draw recomputed layout, rather than reusing the previous one.</summary>
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
        if (_disposed) return;

        Frame.Clear();
        Keys.BeginFrame();

        var root = Frame.Open(NodeKeys.Root, null, StyleRegistry.Get(style), 0);
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
        if (_disposed || Frame.Count == 0) return 0f;

        FrameResult result;
        fixed (NodeRecord* records = Frame.Records)
        fixed (NodeOut* layouts = Frame.Layouts)
        {
            Check(void_frame(_native, records, (uint)Frame.Count, rect.width, availableHeight, rect.x, rect.y,
                layouts, &result), nameof(void_frame));
        }

        LastFrameLaidOut = result.laid_out != 0;
        if (result.removed_count > 0) ReleaseRemovedNodes();

        DrawNodes();
        return result.root_height;
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
                while (_openScrollViews.Count > 0 && i >= _openScrollViews[_openScrollViews.Count - 1])
                {
                    Verse.Widgets.EndScrollView();
                    _openScrollViews.RemoveAt(_openScrollViews.Count - 1);
                }

                var layout = Frame.Layouts[i];
                var rect = new Rect(layout.x, layout.y, layout.width, layout.height);

                Frame.Draws[i]?.Invoke(rect);
                if (debug) Verse.Widgets.DrawRectFast(rect, Color.red with { a = 0.25f });

                if ((layout.flags & (NODE_SCROLL_X | NODE_SCROLL_Y)) != 0)
                {
                    BeginScrollView(Frame.Records[i].key, rect, layout);
                    _openScrollViews.Add((int)Frame.Records[i].subtree_end);
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
    private void BeginScrollView(ulong key, Rect rect, NodeOut layout)
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
    }
}
