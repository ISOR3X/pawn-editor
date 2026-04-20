using Verse;
using Void;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class TabWorker_Pawn(TabDef def) : TabWorker<PawnContext>(def)
{
    protected override void DoInnerTabContents(TaffyBuilder col, PawnContext ctx)
    {
        if (Def.layout == null) return;
        new Layout(Def.layout, ctx).Render(col);
    }
}