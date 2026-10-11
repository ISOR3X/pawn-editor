using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace PawnEditor.v4;

public enum EntityKind
{
    Pawn,
    Thing,
    Faction
}

// ---------------------------------------------------------------------------
// Entities
// ---------------------------------------------------------------------------

/// <summary>
///     Closed set of editable things. Pawn is a Thing in RimWorld, so <see cref="From(Thing)" /> decides once
///     which wrapper to use and nothing else has to care about the overlap.
/// </summary>
public abstract class EditorEntity : IEquatable<EditorEntity>
{
    public abstract EntityKind Kind { get; }
    public abstract bool IsValid { get; }
    public abstract string Label { get; }

    public static EditorEntity From(Thing thing) =>
        thing is Pawn pawn ? new PawnEntity(pawn) : new ThingEntity(thing);

    public static EditorEntity From(Faction faction) => new FactionEntity(faction);

    public abstract bool Equals(EditorEntity? other);
    public override bool Equals(object? obj) => obj is EditorEntity other && Equals(other);
    public abstract override int GetHashCode();
}

/// <summary>Weak reference to the game object, with equality by target identity.</summary>
public abstract class TargetEntity<T> : EditorEntity where T : class
{
    private readonly int hash; // cached so it stays stable if the target is collected
    private readonly System.WeakReference<T> reference;

    protected TargetEntity(T target)
    {
        reference = new System.WeakReference<T>(target);
        hash = RuntimeHelpers.GetHashCode(target);
    }

    public T? Target => reference.TryGetTarget(out var target) ? target : null;

    public override bool IsValid => Target is { } target && IsAlive(target);
    public override string Label => Target is { } target ? GetLabel(target) : "(gone)";

    protected abstract bool IsAlive(T target);
    protected abstract string GetLabel(T target);

    // PawnEntity and ThingEntity are different closed types, so a pawn never equals a thing.
    // The reference check keeps an entity equal to itself after its target has been collected.
    public override bool Equals(EditorEntity? other) =>
        ReferenceEquals(this, other) ||
        (other is TargetEntity<T> o && hash == o.hash && Target is { } mine && ReferenceEquals(mine, o.Target));

    public override int GetHashCode() => hash;
}

public sealed class PawnEntity(Pawn pawn) : TargetEntity<Pawn>(pawn)
{
    public override EntityKind Kind => EntityKind.Pawn;

    // Dead pawns are Destroyed but kept alive in world pawns, and stay editable.
    // Only Discarded (removed from the game entirely) counts as gone.
    protected override bool IsAlive(Pawn pawn) => !pawn.Discarded;
    protected override string GetLabel(Pawn pawn) => pawn.LabelShortCap;
}

public sealed class ThingEntity : TargetEntity<Thing>
{
    public ThingEntity(Thing thing) : base(thing)
    {
        if (thing is Pawn) throw new ArgumentException("Pawns use PawnEntity.", nameof(thing));
    }

    public override EntityKind Kind => EntityKind.Thing;

    // Destroyed already includes Discarded.
    protected override bool IsAlive(Thing thing) => !thing.Destroyed;
    protected override string GetLabel(Thing thing) => thing.LabelCap;
}

public sealed class FactionEntity(Faction faction) : TargetEntity<Faction>(faction)
{
    public override EntityKind Kind => EntityKind.Faction;

    // Find.FactionManager throws before the world exists (scenario selection).
    protected override bool IsAlive(Faction faction) =>
        Current.Game?.World?.factionManager is { } manager && manager.AllFactionsListForReading.Contains(faction);

    protected override string GetLabel(Faction faction) => faction.Name;
}

// ---------------------------------------------------------------------------
// Stack entries
// ---------------------------------------------------------------------------

public enum BrowserTab
{
    Factions,
    Pawns,
    Things,
    Relations
}

/// <summary>
///     The root view. Not part of the entry list: it always exists, can't be popped, and keeps its own state.
///     The stack shows it whenever there are no entity entries.
/// </summary>
public sealed class BrowserState
{
    private string search = "";

    public BrowserTab Tab = BrowserTab.Pawns;

    /// <summary>Pixel offset is fine here: rows have a fixed height, so it survives a rebuild.</summary>
    public float Scroll;

    /// <summary>Group keys the user collapsed.</summary>
    public HashSet<string> CollapsedGroups { get; } = [];

