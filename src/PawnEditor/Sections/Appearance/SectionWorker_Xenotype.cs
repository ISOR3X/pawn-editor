using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Xenotype(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Item(r =>
        {
            if (UIUtility.ButtonTextLabeled_WithIcon(r, "Xenotype", pawn.genes.XenotypeLabelCap,
                    pawn.genes.XenotypeIcon))
            {
            }
        }, new StyleOverride { height = UIUtility.ButtonHeight });
    }
}