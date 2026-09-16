using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_SkinColor(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var currentColor = pawn.story.SkinColor;
        layout.ComponentById<TextElement>("text").Wrap = false;
        layout.ComponentById<TextElement>("text").Content = "Skin color";
        layout.ComponentById<DivElement>("container").Draw = Verse.Widgets.DrawLightHighlight;
        layout.ComponentById<DivElement>("color_preview").Draw = r =>
            Verse.Widgets.DrawRectFast(r, currentColor);

        layout.ComponentById<ButtonElement>("button").OnClick = _ =>
            Find.WindowStack.Add(new Dialog_ColorPicker(c => TrySetSkinColor(c, pawn), currentColor,
                AppearanceUtility.GetSkinColorsFor(pawn)));
    }

    private static void TrySetSkinColor(Color color, Pawn pawn, bool silent = true)
    {
        if (pawn.story.SkinColor == color) return;
        if (AppearanceUtility.ColorsFromGenes.Keys.Contains(color))
        {
            pawn.story.skinColorOverride = null;
            var geneToRemove = pawn.genes.GetFirstEndogeneByCategory(EndogeneCategory.Melanin);
            if (geneToRemove == null) return;
            pawn.genes.RemoveGene(pawn.genes.GetGene(geneToRemove));
            pawn.genes.AddGene(AppearanceUtility.ColorsFromGenes[color], false);
            if (!silent) Messages.Message("Changed melanin gene for " + pawn.Name, MessageTypeDefOf.NeutralEvent);
        }
        else
        {
            pawn.story.skinColorOverride = color;
        }

        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }
}