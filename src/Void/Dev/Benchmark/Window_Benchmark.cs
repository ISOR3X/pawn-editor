using System.Diagnostics;
using LudeonTK;
using UnityEngine;
using Verse;

namespace Void.Dev.Benchmark;

/// <summary>
///     Shared setup for the three benchmark windows: each takes a third of the screen at full height, and none can be
///     moved or resized, so they always draw the same area.
/// </summary>
public abstract class Window_Benchmark : Window
{
    private readonly int _slot;

    protected Window_Benchmark(string title, int slot, bool dynamic)
    {
        _slot = slot;
        Dynamic = dynamic;
        // The title is also what the timings are logged under, so the two scenarios stay apart.
        optionalTitle = dynamic ? title + " (dynamic)" : title;
        doCloseX = true;
        draggable = false;
        resizeable = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
    }

    /// <summary>
    ///     Whether the window runs the dynamic scenario: nodes added and removed on a timer, a ticker that changes, and
    ///     a list that scrolls by itself. Otherwise the content never changes.
    /// </summary>
    protected bool Dynamic { get; }

    public override Vector2 InitialSize => new(UI.screenWidth / 3f, UI.screenHeight);

    /// <summary>Times <see cref="DrawContents" /> for <see cref="BenchmarkTimings" />.</summary>
    public sealed override void DoWindowContents(Rect inRect)
    {
        var start = Stopwatch.GetTimestamp();
        DrawContents(inRect);
        BenchmarkTimings.Add(optionalTitle, Stopwatch.GetTimestamp() - start);
    }

    protected abstract void DrawContents(Rect inRect);

    public override void SetInitialSizeAndPosition()
    {
        var width = UI.screenWidth / 3f;
        windowRect = new Rect(_slot * width, 0f, width, UI.screenHeight).Rounded();
    }

    /// <summary>
    ///     Opens fresh instances of all three windows (closing any that are open), so they start from the same state.
    ///     Their content never changes.
    /// </summary>
    [DebugAction("Void", "Open benchmark windows", allowedGameStates = AllowedGameStates.Invalid)]
    public static void OpenAll()
    {
        Open(false);
    }

    /// <summary>
    ///     The same, running the dynamic scenario (see <see cref="Dynamic" />). What happens in a frame depends only on
    ///     the frame number, so all three windows do the same at the same time.
    /// </summary>
    [DebugAction("Void", "Open dynamic benchmark windows", allowedGameStates = AllowedGameStates.Invalid)]
    public static void OpenAllDynamic()
    {
        Open(true);
    }

    private static void Open(bool dynamic)
    {
        Find.WindowStack.TryRemove(typeof(Window_BenchmarkVanilla));
        Find.WindowStack.TryRemove(typeof(Window_BenchmarkVoid));
        Find.WindowStack.TryRemove(typeof(Window_BenchmarkNative));

        Find.WindowStack.Add(new Window_BenchmarkVanilla(dynamic));
        Find.WindowStack.Add(new Window_BenchmarkVoid(dynamic));
        Find.WindowStack.Add(new Window_BenchmarkNative(dynamic));
    }
}
