using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Race(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return base.ShowSection(p) && GetRacesForPawn(p).Any();
    }

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            if (UIUtility.ButtonTextLabeled(r, "Race", pawn.kindDef.race.LabelCap))
            {
            }
        });
    }

    private static List<ThingDef> GetRacesForPawn(Pawn pawn)
    {
        return [];
    }
}
