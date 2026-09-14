using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

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
            // Opens all four benchmark windows side by side, for comparing them (e.g. with Dubs
            // Performance Analyzer) in the same play session instead of one at a time.
            if (Find.WindowStack.IsOpen<Window_BenchmarkTaffy>())
            {
                Find.WindowStack.TryRemove(typeof(Window_BenchmarkTaffy));
                Find.WindowStack.TryRemove(typeof(Window_BenchmarkVerse));
                Find.WindowStack.TryRemove(typeof(Window_BenchmarkVoid));
                Find.WindowStack.TryRemove(typeof(Window_BenchmarkXml));
            }
            else
            {
                const float margin = 20f;
                var width = (UI.screenWidth - margin * 5f) / 4f;
                var height = UI.screenHeight * 0.7f;

                var taffy = new Window_BenchmarkTaffy();
                Find.WindowStack.Add(taffy);
                taffy.windowRect = new Rect(margin, margin, width, height);

                var verse = new Window_BenchmarkVerse();
                Find.WindowStack.Add(verse);
                verse.windowRect = new Rect(margin * 2f + width, margin, width, height);

                var voidWindow = new Window_BenchmarkVoid();
                Find.WindowStack.Add(voidWindow);
                voidWindow.windowRect = new Rect(margin * 3f + width * 2f, margin, width, height);

                var xmlWindow = new Window_BenchmarkXml();
                Find.WindowStack.Add(xmlWindow);
                xmlWindow.windowRect = new Rect(margin * 4f + width * 3f, margin, width, height);
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