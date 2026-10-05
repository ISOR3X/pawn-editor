using Verse;
using Void.Taffy;

namespace Void.Native;

/// <summary>
///     Interns measurable content (<see cref="LeafContext" />). Everything that does not depend on the available
///     width is measured once here; the native side answers those queries itself and only calls
///     <see cref="Height" /> for wrapping text at a width it has not cached yet.
/// </summary>
internal static unsafe class MeasureRegistry
{
    private static readonly Dictionary<LeafContext, uint> Handles = [];
    private static readonly Dictionary<uint, LeafContext> ByHandle = [];

    /// <summary>Kept in a static field so the delegate the native side holds is never collected.</summary>
    private static readonly VoidNative.HeightCallback Callback = Height;

    public static int Count => Handles.Count;

    public static void Install()
    {
        VoidNative.Check(VoidNative.void_set_height_callback(Callback), nameof(VoidNative.void_set_height_callback));
    }

    /// <returns>The handle for the content, or 0 when there is nothing to measure.</returns>
    public static uint Get(LeafContext? context)
    {
        if (context?.text == null) return 0;
        if (Handles.TryGetValue(context, out var handle)) return handle;

        var desc = Describe(context);
        VoidNative.Check(VoidNative.void_measure_register(&desc, &handle), nameof(VoidNative.void_measure_register));
        // TODO: release handles that have not been used for a while; every distinct string (e.g. each keystroke
        // in an input) currently stays registered for the rest of the session.
        Handles[context] = handle;
        ByHandle[handle] = context;
        return handle;
    }

    private static MeasureDesc Describe(LeafContext context)
    {
        var text = context.text!;
        using (new TextBlock(context.fontSize))
        {
            var size = Text.CalcSize(text);
            var minContent = size.x;
            if (context.textWrap)
            {
                minContent = 0f;
                foreach (var word in text.Split(' '))
                    minContent = Math.Max(minContent, Text.CalcSize(word).x);
            }

            return new MeasureDesc
            {
                maxWidth = size.x,
                maxHeight = size.y,
                minContentWidth = minContent,
                wraps = context.textWrap ? 1u : 0u
            };
        }
    }

    /// <summary>
    ///     Called from native code during layout. An exception must never unwind into Rust, so everything is caught.
    /// </summary>
    private static float Height(uint handle, float width)
    {
        try
        {
            if (!ByHandle.TryGetValue(handle, out var context) || context.text == null) return 0f;
            using (new TextBlock(context.fontSize))
            {
                return Text.CalcHeight(context.text, width);
            }
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"[{VoidMod.ModName}] Measuring text failed: {e}", 0x56_4F_49_44);
            return 0f;
        }
    }
}
