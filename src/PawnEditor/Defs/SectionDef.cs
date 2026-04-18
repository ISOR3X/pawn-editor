using JetBrains.Annotations;
using Verse;
using Void;

namespace PawnEditor;

[UsedImplicitly]
public class SectionDef : Def
{
    private readonly Type workerClass = typeof(SectionWorker);

    /// <summary>
    ///     Optional XML-driven layout for this section. When set, <see cref="SectionWorker.DoSectionContents" />
    ///     in the base class will build the UI from this tree via <see cref="Layout" />.
    ///     Workers that provide their own <see cref="SectionWorker.DoSectionContents" /> override ignore this field.
    /// </summary>
    public ParsedLayout? layout;

    public PawnUtility.PawnCategory sectionCategory = PawnUtility.PawnCategory.All;

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