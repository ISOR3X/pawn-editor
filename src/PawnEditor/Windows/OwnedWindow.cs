using Verse;

namespace PawnEditor;

public abstract class OwnedWindow(Window? owner) : Window
{
    /// <summary>
    ///     window.absorbInputAroundWindow only works for click outside, while OwnedWindow also closes children when the parent
    ///     closes.
    /// </summary>
    public override void ExtraOnGUI()
    {
        base.ExtraOnGUI();
        if (owner != null && !Find.WindowStack.IsOpen(owner))
            Close(false);
    }
}