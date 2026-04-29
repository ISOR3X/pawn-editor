using JetBrains.Annotations;
using Verse;
using Void;

namespace PawnEditor;

[UsedImplicitly]
public class TabDef : Def
{
    private readonly Type workerClass = typeof(TabWorker);

    public ParsedLayout? layout;
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

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        layout?.ResolveClasses();
    }
}