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
        listing.CheckboxLabeled("PawnEditor.InGameDevButton".Translate(), ref Settings.InGameDevButton, "PawnEditor.InGameDevButton.Desc".Translate());
        listing.End();
    }

    private void ApplySettings()
    {
        Harm.UnpatchAll(Harm.Id);
        if (Settings.OverrideVanilla)
            Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents)),
                new HarmonyMethod(GetType(), nameof(OverrideVanilla)));
        else
            Harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DrawXenotypeEditorButton)),
                new HarmonyMethod(GetType(), nameof(AddEditorButton)));

        if (Settings.InGameDevButton)
            Harm.Patch(AccessTools.Method(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DrawButtons)),
                postfix: new HarmonyMethod(GetType(), nameof(AddDevButton)));
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

    public static bool AddEditorButton(Rect rect, Page_ConfigureStartingPawns __instance)
    {
        float x, y;
        if (ModsConfig.BiotechActive)
        {
            Text.Font = GameFont.Small;
            x = rect.x + rect.width / 2 + 2;
            y = rect.y + rect.height - 38f;
            if (Widgets.ButtonText(new Rect(x, y, Page.BottomButSize.x, Page.BottomButSize.y), "XenotypeEditor".Translate()))
                Find.WindowStack.Add(new Dialog_CreateXenotype(StartingPawnUtility.PawnIndex(__instance.curPawn), delegate
                {
                    CharacterCardUtility.cachedCustomXenotypes = null;
                    __instance.RandomizeCurPawn();
                }));
            x = rect.x + rect.width / 2 - 2 - Page.BottomButSize.x;
            y = rect.y + rect.height - 38f;
        }
        else
        {
            x = (rect.width - Page.BottomButSize.x) / 2f;
            y = rect.y + rect.height - 38f;
        }

        if (Widgets.ButtonText(new Rect(x, y, Page.BottomButSize.x, Page.BottomButSize.y), "PawnEditor.CharacterEditor".Translate()))
            Find.WindowStack.Add(new Dialog_PawnEditor_Pregame(__instance.DoNext));

        return false;
    }

    public static void AddDevButton(DebugWindowsOpener __instance)
    {
        if (Current.ProgramState == ProgramState.Playing
         && __instance.widgetRow.ButtonIcon(TexPawnEditor.OpenPawnEditor, "PawnEditor.CharacterEditor".Translate()))
            Find.WindowStack.Add(new Dialog_PawnEditor_InGame());
    }
}

public class PawnEditorSettings : ModSettings
{
    public bool InGameDevButton = true;
    public bool OverrideVanilla = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OverrideVanilla, nameof(OverrideVanilla), true);
        Scribe_Values.Look(ref InGameDevButton, nameof(InGameDevButton), true);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class HotSwappableAttribute : Attribute { }
