using RimWorld;

namespace PawnEditor;

[DefOf]
public static class PawnEditorDefOf
{
    public static TabGroupDef Humanlike;
    public static TabGroupDef AnimalMech;
    public static TabGroupDef PlayerFaction;
    public static TabGroupDef NPCFaction;

    static PawnEditorDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PawnEditorDefOf));
    }
}
