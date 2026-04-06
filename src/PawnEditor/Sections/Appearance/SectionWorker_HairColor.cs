using HotSwap;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_HairColor(SectionDef def) : SectionWorker(def)
{
    private float _hairColorHeight = 30f;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Item(height: Text.LineHeight + _hairColorHeight, draw: r =>
        {
            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(r);
            var color = pawn.story.HairColor;
            listing.ColorPickerLabeled("Hair color", _hairColorHeight, ref color, null,
                AppearanceUtility.GetHairColorsFor(pawn),
                c => AppearanceUtility.TrySetHairColor(c, pawn), out _hairColorHeight);
            AppearanceUtility.TrySetHairColor(color, pawn);
            listing.End();
        });
    }
}
