using RimWorld;
using Verse;

namespace PawnEditor;

public class PawnColumnWorker_Location : PawnColumnWorker_Text
{
    public override string GetTextFor(Pawn pawn)
    {
        return PawnLocation.GetLocationLabel(pawn).Colorize(ColoredText.SubtleGrayColor);
    }
}