using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class SectionDef : Def
{
    private readonly Type workerClass = typeof(SectionWorker);
    public PawnUtility.PawnCategory sectionCategory = PawnUtility.PawnCategory.All;

    /// <summary>
    /// Optional XML-driven layout for this section. When set, <see cref="SectionWorker.DoSectionContents"/>
    /// in the base class will build the UI from this tree via <see cref="UILayout"/>.
    /// Workers that provide their own <see cref="SectionWorker.DoSectionContents"/> override ignore this field.
    /// </summary>
    public UILayoutNode? layout;

    [field: Unsaved]
    public SectionWorker Worker
    {
        get
        {
            if (field != null) return field;
            field = (SectionWorker)Activator.CreateInstance(workerClass, this);
            field.Def = this;

            return field;
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        layout?.ResolveClasses();
    }
}