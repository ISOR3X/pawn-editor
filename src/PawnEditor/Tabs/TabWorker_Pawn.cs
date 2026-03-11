using System.Linq;
using HotSwap;
using PawnEditor.Layout;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public abstract class TabWorker_Pawn(TabDef def) : TabWorker(def)
{
    private float _viewRectHeight = 5000;

    private static Pawn? SelectedPawn => Find.WindowStack.WindowOfType<Window_Editor>()?.GetSelectedPawn();


    protected override void DoInnerTabContents(ref Rect inRect)
    {
        DoInnerTabContents(ref inRect, SelectedPawn);
    }

    protected virtual void DoInnerTabContents(ref Rect inRect, Pawn? pawn)
    {
        inRect.ContractedBy(0, 8f);

        if (pawn == null) return;

        Verse.Widgets.BeginGroup(inRect);
        var contentRect = inRect.AtZero();
        var additionalWidth = inRect.height < _viewRectHeight ? UIUtility.ScrollBarWidth_WithMargin : 0;
        var viewRect = new Rect(contentRect.x, contentRect.y, contentRect.width - additionalWidth, _viewRectHeight);
        Verse.Widgets.BeginScrollView(contentRect, ref TabScrollPosition, viewRect);

        _viewRectHeight = FlexLayoutEngine.Draw(
            Def.layout,
            viewRect,
            (section, r) => section.Worker.DoSection(pawn, r),
            section => section.Worker.ShowSection(pawn));

        Verse.Widgets.EndScrollView();
        Verse.Widgets.EndGroup();
    }

    #region EVENTS

    public override void Notify_ContentChanged()
    {
        base.Notify_ContentChanged();

        if (SelectedPawn == null) return;

        QuickActionUtility.actions.TryGetValue(Def.defName, out var actions);
        if (actions.NullOrEmpty()) return;

        QuickActions.Clear();
        foreach (var (attribute, method) in actions!.Where(a => a.Item1.CanUseQuickAction()))
            QuickActions.Add(attribute.ToFloatMenuOption(method));
    }

    #endregion
}