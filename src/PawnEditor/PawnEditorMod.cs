using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[HotSwappable]
public class PawnEditorMod : Mod
{
    public static Settings Settings = new();

    private readonly Listing_Horizontal _listing = new();

    public PawnEditorMod(ModContentPack content) : base(content)
    {
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
        _listing.Begin(inRect);
        _listing.ButtonTextLabeled("Restriction Mode", Settings.Restriction.ToString(), 6);
        if (_listing.ButtonTextLabeled("Window Size", Settings.Size.ToString(), 6))
            Find.WindowStack.Add(new FloatMenu([
                new FloatMenuOption(nameof(Settings.WindowSize.Small),
                    () => Settings.Size = Settings.WindowSize.Small),
                new FloatMenuOption(nameof(Settings.WindowSize.Medium),
                    () => Settings.Size = Settings.WindowSize.Medium),
                new FloatMenuOption(nameof(Settings.WindowSize.Large),
                    () => Settings.Size = Settings.WindowSize.Large)
            ]));

        _listing.End();
    }
}