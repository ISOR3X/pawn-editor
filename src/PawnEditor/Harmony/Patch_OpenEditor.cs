using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[HarmonyPatch(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DevToolStarterOnGUI))]
public class Patch_OpenEditor
{
    static void Prefix()
    {
        if (KeyBindingDefOf.PawnEditor_OpenEditor.KeyDownEvent)
        {
            if (Find.WindowStack.IsOpen<Window_Editor>()) Find.WindowStack.TryRemove(typeof(Window_Editor));
            else Find.WindowStack.Add(new Window_Editor());
        }
    }
}