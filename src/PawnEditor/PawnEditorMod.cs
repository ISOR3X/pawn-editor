using HarmonyLib;
using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class PawnEditorMod : Mod
{
    public static string ModName = "PawnEditor";
    public static Settings Settings = new();

    public PawnEditorMod(ModContentPack content) : base(content)
    {
        ModName = content.Name;

        var harmony = new Harmony("com.isorex.pawneditor");
        harmony.PatchAll();

        Settings = GetSettings<Settings>();
    }

    public override string SettingsCategory()
    {
        return "Pawn Editor";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ButtonTextLabeled("Restriction Mode", Settings.Restriction.ToString());
        if (listing.ButtonTextLabeled("Window Size", Settings.Size.ToString()))
            Find.WindowStack.Add(new FloatMenu([
                new FloatMenuOption(nameof(Settings.WindowSize.Small),
                    () => Settings.Size = Settings.WindowSize.Small),
                new FloatMenuOption(nameof(Settings.WindowSize.Medium),
                    () => Settings.Size = Settings.WindowSize.Medium),
                new FloatMenuOption(nameof(Settings.WindowSize.Large),
                    () => Settings.Size = Settings.WindowSize.Large)
            ]));

        listing.End();
    }
}