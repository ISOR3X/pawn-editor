using UnityEngine;
using Verse;
using static Void.Native.NativeMethods;

namespace Void.Native;

/// <summary>
///     Gives each distinct piece of measurable content (<see cref="LeafContext" />) a handle. The native side caches
///     sizes per handle and only calls <see cref="Measure" /> for a mode and width it has not seen for that content.
/// </summary>
internal static unsafe class MeasureRegistry
{
    private static readonly Dictionary<LeafContext, uint> Handles = [];
    private static readonly Dictionary<uint, LeafContext> ByHandle = [];

    /// <summary>Kept in a static field so the delegate the native side holds is never collected.</summary>
    private static readonly void_set_measure_callback_callback_delegate Callback = Measure;

    /// <summary>Log.ErrorOnce identifies a message by a number; any number unique to this message will do.</summary>
    private static readonly int MeasureErrorKey = "Void.Native.MeasureRegistry.Measure".GetHashCode();

    public static int Count => Handles.Count;

    /// <summary>
    ///     Installs the measure callback into the native side.
    /// </summary>
    public static void Install()
    {
        Check(void_set_measure_callback(Callback), nameof(void_set_measure_callback));
    }

    /// <returns>The handle for the content, or 0 when there is nothing to measure.</returns>
    public static uint Get(LeafContext? context)
    {
        if (context?.text == null) return 0;
        if (Handles.TryGetValue(context, out var handle)) return handle;

        Check(void_measure_register(&handle), nameof(void_measure_register));
        // TODO: release handles that have not been used for a while; every distinct string (e.g. each keystroke
        // in an input) currently stays registered for the rest of the session.
        Handles[context] = handle;
        ByHandle[handle] = context;
        return handle;
    }

    /// <summary>
    ///     Called from native code during layout. An exception must never unwind into Rust, so everything is caught.
    /// </summary>
    private static void Measure(uint handle, MeasureMode mode, float width, MeasureSize* result)
    {
        try
        {
            var size = ByHandle.TryGetValue(handle, out var context) ? Measure(context, mode, width) : Vector2.zero;
            *result = new MeasureSize { width = size.x, height = size.y };
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"[{VoidMod.ModName}] Measuring text failed: {e}", MeasureErrorKey);
            *result = default;
        }
    }

    private static Vector2 Measure(LeafContext context, MeasureMode mode, float width)
    {
        var text = context.text!;
        using (new TextBlock(context.fontSize))
        {
            var size = Text.CalcSize(text);

            if (!context.textWrap)
            {
                var w = mode is MeasureMode.Exact or MeasureMode.FitContent ? Mathf.Min(width, size.x) : size.x;
                return new Vector2(w, size.y);
            }

            switch (mode)
            {
                case MeasureMode.Exact:
                    return new Vector2(width, Text.CalcHeight(text, width));
                case MeasureMode.FitContent:
                    {
                        var w = Mathf.Min(width, size.x);
                        return new Vector2(w, Text.CalcHeight(text, w));
                    }
                case MeasureMode.MinContent:
                    {
                        var w = text.Split(' ').Select(word => Text.CalcSize(word).x).Prepend(0f).Max();
                        return new Vector2(w, Text.CalcHeight(text, w));
                    }
                default:
                    return size;
            }
        }
    }
}
