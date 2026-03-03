using RimWorld;

namespace PawnEditor;

[DefOf]
public static class PawnTableDefOf
{
    public static PawnTableDef PawnEditor_ColonyOverview;
    
    static PawnTableDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (PawnTableDefOf));
}