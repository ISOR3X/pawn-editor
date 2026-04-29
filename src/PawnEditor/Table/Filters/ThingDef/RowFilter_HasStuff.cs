using Verse;

namespace PawnEditor.Table;

public class RowFilter_HasStuff() : RowFilter_Checkbox<ThingDef>(d => d.MadeFromStuff, "Stuffable only");