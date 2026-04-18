using HotSwap;
using Taffy;
using Verse;
using Void;
using Void.Components;

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
        builder.Text("Race",
            style: new StyleOverride { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
        builder.Button(pawn.kindDef.race.LabelCap, onClick: _ => { },
            style: new StyleOverride { width = 200f });
    }

    private static List<ThingDef> GetRacesForPawn(Pawn pawn)
    {
        return [];
    }
}