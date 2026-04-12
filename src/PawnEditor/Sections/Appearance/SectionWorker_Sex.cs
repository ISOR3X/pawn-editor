using HotSwap;
using Taffy;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Sex(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Sex",
            style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
        builder.Button(pawn.gender.GetLabel().CapitalizeFirst(), pawn.gender.GetIcon(), onClick: _ => { },
            style: new StyleOverride { width = 200f });
    }
}