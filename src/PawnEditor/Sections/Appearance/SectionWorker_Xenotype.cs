using HotSwap;
using Taffy;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Xenotype(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Xenotype",
            style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
        builder.Button(pawn.genes.XenotypeLabelCap, pawn.genes.XenotypeIcon, onClick: _ => { },
            style: new StyleOverride { minWidth = 100f,  maxWidth = 300f, flexGrow = 1f, width = Dimension.AUTO });
    }
}