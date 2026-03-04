using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;
[Reloadable]
public class SectionWorker_AppearanceHair : SectionWorker
{
    private readonly DefTable _beardDefTable;
    private readonly DefTable _hairDefTable;
    private float _hairColorHeight = 30f;

    public SectionWorker_AppearanceHair(SectionDef def) : base(def)
    {
        var hairs = DefDatabase<HairDef>.AllDefs;
        var beards = DefDatabase<BeardDef>.AllDefs;
        _hairDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => hairs), HairDefOf.Bald);
        _beardDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => beards), BeardDefOf.NoBeard);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var hairTableRect = listing.GetRect(_hairDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(hairTableRect.TakeTopPart(UIUtility.ButtonHeight), "Hair");
        _hairDefTable.TableOnGUI(hairTableRect);

        var beardTableRect = listing.GetRect(_beardDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(beardTableRect.TakeTopPart(UIUtility.ButtonHeight), "Beard");
        if (pawn.style.CanWantBeard || PawnEditorMod.Settings.Restriction == Settings.RestrictionMode.None)
            _beardDefTable.TableOnGUI(beardTableRect);
        else
            Widgets.Label(beardTableRect,
                $"No beards available for {pawn.Name.ToStringShort}".Colorize(ColoredText.SubtleGrayColor));

        var hairColor = pawn.story.HairColor;
        listing.ColorPickerLabeled("Hair color", _hairColorHeight, ref hairColor, null,
            AppearanceUtility.GetHairColorsFor(pawn),
            c => AppearanceUtility.TrySetHairColor(c, ref pawn), out _hairColorHeight);
        AppearanceUtility.TrySetHairColor(hairColor, ref pawn);
    }
}