using Verse;

namespace Editor;

/// <summary>A category of the sidebar together with the sections that are visible for the current target.</summary>
public sealed class VisibleCategory(CategoryDef def, List<SectionDef> sections)
{
    public CategoryDef Def { get; } = def;
    public List<SectionDef> Sections { get; } = sections;
}

/// <summary>
///     Looks up which categories and sections apply to a target. Built lazily from the defs and rebuilt if the
///     number of defs changes (def reload).
/// </summary>
public static class EditorRegistry
{
    private static readonly Dictionary<Type, List<CategoryDef>> CategoriesByEntity = [];
    private static readonly Dictionary<CategoryDef, List<SectionDef>> SectionsByCategory = [];
    private static readonly Dictionary<Type, Type?> RegisteredTypeCache = [];
    private static int builtFor = -1;

    private static void EnsureBuilt()
    {
        var count = DefDatabase<CategoryDef>.DefCount + DefDatabase<SectionDef>.DefCount;
        if (count == builtFor) return;
        builtFor = count;

        CategoriesByEntity.Clear();
        SectionsByCategory.Clear();
        RegisteredTypeCache.Clear();

        foreach (var category in DefDatabase<CategoryDef>.AllDefsListForReading)
        {
            if (category.entity == null) continue;
            if (!CategoriesByEntity.TryGetValue(category.entity, out var list))
                CategoriesByEntity[category.entity] = list = [];
            list.Add(category);
            SectionsByCategory[category] = [];
        }

        foreach (var list in CategoriesByEntity.Values)
            list.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));

        foreach (var section in DefDatabase<SectionDef>.AllDefsListForReading)
            if (section.category != null && SectionsByCategory.TryGetValue(section.category, out var list))
                list.Add(section);

        foreach (var list in SectionsByCategory.Values)
            list.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
    }

    /// <summary>
    ///     The nearest type in the target's class chain that has categories defined, or null. So Pawn uses Pawn's
    ///     categories only, a Building falls back to Thing, and a modded subclass inherits its parent's sidebar.
    /// </summary>
    public static Type? RegisteredTypeFor(Type targetType)
    {
        EnsureBuilt();
        if (RegisteredTypeCache.TryGetValue(targetType, out var cached)) return cached;

        Type? found = null;
        for (var type = targetType; type != null; type = type.BaseType)
            if (CategoriesByEntity.ContainsKey(type))
            {
                found = type;
                break;
            }

        return RegisteredTypeCache[targetType] = found;
    }

    /// <summary>
    ///     The sidebar for a target: its type's categories in order, each with its visible sections. Categories
    ///     without a visible section are left out.
    /// </summary>
    public static List<VisibleCategory> VisibleFor(object target)
    {
        var result = new List<VisibleCategory>();
        if (RegisteredTypeFor(target.GetType()) is not { } type) return result;

        foreach (var category in CategoriesByEntity[type])
        {
            List<SectionDef>? visible = null;
            foreach (var section in SectionsByCategory[category])
                if (IsShown(section, target))
                    (visible ??= []).Add(section);

            if (visible != null) result.Add(new VisibleCategory(category, visible));
        }

        return result;
    }

    // A broken worker must not take the whole sidebar down every frame.
    private static bool IsShown(SectionDef section, object target)
    {
        try
        {
            return section.Worker.ShouldShow(target);
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"Editor section {section.defName} threw in ShouldShow: {e}", section.defName.GetHashCode());
            return false;
        }
    }
}
