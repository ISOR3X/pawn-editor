using Taffy;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Sex(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = "Sex";
        layout.ComponentById<ButtonElement>("button").Label = pawn.gender.GetLabel().CapitalizeFirst();
    }
    
    // protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    // {
    //     builder.Text("Sex",
    //         style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
    //     builder.Button(pawn.gender.GetLabel().CapitalizeFirst(), pawn.gender.GetIcon(), onClick: _ => { },
    //         style: new StyleOverride { minWidth = 100f, maxWidth = 300f, flexGrow = 1f, width = Dimension.AUTO });
    // }
}