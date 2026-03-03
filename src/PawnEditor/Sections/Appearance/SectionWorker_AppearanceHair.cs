using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_AppearanceHair : SectionWorker
{
    private readonly DefTable _beardDefTable;

    private readonly DefTable _hairDefTable;
    private readonly Listing_Horizontal _listing = new();

    private float _hairColorHeight = 30f; // Tracks the height of the rows for the skin colors.


    public SectionWorker_AppearanceHair(SectionDef def) : base(def)
    {
        var hairs = DefDatabase<HairDef>.AllDefs;
        var beards = DefDatabase<BeardDef>.AllDefs;
        _hairDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => hairs),
            HairDefOf.Bald);
        _beardDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => beards),
            BeardDefOf.NoBeard);
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        _listing.Begin(inRect);
        var hairTableRect = _listing.GetRect(6, _hairDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(hairTableRect.TakeTopPart(UIUtility.ButtonHeight), "Hair");
        _hairDefTable.TableOnGUI(hairTableRect);

        var beardTableRect = _listing.GetRect(6, _beardDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(beardTableRect.TakeTopPart(UIUtility.ButtonHeight), "Beard");
        if (pawn.style.CanWantBeard || PawnEditorMod.Settings.Restriction == Settings.RestrictionMode.None)
            _beardDefTable.TableOnGUI(beardTableRect);
        else
            Widgets.Label(beardTableRect,
                $"No beards available for {pawn.Name.ToStringShort}".Colorize(ColoredText.SubtleGrayColor));

        var hairColor = pawn.story.HairColor;
        _listing.ColorPickerLabeled("Hair color", _hairColorHeight, ref hairColor, null,
            AppearanceUtility.GetHairColorsFor(pawn),
            c => AppearanceUtility.TrySetHairColor(c, ref pawn), out _hairColorHeight);
        AppearanceUtility.TrySetHairColor(hairColor, ref pawn);

        _listing.End();
        inRect.TakeTopPart(_listing.TotalHeight);
    }
}