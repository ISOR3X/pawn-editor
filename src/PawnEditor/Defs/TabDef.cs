using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class TabDef : Def
{
    private readonly Type workerClass = typeof(TabWorker);

    public TaffyLayoutNode layout = new();
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