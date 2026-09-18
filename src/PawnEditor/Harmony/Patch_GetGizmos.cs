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

        yield return new Command_Action
        {
            defaultLabel = "Open editor (v2)",
            defaultDesc = "Open the v2 editor with this pawn as the subject. Dev mode only.",
            action = () =>
            {
                Find.WindowStack.TryRemove(typeof(v2.Window_Editor), false);
                Find.WindowStack.Add(new v2.Window_Editor(__instance));
            }
        };
    }
}

/*
// Note to self: Finish section conversion before enabling this
// Scope creep is real!
[UsedImplicitly]
[HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
public class Patch_GetGizmosThing
{
    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Thing __instance)
    {
        foreach (var gizmo in __result)
            yield return gizmo;

        if (!Prefs.DevMode) yield break;

        yield return new Command_Action
        {
            defaultLabel = "Open editor (v2)",
            defaultDesc = "Open the v2 editor with this pawn as the subject. Dev mode only.",
            action = () =>
            {
                Find.WindowStack.TryRemove(typeof(v2.Window_Editor), false);
                Find.WindowStack.Add(new v2.Window_Editor(__instance));
            }
        };
    }
}
 */
