using RimWorld;
using Verse;

namespace PawnEditor;

public class PawnColumnWorker_Type : PawnColumnWorker_Text
{
    public override string GetTextFor(Pawn pawn)
    {
        return PawnUtility.GetPawnCategory(pawn).ToString().Colorize(ColoredText.SubtleGrayColor);
    }
}