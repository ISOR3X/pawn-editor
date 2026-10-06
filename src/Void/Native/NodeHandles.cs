namespace Void.Native;

/// <summary>
///     Remembers, per node key, the style and content each node had last frame and their handles. Most nodes look
///     the same every frame, and recognising that is much cheaper than looking them up in
///     <see cref="StyleRegistry" /> and <see cref="MeasureRegistry" />, which hash the whole style or text.
/// </summary>
internal sealed class NodeHandles
{
    private Entry[] _entries = new Entry[64];

    public uint Style(ulong key, Style? style)
    {
        ref var entry = ref EntryFor(key);
        if (style == null) return 0;

        // The same instance, unchanged since, or an equal one (e.g. built inline every frame).
        if (ReferenceEquals(style, entry.style) ? style.version == entry.styleVersion : style.Equals(entry.style))
            return entry.styleHandle;

        entry.style = style;
        entry.styleVersion = style.version;
        return entry.styleHandle = StyleRegistry.Get(style);
    }

    public uint Measure(ulong key, LeafContext? context)
    {
        ref var entry = ref EntryFor(key);
        if (context == null) return 0;

        // Components usually create a new context every frame with the same values (often the same string).
        if (context.Equals(entry.context)) return entry.measureHandle;

        entry.context = context;
        return entry.measureHandle = MeasureRegistry.Get(context);
    }

    /// <summary>Forgets a removed node, so its key starts fresh when it is reused.</summary>
    public void Forget(ulong key)
    {
        if (key < (ulong)_entries.Length) _entries[key] = default;
    }

    private ref Entry EntryFor(ulong key)
    {
        if (key >= (ulong)_entries.Length) Array.Resize(ref _entries, Math.Max(_entries.Length * 2, (int)key + 1));
        return ref _entries[key];
    }

    private struct Entry
    {
        public Style? style;
        public int styleVersion;
        public uint styleHandle;
        public LeafContext? context;
        public uint measureHandle;
    }
}
