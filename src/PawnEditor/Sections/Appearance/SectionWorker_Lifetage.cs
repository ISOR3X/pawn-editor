using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Lifestage(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Item(r =>
        {
            if (UIUtility.ButtonTextLabeled_WithIcon(r, "Lifestage", pawn.DevelopmentalStage.ToString(),
                    pawn.DevelopmentalStage.Icon().Texture))
            {
            }
        }, new StyleOverride { height = UIUtility.ButtonHeight });
    }
}
