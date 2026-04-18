using HotSwap;
using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Lifestage(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Lifestage",
            style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
        builder.Button(pawn.DevelopmentalStage.ToString(), pawn.DevelopmentalStage.Icon().Texture, onClick: _ => { },
            style: new StyleOverride { minWidth = 100f,  maxWidth = 300f, flexGrow = 1f, width = Dimension.AUTO });
    }
}