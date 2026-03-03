using RimWorld;

namespace PawnEditor;

[DefOf]
public static class PawnTableDefOf
{
    public static readonly PawnTableDef PawnEditor_ColonyOverview = null!;

    static PawnTableDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PawnTableDefOf));
    }
}