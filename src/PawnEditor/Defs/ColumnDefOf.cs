using JetBrains.Annotations;
using RimWorld;

namespace PawnEditor;

[DefOf]
[UsedImplicitly]
public class ColumnDefOf
{
    public static ColumnDef PawnEditor_LabelWithIcon = null!;

    static ColumnDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ColumnDefOf));
    }
}