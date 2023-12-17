using System;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class Dialog_Confirm : Dialog_MessageBox
{
    private readonly string confirmationKey;
    private bool dontShowAgain;

    public Dialog_Confirm(TaggedString text, string key, Action confirmedAct, bool destructive = false, string title = null,
        WindowLayer layer = WindowLayer.Dialog) :
        base(text, "Confirm".Translate(), confirmedAct, "GoBack".Translate(), null, title, destructive, confirmedAct, delegate { }, layer) =>
        confirmationKey = key;

    public override void PreOpen()
    {
        base.PreOpen();
        buttonAAction += CheckAddHide;
        acceptAction += CheckAddHide;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        if (PawnEditorMod.Settings.DontShowAgain.Contains(confirmationKey))
        {
            acceptAction();
            Close(false);
        }
    }

    private void CheckAddHide()
    {
        if (dontShowAgain) PawnEditorMod.Settings.DontShowAgain.Add(confirmationKey);
    }

    public override void DoWindowContents(Rect inRect)
    {
        var text = "PawnEditor.DontShowAgain".Translate();
        var width = Text.CalcSize(text).x + 24 + 10;
        Widgets.CheckboxLabeled(new(inRect.width - width, inRect.height - 80, width, 30), text, ref dontShowAgain, placeCheckboxNearText: true);
        base.DoWindowContents(inRect);
    }
}
