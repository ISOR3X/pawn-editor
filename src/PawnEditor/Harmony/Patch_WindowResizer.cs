using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[HarmonyPatch(typeof(WindowResizer), nameof(WindowResizer.DoResizeControl))]
public class Patch_WindowResizer
{
    private static readonly Vector2 MaxWindowSize =
        new(Verse.UI.screenWidth, Verse.UI.screenHeight - MainTabWindow_Architect.ButHeight);

    private static Rect Postfix(Rect winRect, ref Rect __result)
    {
        __result.width = Mathf.Min(__result.width, MaxWindowSize.x);
        __result.height = Mathf.Min(__result.height, MaxWindowSize.y);
        return __result;
    }
}