using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Sex(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            if (UIUtility.ButtonTextLabeled_WithIcon(r, "Sex", pawn.gender.GetLabel().CapitalizeFirst(),
                    pawn.gender.GetIcon()))
            {
            }
        });
    }
}
