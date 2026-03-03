using RimWorld;

namespace PawnEditor;

[DefOf]
public static class TableDefOf
{
    public static TableDef PawnEditor_Hairs; 
    public static TableDef PawnEditor_Beards; 
    [MayRequireIdeology]
    public static TableDef PawnEditor_FaceTattoos;
    [MayRequireIdeology]
    public static TableDef PawnEditor_BodyTattoos; 
    static TableDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (TableDefOf));
}