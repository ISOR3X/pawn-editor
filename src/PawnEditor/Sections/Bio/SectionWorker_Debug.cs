using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.XMLComponents;
using FlexDirection = Taffy.FlexDirection;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Debug(SectionDef def) : SectionWorker(def)
{
    private readonly Ref<string> _content = new("");
    private readonly Ref<int> _contentNr = new(0);

    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<InputElement>("input").Value = _content;
        layout.ComponentById<InputElement>("input2").Value = _contentNr;
        layout.ComponentById<TextElement>("text").Content = _content.Inner + " " + _contentNr.Inner;
    }
}