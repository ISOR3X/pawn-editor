using HarmonyLib;
using JetBrains.Annotations;
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
            if (Current.ProgramState == ProgramState.Playing)
                VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug;
        
        if (KeyBindingDefOf.PawnEditor_HotReloadDefs.KeyDownEvent)
        {
            var open = Find.WindowStack.IsOpen<Window_Editor>();
            PlayDataLoader.HotReloadDefs();
            if (open) Find.WindowStack.Add(new Window_Editor());
        }
    }
}