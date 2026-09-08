using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Void;

namespace PawnEditor;

[UsedImplicitly]
[HarmonyPatch(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DevToolStarterOnGUI))]
public class Patch_OpenEditor
{
    private static void Prefix()
    {
        if (KeyBindingDefOf.PawnEditor_OpenEditor.KeyDownEvent)
        {
            if (Find.WindowStack.IsOpen<Window_Editor>()) Find.WindowStack.TryRemove(typeof(Window_Editor));
            else Find.WindowStack.Add(new Window_Editor());
        }

        if (KeyBindingDefOf.PawnEditor_OpenDev.KeyDownEvent)
        {
            // Shift opens the Void.v2 benchmark window instead of the v1 one.
            if (Event.current.shift)
            {
                if (Find.WindowStack.IsOpen<Window_BenchmarkV4>()) Find.WindowStack.TryRemove(typeof(Window_BenchmarkV4));
                else Find.WindowStack.Add(new Window_BenchmarkV4());
            }
            else
            {
                if (Find.WindowStack.IsOpen<Window_Benchmark>()) Find.WindowStack.TryRemove(typeof(Window_Benchmark));
                else Find.WindowStack.Add(new Window_Benchmark());
            }
        }

        if (KeyBindingDefOf.PawnEditor_HotReloadDefs.KeyDownEvent)
        {
            var open = Find.WindowStack.IsOpen<Window_Editor>();
            PlayDataLoader.HotReloadDefs();
            if (open) Find.WindowStack.Add(new Window_Editor());
        }
    }
}
