using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class TabDef : Def
{
    private Type workerClass = typeof(TabWorker);
    public PawnUtility.PawnCategory tabCategory = PawnUtility.PawnCategory.Humanlike;
    public int priority = 10;
    public List<SectionDef> sections;
    [Unsaved] private TabWorker _workerInt;

    public TabWorker Worker
    {
        get
        {
            if (this._workerInt == null)
            {
                this._workerInt = (TabWorker)Activator.CreateInstance(this.workerClass, this);
                this._workerInt.def = this;
            }

            return this._workerInt;
        }
    }
}