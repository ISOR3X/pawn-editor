using Verse;

namespace PawnEditor;

public abstract class OwnedWindow(Window? owner) : Window
{
    // TODO: Check if absorbInputAroundWindow field from window is enough?
    public override void ExtraOnGUI()
    {
        base.ExtraOnGUI();
        if (owner != null && !Find.WindowStack.IsOpen(owner))
            Close(false);
    }
}