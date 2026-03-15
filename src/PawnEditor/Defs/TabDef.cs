using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PawnEditor.Layout;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class TabDef : Def
{
    private readonly Type workerClass = typeof(TabWorker);

    public SectionFlexLayoutNode layout = new();
    public int priority = 10;
    public PawnUtility.PawnCategory tabCategory = PawnUtility.PawnCategory.Humanlike;

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

public class SectionFlexLayoutNode : FlexLayoutNode<SectionDef>
{
    public SectionFlexLayoutNode()
    {
        registry = new Dictionary<string, Func<LayoutNode<SectionDef>>>
        {
            ["section"] = () => new DefLeafNode<SectionDef>(),
            ["flex"] = () => new SectionFlexLayoutNode()
        };
    }
}