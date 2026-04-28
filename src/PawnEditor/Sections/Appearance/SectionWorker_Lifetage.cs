using Verse;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Lifestage(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = "Lifestage";
        layout.ComponentById<ButtonElement>("button").Label = pawn.DevelopmentalStage.ToString();
        layout.ComponentById<ButtonElement>("button").Icon = pawn.DevelopmentalStage.Icon().Texture;
    }
}