using RimWorld;

namespace PawnEditor;

[DefOf]
public static class TableDefOf
{
    public static readonly DefTableDef PawnEditor_Hairs = null!;
    public static readonly DefTableDef PawnEditor_Beards = null!;

    [MayRequireIdeology] public static readonly DefTableDef PawnEditor_FaceTattoos = null!;

    [MayRequireIdeology] public static readonly DefTableDef PawnEditor_BodyTattoos = null!;

    public static readonly ThingTableDef PawnEditor_Apparel = null!;

    static TableDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(TableDefOf));
    }
}