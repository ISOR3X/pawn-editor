using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class TabDef : Def
{
    private readonly Type workerClass = typeof(TabWorker);
    [Unsaved] private TabWorker? _workerInt;
    public int priority = 10;
    public required List<SectionDef> sections;
    public PawnUtility.PawnCategory tabCategory = PawnUtility.PawnCategory.Humanlike;

    public TabWorker Worker
    {
        get
        {
            if (_workerInt != null) return _workerInt;
            _workerInt = (TabWorker)Activator.CreateInstance(workerClass, this);
            _workerInt.Def = this;

            return _workerInt;
        }
    }
}