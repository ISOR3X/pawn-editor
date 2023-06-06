using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class PawnEditorMod : Mod
{
    public static Harmony Harm;
    public static PawnEditorSettings Settings;

    public PawnEditorMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("legodude17.pawneditor");
        Settings = GetSettings<PawnEditorSettings>();
        Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents)),
            new HarmonyMethod(GetType(), nameof(OverrideVanilla)));
    }

    public static bool OverrideVanilla(Rect rect, Page_ConfigureStartingPawns __instance)
    {
        if (!Settings.OverrideVanilla) return true;
        PawnEditor.DoUI(rect, __instance.DoBack, __instance.DoNext, true);
        return false;
    }
}

public class PawnEditorSettings : ModSettings
{
    public bool OverrideVanilla = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OverrideVanilla, nameof(OverrideVanilla), true);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class HotSwappableAttribute : Attribute { }
