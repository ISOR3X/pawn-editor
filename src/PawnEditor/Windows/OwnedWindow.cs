using Verse;

namespace PawnEditor;

public abstract class OwnedWindow(Window? owner) : Window
{
    public override void ExtraOnGUI()
    {
        base.ExtraOnGUI();
        if (owner != null && !Find.WindowStack.IsOpen(owner))
            Close(false);
    }
}
