using UnityEngine;
using Verse;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_FavColor(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var currentColor = pawn.story.favoriteColor?.color ?? Color.white;
        layout.ComponentById<TextElement>("text").Wrap = false;
        layout.ComponentById<TextElement>("text").Content = "Favorite color";
        layout.ComponentById<DivElement>("container").Draw = Verse.Widgets.DrawLightHighlight;
        layout.ComponentById<DivElement>("color_preview").Draw = r =>
            Verse.Widgets.DrawRectFast(r, currentColor);

        layout.ComponentById<ButtonElement>("button").OnClick = _ =>
            Find.WindowStack.Add(new Dialog_ColorPicker(c => pawn.story.favoriteColor?.color = c, currentColor));
    }
}