using Verse;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Sex(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = "Sex";
        layout.ComponentById<ButtonElement>("button").Label = pawn.gender.GetLabel().CapitalizeFirst();
        layout.ComponentById<ButtonElement>("button").Icon = pawn.gender.GetIcon();
    }
}