using Void.Native;

namespace Editor;

/// <summary>
///     Builds the UI of one section. Instances are created once per def and shared, so a worker must not keep
///     per-target state. Subclass <see cref="SectionWorker{T}" /> instead of this for a typed target.
/// </summary>
public abstract class SectionWorker
{
    /// <summary>
    ///     Whether the section applies to this target. Runs for every section on every frame while building the
    ///     sidebar, so keep it to cheap type and null checks.
    /// </summary>
    public virtual bool ShouldShow(object target) => true;

    /// <summary>
    ///     Builds the section's content. The caller wraps each section in its own node, so child ids only need to
    ///     be unique within the section.
    /// </summary>
    public abstract void Build(UIBranch b, object target, EditorState editor);
}

/// <summary>Typed convenience layer: the target is cast once, here. Concrete subclasses are what defs reference.</summary>
public abstract class SectionWorker<T> : SectionWorker where T : class
{
    public sealed override bool ShouldShow(object target) => target is T typed && Show(typed);

    public sealed override void Build(UIBranch b, object target, EditorState editor)
    {
        if (target is T typed) BuildSection(b, typed, editor);
    }

    protected virtual bool Show(T target) => true;

    protected abstract void BuildSection(UIBranch b, T target, EditorState editor);
}
