using JetBrains.Annotations;
using Verse;

namespace Editor;

/// <summary>
///     One entry in the sidebar for a kind of target: Appearance, Gear, Health... Belongs to exactly one target
///     type and groups sections. A target uses the categories of the nearest type in its class chain that has any
///     (see <see cref="EditorRegistry" />), so a Pawn never picks up Thing's categories.
/// </summary>
[UsedImplicitly]
public class CategoryDef : Def
{
    /// <summary>The target type this category is for, for example <c>Verse.Pawn</c>.</summary>
    public Type entity = null!;

    /// <summary>Sort order in the sidebar. Leave gaps (10, 20, 30) so other mods can slot in.</summary>
    public int order = 100;

    public string Title => label.NullOrEmpty() ? defName : LabelCap.Resolve();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (entity == null) yield return "entity is not set";
    }
}

/// <summary>
///     A block of editing UI inside a category. Sections of the selected category are stacked in the content pane.
///     The target type comes from the category, so a mod adds content by pointing at an existing category.
/// </summary>
[UsedImplicitly]
public class SectionDef : Def
{
    public CategoryDef category = null!;
    public int order = 100;
    public Type workerClass = null!;

    [field: Unsaved]
    public SectionWorker Worker
    {
        get
        {
            if (field != null) return field;
            field = (SectionWorker)Activator.CreateInstance(workerClass);
            return field;
        }
    }

    public string Title => label.NullOrEmpty() ? defName : LabelCap.Resolve();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (category == null) yield return "category is not set";
        if (workerClass == null) yield return "workerClass is not set";
        else if (!typeof(SectionWorker).IsAssignableFrom(workerClass))
            yield return $"workerClass {workerClass} does not derive from {nameof(SectionWorker)}";
    }
}
