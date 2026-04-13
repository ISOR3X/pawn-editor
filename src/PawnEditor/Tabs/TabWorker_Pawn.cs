using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public class TabWorker_Pawn(TabDef def) : TabWorker<PawnContext>(def)
{
    protected override StyleOverride? LayoutStyle => Def.layout.RootStyle;

    protected override void DoInnerTabContents(TaffyBuilder col, PawnContext ctx)
    {
        Def.layout.BuildChildrenInto(col, ctx.Value, s => s.Worker.ShowSection(ctx.Value));
    }
}
