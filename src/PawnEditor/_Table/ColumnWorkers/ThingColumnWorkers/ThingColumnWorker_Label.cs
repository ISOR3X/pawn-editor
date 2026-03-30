using Verse;

namespace PawnEditor;

public class ThingColumnWorker_Label : ColumnWorker_Text<Thing>
{
    public override string? GetTextFor(Thing thing) => thing.LabelCap;
}