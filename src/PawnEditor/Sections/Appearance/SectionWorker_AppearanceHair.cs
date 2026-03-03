using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_AppearanceHair : SectionWorker
{
    readonly Listing_Horizontal listing = new Listing_Horizontal();

    private DefTable hairDefTable;
    private DefTable beardDefTable;

    private float hairColorHeight = 30f; // Tracks the height of the rows for the skin colors.


    public SectionWorker_AppearanceHair(SectionDef def) : base(def)
    {
        IEnumerable<HairDef> hairs = DefDatabase<HairDef>.AllDefs;
        IEnumerable<BeardDef> beards = DefDatabase<BeardDef>.AllDefs;
        hairDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass, TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => hairs),
            HairDefOf.Bald);
        beardDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass, TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => beards),
            BeardDefOf.NoBeard);
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        listing.Begin(inRect);
        var hairTableRect = listing.GetRect(6, height: hairDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(hairTableRect.TakeTopPart(UIUtility.ButtonHeight), "Hair");
        hairDefTable.TableOnGUI(hairTableRect);

        var beardTableRect = listing.GetRect(6, height: beardDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(beardTableRect.TakeTopPart(UIUtility.ButtonHeight), "Beard");
        if (pawn.style.CanWantBeard || PawnEditorMod.Settings.Restriction == Settings.RestrictionMode.None)
        {
            beardDefTable.TableOnGUI(beardTableRect);
        }
        else Widgets.Label(beardTableRect, $"No beards available for {pawn.Name.ToStringShort}".Colorize(ColoredText.SubtleGrayColor));

        var hairColor = pawn.story.HairColor;
        listing.ColorPickerLabeled("Hair color", hairColorHeight, ref hairColor, null, AppearanceUtility.GetHairColorsFor(pawn),
            c => AppearanceUtility.TrySetHairColor(c, ref pawn), out hairColorHeight);
        AppearanceUtility.TrySetHairColor(hairColor, ref pawn);

        listing.End();
        inRect.TakeTopPart(listing.totalHeight);
    }
}