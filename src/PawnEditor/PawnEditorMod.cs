using HarmonyLib;
using PawnEditor.XMLComponents;
using UnityEngine;
using Verse;
using Void;

namespace PawnEditor;

public class PawnEditorMod : Mod
{
    public static string ModName = "PawnEditor";
    public static PawnEditorSettings PawnEditorSettings = new();

    public PawnEditorMod(ModContentPack content) : base(content)
    {
        ModName = content.Name;
        PawnEditorSettings = GetSettings<PawnEditorSettings>();

        var harmony = new Harmony("com.isorex.pawneditor");
        harmony.PatchAll();
        
        XMLLayoutParser.RegisterTag("section", () => new SectionElement());

        // Save settings when the game quits.
        Application.quitting += () => PawnEditorSettings.Write();
    }

    public override string SettingsCategory()
    {
        return "Pawn Editor";
    }


    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ButtonTextLabeled("Restriction Mode", PawnEditorSettings.restriction.ToString());
        listing.CheckboxLabeled("Allow resize", ref PawnEditorSettings.allowResize);
        listing.CheckboxLabeled("DEBUG: Draw leaf boxes", ref PawnEditorSettings.drawDebug);
        if (listing.ButtonText("Window presets"))
        {
            List<FloatMenuOption> opts =
            [
                new("Centered", () => Window_Editor.SavedWindowRect = Window_Editor.DefaultWindowRect),
                new("Left half",
                    () => Window_Editor.SavedWindowRect = new Rect(0, 0, UI.screenWidth / 2f, UI.screenHeight)),
                new("Full screen",
                    () => Window_Editor.SavedWindowRect = new Rect(0, 0, UI.screenWidth, UI.screenHeight))
            ];
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        listing.End();
    }
}