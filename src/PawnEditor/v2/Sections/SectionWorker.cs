using Verse;
using Void.Taffy;

namespace PawnEditor.v2;

public abstract class SectionWorker
{
    public virtual bool ShowFor(object obj) => true;

    public abstract void DoSectionContents(UIBranch b);
}

public abstract class SectionWorker<T> : SectionWorker
{
    public override bool ShowFor(object obj) => obj is T;

    public override sealed void DoSectionContents(UIBranch b)
    {
        if (b.TryInject<T>("ctx", out var thing))
        {
            DoSectionContents(b, thing);
        }
    }

    public abstract void DoSectionContents(UIBranch b, T thing);
}
