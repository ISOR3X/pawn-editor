using System.Linq;
using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public class TabWorker_Pawn(TabDef def) : TabWorker(def)
{
    private static Pawn? SelectedPawn => Find.WindowStack.WindowOfType<Window_Editor>()?.GetSelectedPawn();

    protected override void DoInnerTabContents(TaffyBuilder col)
        => DoInnerTabContents(col, SelectedPawn);

    protected virtual void DoInnerTabContents(TaffyBuilder col, Pawn? pawn)
    {
        if (pawn == null) return;
        Def.layout.BuildInto(col, pawn, s => s.Worker.ShowSection(pawn));
    }

    #region EVENTS

    public override void Notify_ContentChanged()
    {
        base.Notify_ContentChanged();

        if (SelectedPawn == null) return;

        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        quickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            quickActions.Add(attribute.ToFloatMenuOption(method));
    }

    #endregion
}
