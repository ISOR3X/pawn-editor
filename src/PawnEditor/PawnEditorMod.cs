using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;


namespace PawnEditor
{
    [UsedImplicitly]
    [HotSwappable]
    public class PawnEditorMod : Mod
    {
        public static Settings Settings;
        public override string SettingsCategory() => "Pawn Editor";
        private readonly Listing_Horizontal listing = new Listing_Horizontal();

        public PawnEditorMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.isorex.pawneditor");
            harmony.PatchAll();

            Settings = GetSettings<Settings>(); 
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            listing.Begin(inRect);
            listing.ButtonTextLabeled("Restriction Mode", Settings.Restriction.ToString(), 6);
            if (listing.ButtonTextLabeled("Window Size", Settings.Size.ToString(), 6))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>()
                {
                    new FloatMenuOption(Settings.WindowSize.Small.ToString(), () => Settings.Size = Settings.WindowSize.Small),
                    new FloatMenuOption(Settings.WindowSize.Medium.ToString(), () => Settings.Size = Settings.WindowSize.Medium),
                    new FloatMenuOption(Settings.WindowSize.Large.ToString(), () => Settings.Size = Settings.WindowSize.Large)
                }));
            }

            listing.End();
        }
    }
}