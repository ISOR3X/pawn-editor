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
        ApplySettings();
    }

    public override string SettingsCategory() => "PawnEditor".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("PawnEdtior.OverrideVanilla".Translate(), ref Settings.OverrideVanilla, "PawnEditor.OverrideVanilla.Desc".Translate());
        listing.End();
    }

    private void ApplySettings()
    {
        Harm.Unpatch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents)),
            AccessTools.Method(GetType(), nameof(OverrideVanilla)));
        if (Settings.OverrideVanilla)
            Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents)),
                new HarmonyMethod(GetType(), nameof(OverrideVanilla)));
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        ApplySettings();
    }

    public static bool OverrideVanilla(Rect rect, Page_ConfigureStartingPawns __instance)
    {
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
