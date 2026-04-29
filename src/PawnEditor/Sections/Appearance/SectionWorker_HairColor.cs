using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_HairColor(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var currentColor = pawn.story.HairColor;
        layout.ComponentById<TextElement>("text").Wrap = false;
        layout.ComponentById<TextElement>("text").Content = "Hair color";

        layout.ComponentById<DivElement>("container").Draw = Verse.Widgets.DrawLightHighlight;

        layout.ComponentById<DivElement>("color_preview").Draw = r =>
            Verse.Widgets.DrawRectFast(r, currentColor);

        layout.ComponentById<ButtonElement>("button").OnClick = _ =>
            Find.WindowStack.Add(new Dialog_ColorPicker(c => AppearanceUtility.TrySetHairColor(c, pawn), currentColor,
                AppearanceUtility.GetHairColorsFor(pawn)));
    }
}