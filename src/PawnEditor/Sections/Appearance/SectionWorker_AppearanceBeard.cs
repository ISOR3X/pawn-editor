using System;
using System.Collections.Generic;
using PawnEditor.Extensions;
using RimWorld;
using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_AppearanceBeard : SectionWorker
{
    private readonly DefTable _beardDefTable;
    private float _hairColorHeight = 30f;

    public SectionWorker_AppearanceBeard(SectionDef def) : base(def)
    {
        var beards = DefDatabase<BeardDef>.AllDefs;
        _beardDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => beards), BeardDefOf.NoBeard);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var beardTableRect = listing.GetRect(_beardDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        Widgets.WidgetLabel(beardTableRect.TakeTopPart(UIUtility.ButtonHeight), "Beard");
        if (pawn.style.CanWantBeard || PawnEditorMod.Settings.Restriction == Settings.RestrictionMode.None)
            _beardDefTable.TableOnGUI(beardTableRect);
        else
            Verse.Widgets.Label(beardTableRect,
                $"No beards available for {pawn.Name.ToStringShort}".Colorize(ColoredText.SubtleGrayColor));

        var hairColor = pawn.story.HairColor;
        listing.ColorPickerLabeled("Hair color", _hairColorHeight, ref hairColor, null,
            AppearanceUtility.GetHairColorsFor(pawn),
            c => AppearanceUtility.TrySetHairColor(c, pawn), out _hairColorHeight);
    }
}