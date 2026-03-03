using RimWorld;

namespace PawnEditor;

[DefOf]
public static class TableDefOf
{
    public static readonly TableDef PawnEditor_Hairs = null!;
    public static readonly TableDef PawnEditor_Beards = null!;

    [MayRequireIdeology] public static readonly TableDef PawnEditor_FaceTattoos = null!;

    [MayRequireIdeology] public static readonly TableDef PawnEditor_BodyTattoos = null!;

    static TableDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(TableDefOf));
    }
}