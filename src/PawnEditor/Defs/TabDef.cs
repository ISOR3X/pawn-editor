using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PawnEditor.Layout;
using Verse;

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

namespace PawnEditor;

[UsedImplicitly]
public class TabDef : Def
{
    private readonly Type workerClass = typeof(TabWorker);
    public int priority = 10;
    public required List<SectionRef> sections = [];
    public List<SectionRef> stickySections = [];
    public PawnUtility.PawnCategory tabCategory = PawnUtility.PawnCategory.Humanlike;

    public SectionLayoutNode layout = new();

    [field: Unsaved]
    public TabWorker Worker
    {
        get
        {
            if (field != null) return field;
            field = (TabWorker)Activator.CreateInstance(workerClass, this);
            field.Def = this;

            return field;
        }
    }
}

public class SectionRef : IFlexItem
{
    public required SectionDef section;
    public float width = 1f;
    public float grow = 1f;
    public int priority = 0;

    public float Width => width;
    public int Priority => priority;
    public float Grow => grow;
}