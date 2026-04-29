// Port of taffy/src/compute/grid/explicit_grid.rs
//
// Supports repeat(auto-fill, …) and repeat(auto-fit, …).

namespace Taffy;

internal static class ExplicitGrid
{
    // ── ComputeExplicitGridSizeInAxis ─────────────────────────────────────

    /// <summary>
    ///     Returns the number of explicit tracks in the given axis, expanding any
    ///     auto-fill/auto-fit repeat() based on the available container size.
    /// </summary>
    public static ushort ComputeExplicitGridSizeInAxis(
        Style style, float? autoFitContainerSize, bool useMaxRepetitions, AbsoluteAxis axis)
    {
        var template = axis == AbsoluteAxis.Horizontal
            ? style.gridTemplateColumns
            : style.gridTemplateRows;

        if (template == null || template.Count == 0)
            return 0;

        // Invalid if any repeat() group contains zero tracks.
        foreach (var comp in template)
            if (comp.IsRepeat && comp.AsRepeat().Tracks.Count == 0)
                return 0;

        // Count non-auto-repeating tracks and auto-repeat definitions.
        ushort nonAutoCount = 0;
        var autoRepeatDefs = new List<GridTemplateRepeat>();
        foreach (var comp in template)
            if (comp.IsSingle)
            {
                nonAutoCount++;
            }
            else
            {
                var rep = comp.AsRepeat();
                if (rep.Count == GridTrackRepetition.AutoFill || rep.Count == GridTrackRepetition.AutoFit)
                    autoRepeatDefs.Add(rep);
                // Integer repeat not supported; treated as zero tracks (invalid template → 0 below).
            }

        // Validation: at most one auto-repeat, and every track must have a fixed component.
        if (autoRepeatDefs.Count > 1) return 0;
        if (autoRepeatDefs.Count == 1)
        {
            var allFixed = template.All(c =>
                c.IsSingle
                    ? c.AsSingle().HasFixedComponent()
                    : c.AsRepeat().Tracks.All(t => t.HasFixedComponent()));
            if (!allFixed) return 0;
        }

        if (autoRepeatDefs.Count == 0)
            return nonAutoCount;

        var repeatDef = autoRepeatDefs[0];
        var numReps = ComputeAutoRepetitions(template, repeatDef, autoFitContainerSize, useMaxRepetitions, style, axis);
        return (ushort)(nonAutoCount + (ushort)repeatDef.Tracks.Count * numReps);
    }

    private static ushort ComputeAutoRepetitions(
        List<GridTemplateComponent> template,
        GridTemplateRepeat repeatDef,
        float? autoFitContainerSize,
        bool useMaxRepetitions,
        Style style,
        AbsoluteAxis axis)
    {
        if (autoFitContainerSize == null) return 1;

        var container = autoFitContainerSize.Value;
        var gapSize = axis == AbsoluteAxis.Horizontal
            ? style.gap.Width.ResolveOrZero(container)
            : style.gap.Height.ResolveOrZero(container);

        // Space used by non-repeating (single) tracks.
        var nonRepSpace = 0f;
        ushort nonAutoTrackCount = 0;
        foreach (var comp in template)
        {
            if (!comp.IsSingle) continue;
            nonRepSpace += TrackDefiniteValue(comp.AsSingle(), container);
            nonAutoTrackCount++;
        }

        var repTrackCount = (ushort)repeatDef.Tracks.Count;
        var perRepTrackSpace = repeatDef.Tracks.Sum(t => TrackDefiniteValue(t, container));

        // First repetition + non-repeating tracks, including gaps between all of them.
        var firstRepSpace = nonRepSpace
                            + perRepTrackSpace
                            + MathF.Max(0f, nonAutoTrackCount + repTrackCount - 1) * gapSize;

        if (firstRepSpace > container) return 1;

        var perRepUsed = perRepTrackSpace + repTrackCount * gapSize;
        if (perRepUsed <= 0f) return 1;

        var additionalReps = (container - firstRepSpace) / perRepUsed;
        var reps = useMaxRepetitions
            ? (ushort)(MathF.Floor(additionalReps) + 1)
            : (ushort)(MathF.Ceiling(additionalReps) + 1);
        return Math.Max((ushort)1, reps);
    }

    // "Treating each track as its max function if definite, else its min function."
    private static float TrackDefiniteValue(TrackSizingFunction t, float parentSize)
    {
        var max = t.Max.DefiniteValue(parentSize);
        var min = t.Min.DefiniteValue(parentSize);
        return max.HasValue ? MathF.Max(max.Value, min ?? 0f) : min ?? 0f;
    }

