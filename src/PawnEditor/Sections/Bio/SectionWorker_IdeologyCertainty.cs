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
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var cert = pawn.ideo.certaintyInt;
        var minMaxCert = new FloatRange(0, 1);
        layout.ComponentById<TextElement>("min").Content = minMaxCert.min.ToStringPercent();
        layout.ComponentById<TextElement>("max").Content = minMaxCert.max.ToStringPercent();
        layout.ComponentById<TextElement>("value").Content = pawn.ideo.Certainty.ToStringPercent();
        layout.ComponentById<DivElement>("certainty").Children = b =>
        {
            b.InputRange(ref cert, minMaxCert.min, minMaxCert.max, step: 0.05f,
                style: new StyleOverride { width = Dimension.Percent(1f) });
            if (!Mathf.Approximately(cert, pawn.ideo.certaintyInt))
                pawn.ideo.certaintyInt = cert;
        };
    }
}