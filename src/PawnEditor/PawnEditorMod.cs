using HarmonyLib;
using HotSwap;
using UnityEngine;
using Verse;
using Void;

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
        XMLLayoutParser.RegisterTag("section", () => new XMLComponents.SectionElement());

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
        listing.CheckboxLabeled("Allow resize", ref Settings.allowResize);
        listing.CheckboxLabeled("DEBUG: Draw leaf boxes", ref Settings.drawDebug);
        if (listing.ButtonText("Window presets"))
        {
            List<FloatMenuOption> opts =
            [
                new("Centered", () => Window_Editor.SavedWindowRect = Window_Editor.DefaultWindowRect),
                new("Left half",
                    () => Window_Editor.SavedWindowRect = new Rect(0, 0, Verse.UI.screenWidth / 2f, Verse.UI.screenHeight)),
                new("Full screen",
                    () => Window_Editor.SavedWindowRect = new Rect(0, 0, Verse.UI.screenWidth, Verse.UI.screenHeight))
            ];
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        listing.End();
    }
}