using HotSwap;
using PawnEditor.Extensions;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Info(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        // TODO: This shows Allowed area selector even for pawns off the map.
        col.Item(height: 120f, draw: r => Widgets.InspectPane(r.TakeLeftPart(400f), pawn));
    }
}
