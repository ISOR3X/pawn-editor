using Taffy;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Xenotype(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = "Xenotype";
        layout.ComponentById<ButtonElement>("button").Label = pawn.genes.XenotypeLabelCap;
        layout.ComponentById<ButtonElement>("button").Icon = pawn.genes.XenotypeIcon;
    }
}