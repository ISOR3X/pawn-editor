using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace PawnEditor;

[DefOf]
[UsedImplicitly]
public static class KeyBindingDefOf
{
    public static readonly KeyBindingDef PawnEditor_OpenEditor = null!;
    public static readonly KeyBindingDef PawnEditor_OpenDev = null!;

    static KeyBindingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(KeyBindingDefOf));
    }
}