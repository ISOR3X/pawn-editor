using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace PawnEditor
{
    [DefOf]
    [UsedImplicitly]
    public static class KeyBindingDefOf
    {
        public static KeyBindingDef PawnEditor_OpenEditor;

        static KeyBindingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyBindingDefOf));
    }
}