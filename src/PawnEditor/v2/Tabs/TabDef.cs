using Verse;
using Void.XML;

namespace PawnEditor.v2;

class TabDef : Def
{
    private readonly Type workerClass = typeof(TabWorker);

    public required ParsedLayout layout = null!;
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
        layout?.ParseAndResolveReferences();
    }
}
