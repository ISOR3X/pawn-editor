using UnityEngine;

namespace Void.Native;

/// <summary>
///     One frame's description of the tree. Index <c>i</c> is the same node in all three arrays: its
///     <see cref="NodeRecord" /> (in pre-order, so a parent comes before its children), its draw callback, and the
///     layout the native side writes back for it.
/// </summary>
internal sealed class FrameBuffer
{
    private const int InitialCapacity = 64;

    public int Count;
    public Action<Rect>?[] Draws = new Action<Rect>?[InitialCapacity];
    public NodeOut[] Layouts = new NodeOut[InitialCapacity];
    public NodeRecord[] Records = new NodeRecord[InitialCapacity];

    public void Clear()
    {
        // Release the previous frame's callbacks so whatever they captured can be collected.
        Array.Clear(Draws, 0, Count);
        Count = 0;
    }

    /// <summary>
    ///     Appends a node and returns its index. Its children are the nodes appended until the matching
    ///     <see cref="Close" />.
    /// </summary>
    public int Open(ulong key, Action<Rect>? draw, uint style, uint measure)
    {
        if (Count == Records.Length)
        {
            Array.Resize(ref Records, Count * 2);
            Array.Resize(ref Draws, Count * 2);
            Array.Resize(ref Layouts, Count * 2);
        }

        Records[Count] = new NodeRecord { key = key, style = style, measure = measure };
        Draws[Count] = draw;
        return Count++;
    }

    public void Close(int index)
    {
        Records[index].subtree_end = (uint)Count;
    }
}
