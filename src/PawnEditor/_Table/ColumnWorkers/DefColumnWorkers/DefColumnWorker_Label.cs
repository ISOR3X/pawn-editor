using Verse;

namespace PawnEditor;

public class DefColumnWorker_Label : ColumnWorker_Text<Def>
{
    public override string? GetTextFor(Def thing) => thing.label.CapitalizeFirst() ?? thing.ReadableDefName();
}