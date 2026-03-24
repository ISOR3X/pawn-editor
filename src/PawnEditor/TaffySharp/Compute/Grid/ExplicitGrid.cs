// Port of taffy/src/compute/grid/explicit_grid.rs
//
// Helpers for initialising GridTracks from the style.
// Note: This port does NOT support repeat() in grid-template-columns/rows.
// gridTemplateColumns and gridTemplateRows are plain List<TrackSizingFunction>.

using System.Collections.Generic;

namespace PawnEditor.TaffySharp
{
    internal static class ExplicitGrid
    {
        /// <summary>
        /// Returns the number of explicit tracks defined in the given axis.
        /// Since we don't support repeat(), this is simply the length of the template list.
        /// </summary>
        public static ushort ComputeExplicitGridSizeInAxis(Style style, AbsoluteAxis axis)
        {
            var template = axis == AbsoluteAxis.Horizontal
                ? style.gridTemplateColumns
                : style.gridTemplateRows;
            return (ushort)(template?.Count ?? 0);
        }

        /// <summary>
        /// Populates <paramref name="tracks"/> with all grid tracks and gutters for the given axis,
        /// based on the final track counts and style.
        /// </summary>
        public static void InitializeGridTracks(
            List<GridTrack> tracks,
            TrackCounts counts,
            Style style,
            AbsoluteAxis axis,
            System.Func<int, bool> trackHasItems)
        {
            var template = axis == AbsoluteAxis.Horizontal
                ? style.gridTemplateColumns
                : style.gridTemplateRows;
            var autoTracks = axis == AbsoluteAxis.Horizontal
                ? style.gridAutoColumns
                : style.gridAutoRows;
            var gap = axis == AbsoluteAxis.Horizontal ? style.gap.Width : style.gap.Height;

            int autoTrackCount = autoTracks?.Count ?? 0;

            tracks.Clear();
            // Reserve capacity: each track has an entry + gutter, plus a leading gutter
            int capacity = (counts.Len() * 2) + 1;
            if (tracks.Capacity < capacity) tracks.Capacity = capacity;

            // Leading gutter (will be collapsed at the end)
            tracks.Add(GridTrack.NewGutter(gap));

            // ── Negative implicit tracks ──────────────────────────────────────────
            if (counts.NegativeImplicit > 0)
            {
                // Negative implicit tracks are filled from the auto-tracks list, cycling from an offset
                // so that they align correctly with the positive implicit tracks.
                int offset = autoTrackCount == 0
                    ? 0
                    : autoTrackCount - (counts.NegativeImplicit % autoTrackCount);
                CreateImplicitTracks(tracks, counts.NegativeImplicit, autoTracks, offset, gap);
            }

            int currentTrackIndex = counts.NegativeImplicit;

            // ── Explicit tracks ───────────────────────────────────────────────────
            if (counts.Explicit > 0 && template != null && template.Count > 0)
            {
                int explicitEnd = counts.NegativeImplicit + counts.Explicit;
                for (int i = 0; i < template.Count && currentTrackIndex < explicitEnd; i++)
                {
                    var def = template[i];
                    tracks.Add(GridTrack.NewTrack(def.Min, def.Max));
                    tracks.Add(GridTrack.NewGutter(gap));
                    currentTrackIndex++;
                }
            }

            // ── Positive implicit tracks ──────────────────────────────────────────
            // Any remaining explicit grid area tracks + the explicitly requested positive implicit tracks
            int gridAreaTracks = (counts.NegativeImplicit + counts.Explicit) - currentTrackIndex;
            int posImplicit = counts.PositiveImplicit + gridAreaTracks;
            CreateImplicitTracks(tracks, (ushort)posImplicit, autoTracks, 0, gap);

            // Collapse first and last gutters (they are virtual boundary lines, not real gaps)
            tracks[0].Collapse();
            tracks[tracks.Count - 1].Collapse();
        }

        private static void CreateImplicitTracks(
            List<GridTrack> tracks,
            ushort count,
            List<TrackSizingFunction>? autoTracks,
            int startOffset,
            LengthPercentage gap)
        {
            int autoTrackCount = autoTracks?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                TrackSizingFunction def;
                if (autoTrackCount == 0 || autoTracks == null)
                    def = TrackSizingFunction.Auto();
                else
                    def = autoTracks[(startOffset + i) % autoTrackCount];

                tracks.Add(GridTrack.NewTrack(def.Min, def.Max));
                tracks.Add(GridTrack.NewGutter(gap));
            }
        }
    }
}
