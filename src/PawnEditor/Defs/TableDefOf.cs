using RimWorld;

namespace PawnEditor;

[DefOf]
public static class TableDefOf
{
    public static readonly DefTableDef PawnEditor_DefTable_Hair = null!;
    public static readonly DefTableDef PawnEditor_DefTable_Beard = null!;
    public static readonly DefTableDef PawnEditor_DefTable_ThingDef = null!;

    [MayRequireIdeology] public static readonly DefTableDef PawnEditor_FaceTattoos = null!;
    [MayRequireIdeology] public static readonly DefTableDef PawnEditor_BodyTattoos = null!;

    public static readonly ThingTableDef PawnEditor_ThingTable_Apparel = null!;
    public static readonly ThingTableDef PawnEditor_ThingTable_Equipment = null!;
    public static readonly ThingTableDef PawnEditor_ThingTable_Inventory = null!;

    static TableDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(TableDefOf));
    }
}