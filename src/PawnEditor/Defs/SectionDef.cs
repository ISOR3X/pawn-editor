using System;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class SectionDef : Def
{
    private readonly Type workerClass = typeof(SectionWorker);
    public bool hideHeader = false;
    public int priority = 0;
    public PawnUtility.PawnCategory sectionCategory = PawnUtility.PawnCategory.All;
    public bool sticky = false;
    [Unsaved] private SectionWorker? workerInt;

    public SectionWorker Worker
    {
        get
        {
            if (workerInt != null) return workerInt;
            workerInt = (SectionWorker)Activator.CreateInstance(workerClass, this);
            workerInt.Def = this;

            return workerInt;
        }
    }
}