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
    public static Vector2 maxWindowSize = new Vector2(UI.screenWidth, UI.screenHeight - MainTabWindow_Architect.ButHeight);

    static Rect Postfix(Rect winRect, ref Rect __result)
    {
        __result.width = Mathf.Min(__result.width, maxWindowSize.x);
        __result.height = Mathf.Min(__result.height, maxWindowSize.y);
        return __result;
    }
}