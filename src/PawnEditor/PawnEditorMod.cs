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
        listing.ButtonTextLabeled("Restriction Mode", Settings.restriction.ToString());
        if (listing.ButtonTextLabeled("Window Size", Settings.size.ToString()))
            Find.WindowStack.Add(new FloatMenu([
                new FloatMenuOption(nameof(Settings.WindowSize.Small),
                    () => Settings.size = Settings.WindowSize.Small),
                new FloatMenuOption(nameof(Settings.WindowSize.Medium),
                    () => Settings.size = Settings.WindowSize.Medium),
                new FloatMenuOption(nameof(Settings.WindowSize.Large),
                    () => Settings.size = Settings.WindowSize.Large)
            ]));
        listing.CheckboxLabeled("DEBUG: Draw leaf boxes", ref Settings.drawDebug);

        listing.End();
    }
}