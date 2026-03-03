using JetBrains.Annotations;
using RimWorld;

namespace PawnEditor;

[DefOf]
[UsedImplicitly]
public class ColumnDefOf
{
    public static ColumnDef PawnEditor_LabelWithIcon; 
    static ColumnDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (ColumnDefOf));
}