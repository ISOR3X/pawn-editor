using System.Diagnostics;
using UnityEngine;
using Verse;

namespace Void.Dev.Benchmark;

/// <summary>
///     Times the benchmark windows without a profiler, so there is no per-method instrumentation overhead in the
///     numbers. Every <see cref="ReportInterval" /> seconds the averages per frame are written to the log and reset.
/// </summary>
internal static class BenchmarkTimings
{
    private const float ReportInterval = 10f;

    private static readonly Dictionary<string, Totals> ByWindow = [];
    private static float _lastReport = -1f;

    public static void Add(string window, long ticks)
    {
        if (!ByWindow.TryGetValue(window, out var totals)) ByWindow[window] = totals = new Totals();
        totals.ticks += ticks;
        totals.calls++;
        if (totals.lastFrame != Time.frameCount)
        {
            totals.lastFrame = Time.frameCount;
            totals.frames++;
        }

        var now = Time.realtimeSinceStartup;
        if (_lastReport < 0f) _lastReport = now;
        if (now - _lastReport >= ReportInterval) Report(now);
    }

    private static void Report(float now)
    {
        var lines = ByWindow.OrderBy(w => w.Key).Select(w =>
        {
            var t = w.Value;
            var msPerFrame = t.ticks * 1000.0 / Stopwatch.Frequency / Math.Max(1, t.frames);
            var callsPerFrame = (double)t.calls / Math.Max(1, t.frames);
            return $"  {w.Key}: {msPerFrame:0.000} ms/frame ({callsPerFrame:0.0} calls/frame, {t.frames} frames)";
        });
        Log.Message($"[Void benchmark] last {now - _lastReport:0} s\n{string.Join("\n", lines)}");

        ByWindow.Clear();
        _lastReport = now;
    }

    private sealed class Totals
    {
        public long calls;
        public long frames;
        public int lastFrame = -1;
        public long ticks;
    }
}
