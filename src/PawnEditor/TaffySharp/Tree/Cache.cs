// Port of taffy/src/tree/cache.rs

namespace PawnEditor.TaffySharp
{
    /// <summary>Outcome of a <see cref="Cache.Clear"/> call.</summary>
    public enum ClearState { Cleared, AlreadyEmpty }

    /// <summary>
    /// Per-node layout result cache.
    /// Stores up to 9 size-measurement results and 1 full-layout result.
    /// </summary>
    public class Cache
    {
        private const int CacheSize = 9;

        private struct CacheEntry<T>
        {
            public Size<float?> KnownDimensions;
            public Size<AvailableSpace> AvailableSpace;
            public T Content;
        }

        private CacheEntry<LayoutOutput>?    _finalLayoutEntry;
        private CacheEntry<Size<float>>?[]  _measureEntries = new CacheEntry<Size<float>>?[CacheSize];
        private bool _isEmpty = true;

        public Cache() { }

        // ── Cache slot selection (mirrors Rust exactly) ───────────────────────

        private static int ComputeCacheSlot(
            Size<float?> knownDimensions, Size<AvailableSpace> availableSpace)
        {
            bool hasW = knownDimensions.Width.HasValue;
            bool hasH = knownDimensions.Height.HasValue;

            if (hasW && hasH) return 0;

            if (hasW) return 1 + (availableSpace.Height == AvailableSpace.MinContent ? 1 : 0);
            if (hasH) return 3 + (availableSpace.Width  == AvailableSpace.MinContent ? 1 : 0);

            bool wIsMin = availableSpace.Width  == AvailableSpace.MinContent;
            bool hIsMin = availableSpace.Height == AvailableSpace.MinContent;
            return 5 + (wIsMin ? 2 : 0) + (hIsMin ? 1 : 0);
        }

        // ── Get ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a cached <see cref="LayoutOutput"/> if one exists for the given inputs,
        /// or <c>null</c> otherwise.
        /// </summary>
        public LayoutOutput? Get(
            Size<float?> knownDimensions,
            Size<AvailableSpace> availableSpace,
            RunMode runMode)
        {
            switch (runMode)
            {
                case RunMode.PerformLayout:
                {
                    if (_finalLayoutEntry is null) return null;
                    var e = _finalLayoutEntry.Value;
                    var cachedSize = e.Content.Size;
                    bool match =
                        (knownDimensions.Width  == e.KnownDimensions.Width  || knownDimensions.Width  == cachedSize.Width) &&
                        (knownDimensions.Height == e.KnownDimensions.Height || knownDimensions.Height == cachedSize.Height) &&
                        (knownDimensions.Width.HasValue  || e.AvailableSpace.Width .IsRoughlyEqual(availableSpace.Width)) &&
                        (knownDimensions.Height.HasValue || e.AvailableSpace.Height.IsRoughlyEqual(availableSpace.Height));
                    return match ? e.Content : null;
                }

                case RunMode.ComputeSize:
                {
                    for (int i = 0; i < CacheSize; i++)
                    {
                        if (_measureEntries[i] is null) continue;
                        var e = _measureEntries[i]!.Value;
                        var cachedSize = e.Content;
                        bool match =
                            (knownDimensions.Width  == e.KnownDimensions.Width  || knownDimensions.Width  == cachedSize.Width) &&
                            (knownDimensions.Height == e.KnownDimensions.Height || knownDimensions.Height == cachedSize.Height) &&
                            (knownDimensions.Width.HasValue  || e.AvailableSpace.Width .IsRoughlyEqual(availableSpace.Width)) &&
                            (knownDimensions.Height.HasValue || e.AvailableSpace.Height.IsRoughlyEqual(availableSpace.Height));
                        if (match) return LayoutOutput.FromOuterSize(cachedSize);
                    }
                    return null;
                }

                default: // PerformHiddenLayout
                    return null;
            }
        }

        // ── Store ─────────────────────────────────────────────────────────────

        /// <summary>Stores a computed layout result in the appropriate cache slot.</summary>
        public void Store(
            Size<float?> knownDimensions,
            Size<AvailableSpace> availableSpace,
            RunMode runMode,
            LayoutOutput layoutOutput)
        {
            switch (runMode)
            {
                case RunMode.PerformLayout:
                    _isEmpty = false;
                    _finalLayoutEntry = new CacheEntry<LayoutOutput>
                    {
                        KnownDimensions = knownDimensions,
                        AvailableSpace = availableSpace,
                        Content = layoutOutput,
                    };
                    break;

                case RunMode.ComputeSize:
                    _isEmpty = false;
                    int slot = ComputeCacheSlot(knownDimensions, availableSpace);
                    _measureEntries[slot] = new CacheEntry<Size<float>>
                    {
                        KnownDimensions = knownDimensions,
                        AvailableSpace = availableSpace,
                        Content = layoutOutput.Size,
                    };
                    break;

                // PerformHiddenLayout: nothing to cache
            }
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        /// <summary>Clears all cached entries and returns whether anything was cleared.</summary>
        public ClearState Clear()
        {
            if (_isEmpty) return ClearState.AlreadyEmpty;
            _isEmpty = true;
            _finalLayoutEntry = null;
            for (int i = 0; i < CacheSize; i++) _measureEntries[i] = null;
            return ClearState.Cleared;
        }

        /// <summary>Returns true if all cache entries are empty.</summary>
        public bool IsEmpty => _isEmpty;
    }
}
