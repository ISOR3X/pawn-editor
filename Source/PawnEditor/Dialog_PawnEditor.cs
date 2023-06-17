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

    public override Vector2 InitialSize => Page.StandardSize;
}

public class Dialog_PawnEditor_Pregame : Dialog_PawnEditor
{
    private readonly Action doNext;

    public Dialog_PawnEditor_Pregame(Action doNext) => this.doNext = doNext;

    public override void DoWindowContents(Rect inRect)
    {
        PawnEditor.DoUI(inRect, () => Close(), doNext, true);
    }
}

public class Dialog_PawnEditor_InGame : Dialog_PawnEditor
{
    public override void PreOpen()
    {
        base.PreOpen();
        PawnEditor.RecachePawnList();
    }

    public override void PostClose()
    {
        base.PostClose();
        PawnLister.ClearCache();
    }

    public override void DoWindowContents(Rect inRect)
    {
        PawnEditor.DoUI(inRect, () => Close(), null, false);
    }
}