    public string Search
    {
        get => search;
        set
        {
            if (search == value) return;
            search = value;
            Scroll = 0f; // the old offset means nothing for a different result list
        }
    }
}

/// <summary>
///     One level of drill-in. View state is stored by def name rather than by def reference, so this file
///     doesn't depend on the def types and a def reload can't leave dangling references.
/// </summary>
public sealed class EntityEntry
{
    internal EntityEntry(EditorEntity entity, Func<IEnumerable<EditorEntity>>? source)
    {
        Entity = entity;
        Source = source;
    }

    public EditorEntity Entity { get; private set; }

    /// <summary>
    ///     Where this entity was opened from. A delegate rather than a list so deletions show up without
    ///     bookkeeping. Drives prev/next and the breadcrumb dropdown.
    /// </summary>
    public Func<IEnumerable<EditorEntity>>? Source { get; }

    /// <summary>Selected category. Null means "first visible one", resolved by the view.</summary>
    public string? Category { get; private set; }

    /// <summary>
    ///     Section at the top of the viewport when the entry was left, plus how far into it. Saved as a
    ///     section, not pixels, because layout nodes are rebuilt when the entry becomes the top again.
    ///     The view re-resolves it until section positions stop changing.
    /// </summary>
    public string? Section { get; private set; }

    public float OffsetInSection { get; private set; }

    public void SelectCategory(string? category)
    {
        Category = category;
        Section = null;
        OffsetInSection = 0f;
    }

    /// <summary>Called by scroll-spy and by sidebar section clicks.</summary>
    public void SetAnchor(string category, string section, float offset)
    {
        Category = category;
        Section = section;
        OffsetInSection = offset;
    }

    // Keeps category and section so switching pawns stays where the user was.
    // The view falls back to the first visible ones if they don't exist for the new entity.
    internal void Replace(EditorEntity entity)
    {
        if (entity.Kind != Entity.Kind) SelectCategory(null);
        Entity = entity;
    }
}

// ---------------------------------------------------------------------------
// The stack
// ---------------------------------------------------------------------------

/// <summary>
///     Navigation history: the browser, then zero or more entity entries.
///     <para>
///         Mutations are queued and applied by <see cref="Apply" />, which the window calls once at the
///         start of a frame. UI code can therefore call Push/Pop/ReplaceTop from inside a draw or a click
///         handler without the stack changing under the code that is still drawing it.
///     </para>
/// </summary>
public sealed class EditorStack
{
    private readonly List<EntityEntry> entries = [];
    private readonly List<Action> pending = [];

    public BrowserState Browser { get; } = new();

    /// <summary>Entity entries, bottom to top. The browser is the implicit root in front of them.</summary>
    public IReadOnlyList<EntityEntry> Entries => entries;

    /// <summary>The topmost entity entry, or null when the browser is the visible view.</summary>
    public EntityEntry? Top => entries.Count > 0 ? entries[entries.Count - 1] : null;

    public bool ShowsBrowser => entries.Count == 0;

    /// <summary>Bumped whenever the stack changed. Views compare it to know when to rebuild.</summary>
    public int Version { get; private set; }

    public EntityEntry? EntryFor(EditorEntity entity)
    {
        foreach (var entry in entries)
            if (entry.Entity.Equals(entity))
                return entry;
        return null;
    }

    // ---- Queued operations ------------------------------------------------

    /// <summary>Open a linked entity. If it is already in the stack, pop back to it instead.</summary>
    public void Push(EditorEntity entity, Func<IEnumerable<EditorEntity>>? source = null) =>
        pending.Add(() => PushNow(entity, source));

    /// <summary>Back. Pops the top entity entry; from the first entity this shows the browser.</summary>
    public void Pop() => pending.Add(() => TruncateTo(entries.Count - 1));

    /// <summary>Breadcrumb click. Keeps <paramref name="entry" /> as top; null goes back to the browser.</summary>
    public void PopTo(EntityEntry? entry) => pending.Add(() =>
    {
        var index = entry == null ? -1 : entries.IndexOf(entry);
        if (entry == null || index >= 0) TruncateTo(index + 1);
    });

    /// <summary>Prev/next: swap the top entity without adding a crumb.</summary>
    public void ReplaceTop(EditorEntity entity) => pending.Add(() => ReplaceTopNow(entity));

    // ---- Queries ----------------------------------------------------------

