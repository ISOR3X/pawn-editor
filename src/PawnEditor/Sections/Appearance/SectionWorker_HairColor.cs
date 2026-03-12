using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_HairColor(SectionDef def) : SectionWorker(def)
{
    private float _hairColorHeight = 30f;

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var hairColor = pawn.story.HairColor;
        listing.ColorPickerLabeled("Hair color", _hairColorHeight, ref hairColor, null,
            AppearanceUtility.GetHairColorsFor(pawn),
            c => AppearanceUtility.TrySetHairColor(c, pawn), out _hairColorHeight);
        AppearanceUtility.TrySetHairColor(hairColor, pawn);
    }
}