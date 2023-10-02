using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class Dialog_PawnEditor : Window
{
    protected Dialog_PawnEditor()
    {
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnCancel = true;
        forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
    }

    protected abstract bool Pregame { get; }

    public override Vector2 InitialSize => Page.StandardSize;

    public override void PreOpen()
    {
        base.PreOpen();
        PawnEditor.Pregame = Pregame;
        PawnEditor.RecachePawnList();
        PawnEditor.CheckChangeTabGroup();
        TabWorker<Pawn>.Notify_OpenedDialog();
        TabWorker<Faction>.Notify_OpenedDialog();
    }
}

public class Dialog_PawnEditor_Pregame : Dialog_PawnEditor
{
    private readonly Action doNext;

    public Dialog_PawnEditor_Pregame(Action doNext) => this.doNext = doNext;

    protected override bool Pregame => true;

    public override void DoWindowContents(Rect inRect)
    {
        PawnEditor.DoUI(inRect, () => Close(), doNext);
    }
}

public class Dialog_PawnEditor_InGame : Dialog_PawnEditor
{
    protected override bool Pregame => false;

    public override void PreOpen()
    {
        base.PreOpen();
        ColonyInventory.RecacheItems();
    }

    public override void PostClose()
    {
        base.PostClose();
        PawnEditor.PawnList.ClearCache();
    }

    public override void DoWindowContents(Rect inRect)
    {
        PawnEditor.DoUI(inRect, () => Close(), null);
    }
}
