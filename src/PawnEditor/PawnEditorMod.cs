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
        var harmony = new Harmony("com.isorex.pawneditor");
        harmony.PatchAll();

        ModName = content.Name;
        Settings = GetSettings<Settings>();

        // Save settings when the game quits.
        Application.quitting += () => Settings.Write();
    }

    public override string SettingsCategory() => "Pawn Editor";


    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ButtonTextLabeled("Restriction Mode", Settings.restriction.ToString());
        listing.CheckboxLabeled("DEBUG: Draw leaf boxes", ref Settings.drawDebug);
        if (listing.ButtonText("Reset window size and position"))
        {
            Window_Editor.SavedWindowRect = Window_Editor.DefaultWindowRect;
        }

        listing.End();
    }
}