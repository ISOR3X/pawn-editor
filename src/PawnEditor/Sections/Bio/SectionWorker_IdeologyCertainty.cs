using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_IdeologyCertainty(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var minMaxCert = new FloatRange(0, 1);
        layout.ComponentById<TextElement>("min").Content = minMaxCert.min.ToStringPercent();
        layout.ComponentById<TextElement>("max").Content = minMaxCert.max.ToStringPercent();
        layout.ComponentById<TextElement>("value").Content = pawn.ideo.Certainty.ToStringPercent();

        var slider = layout.ComponentById<InputElement>("certainty");
        slider.Value = new Reactive<float>(() => pawn.ideo.certaintyInt, v => pawn.ideo.certaintyInt = v);
        slider.MinMax = minMaxCert;
        slider.Step = 0.05f;
    }
}