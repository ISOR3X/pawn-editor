using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PawnEditor;
[Reloadable]
public class SectionWorker_AppearanceHair : SectionWorker
{
    private readonly DefTable _hairDefTable;
    private float _hairColorHeight = 30f;

    public SectionWorker_AppearanceHair(SectionDef def) : base(def)
    {
        var hairs = DefDatabase<HairDef>.AllDefs;
        _hairDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => hairs), HairDefOf.Bald);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var hairTableRect = listing.GetRect(_hairDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(hairTableRect.TakeTopPart(UIUtility.ButtonHeight), "Hair");
        _hairDefTable.TableOnGUI(hairTableRect);

        var hairColor = pawn.story.HairColor;
        listing.ColorPickerLabeled("Hair color", _hairColorHeight, ref hairColor, null,
            AppearanceUtility.GetHairColorsFor(pawn),
            c => AppearanceUtility.TrySetHairColor(c, pawn), out _hairColorHeight);
        AppearanceUtility.TrySetHairColor(hairColor, pawn);
    }
}