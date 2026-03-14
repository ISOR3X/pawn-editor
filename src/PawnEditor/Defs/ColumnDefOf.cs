using JetBrains.Annotations;
using RimWorld;

namespace PawnEditor;

[DefOf]
[UsedImplicitly]
public class ColumnDefOf
{
    public static DefColumnDef PawnEditor_LabelWithIcon = null!;

    static ColumnDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ColumnDefOf));
    }
}