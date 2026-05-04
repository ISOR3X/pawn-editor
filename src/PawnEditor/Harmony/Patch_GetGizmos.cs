using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public class Patch_GetGizmos
{
    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
    {
        foreach (var gizmo in __result)
            yield return gizmo;

        yield return new Command_Action
        {
            defaultLabel = "Open editor",
            action = () =>
            {
                var editor = Find.WindowStack.WindowOfType<Window_Editor>() ?? new Window_Editor();
                editor.TrySelect(__instance);
                if (!Find.WindowStack.IsOpen<Window_Editor>())
                    Find.WindowStack.Add(editor);
            }
        };
    }
}