using Verse;
using Layout = Void.Layout;
using Void.XMLComponents;

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