    // ── InitializeGridTracks ──────────────────────────────────────────────

    /// <summary>
    ///     Populates <paramref name="tracks" /> with all grid tracks and gutters for the given axis.
    /// </summary>
    public static void InitializeGridTracks(
        List<GridTrack> tracks,
        TrackCounts counts,
        Style style,
        AbsoluteAxis axis,
        Func<int, bool> trackHasItems)
    {
        var template = axis == AbsoluteAxis.Horizontal
            ? style.gridTemplateColumns
            : style.gridTemplateRows;
        var autoTracks = axis == AbsoluteAxis.Horizontal
            ? style.gridAutoColumns
            : style.gridAutoRows;
        var gap = axis == AbsoluteAxis.Horizontal ? style.gap.Width : style.gap.Height;

        var autoTrackCount = autoTracks?.Count ?? 0;

        tracks.Clear();
        var capacity = counts.Len() * 2 + 1;
        if (tracks.Capacity < capacity) tracks.Capacity = capacity;
        tracks.Add(GridTrack.NewGutter(gap));

        // ── Negative implicit tracks ──────────────────────────────────────
        if (counts.NegativeImplicit > 0)
        {
            var offset = autoTrackCount == 0
                ? 0
                : autoTrackCount - counts.NegativeImplicit % autoTrackCount;
            CreateImplicitTracks(tracks, counts.NegativeImplicit, autoTracks, offset, gap);
        }

        var currentTrackIndex = (int)counts.NegativeImplicit;

        // ── Explicit tracks ───────────────────────────────────────────────
        if (counts.Explicit > 0 && template != null && template.Count > 0)
        {
            // Count non-auto-repeating tracks (for computing repeat expansion size).
            ushort nonAutoRepeatingCount = 0;
            foreach (var comp in template)
                if (comp.IsSingle)
                    nonAutoRepeatingCount++;

            var explicitEnd = counts.NegativeImplicit + counts.Explicit;

            foreach (var comp in template)
            {
                if (currentTrackIndex >= explicitEnd) break;

                if (comp.IsSingle)
                {
                    var def = comp.AsSingle();
                    tracks.Add(GridTrack.NewTrack(def.Min, def.Max));
                    tracks.Add(GridTrack.NewGutter(gap));
                    currentTrackIndex++;
                }
                else
                {
                    var repeat = comp.AsRepeat();
                    var isAutoFit = repeat.Count == GridTrackRepetition.AutoFit;
                    var expandCount = counts.Explicit - nonAutoRepeatingCount;

                    for (var i = 0; i < expandCount && currentTrackIndex < explicitEnd; i++)
                    {
                        var trackDef = repeat.Tracks[i % repeat.Tracks.Count];
                        var track = GridTrack.NewTrack(trackDef.Min, trackDef.Max);
                        var gutter = GridTrack.NewGutter(gap);

                        if (isAutoFit && !trackHasItems(currentTrackIndex))
                        {
                            track.Collapse();
                            gutter.Collapse();
                        }

                        tracks.Add(track);
                        tracks.Add(gutter);
                        currentTrackIndex++;
                    }

                    // For auto-fit at end: collapse trailing gutters of collapsed tracks.
                    if (isAutoFit && currentTrackIndex == counts.Len())
                        for (var i = tracks.Count - 1; i >= 0; i--)
                        {
                            if (tracks[i].Kind == GridTrackKind.Track && !tracks[i].IsCollapsed) break;
                            tracks[i].Collapse();
                        }
                }
            }
        }

        // ── Positive implicit tracks ──────────────────────────────────────
        var gridAreaTracks = counts.NegativeImplicit + counts.Explicit - currentTrackIndex;
        CreateImplicitTracks(tracks, (ushort)(counts.PositiveImplicit + gridAreaTracks), autoTracks, 0, gap);

        tracks[0].Collapse();
        tracks[tracks.Count - 1].Collapse();
    }

    private static void CreateImplicitTracks(
        List<GridTrack> tracks, ushort count, List<TrackSizingFunction>? autoTracks, int offset, LengthPercentage gap)
    {
        var autoCount = autoTracks?.Count ?? 0;
        for (var i = 0; i < count; i++)
        {
            var def = autoCount > 0 && autoTracks != null
                ? autoTracks[(offset + i) % autoCount]
                : TrackSizingFunction.Auto();
            tracks.Add(GridTrack.NewTrack(def.Min, def.Max));
            tracks.Add(GridTrack.NewGutter(gap));
        }
    }
}