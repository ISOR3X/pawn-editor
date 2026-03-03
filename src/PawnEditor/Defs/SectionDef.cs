using System;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class SectionDef : Def
{
    private Type workerClass = typeof(SectionWorker);
    public bool sticky = false;
    public bool hideHeader = false;
    public PawnUtility.PawnCategory sectionCategory = PawnUtility.PawnCategory.All;
    public int priority = 0;
    [Unsaved] private SectionWorker workerInt;

    public SectionWorker Worker
    {
        get
        {
            if (this.workerInt == null)
            {
                this.workerInt = (SectionWorker)Activator.CreateInstance(this.workerClass, this);
                this.workerInt.def = this;
            }

            return this.workerInt;
        }
    }
}