    /// <summary>
    ///     Next (+1) or previous (-1) sibling of the top entity, wrapping. Skips siblings that are already
    ///     further down the stack so stepping never collapses the breadcrumb. Enumerates the source, so call it
    ///     from a click handler rather than every frame; use <see cref="CanStep" /> to enable the buttons.
    /// </summary>
    public EditorEntity? Neighbor(int delta)
    {
        if (Top is not { Source: { } source } top) return null;

        var siblings = source().Where(e => e.IsValid).ToList();
        var index = siblings.IndexOf(top.Entity);
        if (index < 0) return null;

        var count = siblings.Count;
        for (var step = 1; step < count; step++)
        {
            var at = (index + delta * step) % count;
            var candidate = siblings[at < 0 ? at + count : at];
            if (EntryFor(candidate) == null) return candidate;
        }

        return null;
    }

    /// <summary>Cheap check for enabling the prev/next buttons.</summary>
    public bool CanStep => Top?.Source != null;

    // ---- Per-frame --------------------------------------------------------

    /// <summary>
    ///     Runs queued operations, then drops entries whose entity is gone. Call once at the start of the
    ///     frame. Returns true if the stack changed.
    /// </summary>
    public bool Apply()
    {
        var before = Version;

        if (pending.Count > 0)
        {
            // Operations may queue more operations through the helpers; take a snapshot to be safe.
            var batch = pending.ToArray();
            pending.Clear();
            foreach (var operation in batch) operation();
        }

        Validate();
        return Version != before;
    }

    /// <summary>
    ///     Truncates at the first entry whose entity is gone, which also covers deletions done from inside the
    ///     editor. Cheap enough to run every frame.
    /// </summary>
    private void Validate()
    {
        for (var i = 0; i < entries.Count; i++)
            if (!entries[i].Entity.IsValid)
            {
                TruncateTo(i);
                return;
            }
    }

    /// <summary>Drops all entity entries but keeps the browser state.</summary>
    internal void Clear()
    {
        pending.Clear();
        TruncateTo(0);
    }

    // ---- Immediate implementations ---------------------------------------

    private void PushNow(EditorEntity entity, Func<IEnumerable<EditorEntity>>? source)
    {
        if (!entity.IsValid) return;

        var existing = EntryFor(entity);
        if (existing != null)
        {
            TruncateTo(entries.IndexOf(existing) + 1);
            return;
        }

        entries.Add(new EntityEntry(entity, source));
        Version++;
    }

    private void ReplaceTopNow(EditorEntity entity)
    {
        if (Top is not { } top || !entity.IsValid || top.Entity.Equals(entity)) return;

        // Same rule as Push: never hold the same entity twice.
        var existing = EntryFor(entity);
        if (existing != null)
        {
            TruncateTo(entries.IndexOf(existing) + 1);
            return;
        }

        top.Replace(entity);
        Version++;
    }

    /// <summary>Keeps the first <paramref name="count" /> entries.</summary>
    private void TruncateTo(int count)
    {
        count = Math.Max(count, 0);
        if (count >= entries.Count) return;
        entries.RemoveRange(count, entries.Count - count);
        Version++;
    }
}

// ---------------------------------------------------------------------------
// Session lifetime
// ---------------------------------------------------------------------------

/// <summary>
///     One stack per game session, kept in memory only. A static is acceptable here because it carries a
///     stale guard: it is dropped as soon as Current.Game is a different Game. The game is held weakly so
///     the stack never keeps a finished game alive.
/// </summary>
public static class EditorSession
{
    private static System.WeakReference<Game>? owner;
    private static EditorStack? stack;

    public static EditorStack Stack
    {
        get
        {
            if (stack == null || !IsOwnedByCurrentGame()) Reset();
            return stack!;
        }
    }

    /// <summary>
    ///     With a target: start at that target, on top of the browser (whose tab/search/scroll are kept).
    ///     Without one: restore wherever the user left off.
    /// </summary>
    public static EditorStack Open(EditorEntity? target = null, Func<IEnumerable<EditorEntity>>? source = null)
    {
        var current = Stack;
        if (target != null)
        {
            current.Clear();
            current.Push(target, source);
        }

        current.Apply();
        return current;
    }

    private static bool IsOwnedByCurrentGame()
    {
        Game? ownerGame = null;
        owner?.TryGetTarget(out ownerGame);
        return ReferenceEquals(Current.Game, ownerGame);
    }

    private static void Reset()
    {
        stack = new EditorStack();
        var game = Current.Game;
        owner = game != null ? new System.WeakReference<Game>(game) : null;
    }
}
