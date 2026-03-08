using System;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class SectionDef : Def
{
    private readonly Type workerClass = typeof(SectionWorker);
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
}