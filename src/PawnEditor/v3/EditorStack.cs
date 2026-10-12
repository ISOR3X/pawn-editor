using LudeonTK;
using UnityEngine;
using Verse;
using Void.Native;

namespace Editor;

public class EditorEntry(string label, object obj) : IEquatable<EditorEntry>
{
    public string label = label;
    private readonly System.WeakReference<object> reference = new(obj);
    public object? Target => reference.TryGetTarget(out var t) ? t : null;
    public bool Equals(EditorEntry? other) => other is not null && ReferenceEquals(Target, other.Target);

    /// <summary>Selected category, by def name. Null means the first visible one.</summary>
    public string? Category { get; private set; }

    /// <summary>Section chosen in the sidebar, by def name, for highlighting and later scroll-to.</summary>
    public string? Section { get; private set; }

    public void SelectCategory(string category)
    {
        Category = category;
        Section = null;
    }

    public void SelectSection(string category, string section)
    {
        Category = category;
        Section = section;
    }
}


public class EditorState
{
    private readonly List<EditorEntry> entryStack = [];

    public void Pop()
    {
        if (entryStack.Count > 0)
            entryStack.RemoveAt(entryStack.Count - 1);
    }

    public void Push(EditorEntry entry)
    {
        // == on a class is reference equality of the entry objects; Equals compares their targets.
        var existing = entryStack.FirstOrDefault(e => e.Equals(entry));
        if (existing != null)
            PopTo(existing);
        else
            entryStack.Add(entry);
    }

    public void PopTo(EditorEntry? entry)
    {
        var keep = entry == null ? 0 : entryStack.IndexOf(entry) + 1;
        if (entry != null && keep == 0) return;
        entryStack.RemoveRange(keep, entryStack.Count - keep);
    }


    /// <summary>
    ///     Categories the user expanded in the sidebar, by def name. Category names are unique per target type, so
    ///     this is effectively remembered per type and survives switching between entries.
    /// </summary>
    public HashSet<string> ExpandedCategories { get; } = [];

    public EditorEntry? CurrentEntry => entryStack.Count > 0 ? entryStack[^1] : null;
    public object? CurrentTarget => CurrentEntry?.Target;
    public IReadOnlyList<EditorEntry> Entries => entryStack;
}

/// <summary>
///     Holds the editor state for the lifetime of a Game. Game.FillComponents creates every component with
///     Activator.CreateInstance(type, game), so the Game constructor is required even though it is unused.
/// </summary>
#pragma warning disable CS9113
public class GameComponent_EditorStack(Game game) : GameComponent
{
    public EditorState State { get; } = new();

    /// <summary>
    ///     The game's state, or a throwaway one when there is no game (main menu) so the window still works.
    /// </summary>
    public static EditorState StateForCurrentGame() =>
        Current.Game?.GetComponent<GameComponent_EditorStack>()?.State ?? new EditorState();
}
#pragma warning restore CS9113
