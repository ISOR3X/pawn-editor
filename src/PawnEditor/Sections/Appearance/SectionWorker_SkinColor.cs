using System.Collections.Generic;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_SkinColor(SectionDef def) : SectionWorker(def)
{
    private float _skinColorHeight = 30f;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Item(height: Text.LineHeight + _skinColorHeight, draw: r =>
        {
            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(r);
            var p = pawn;
            var skinColor = pawn.story.SkinColor;
            var availableColors = AppearanceUtility.GetSkinColorsFor(pawn);
            var specialColors = new Dictionary<string, Color> { { "Old", pawn.story.SkinColor } };
            if (pawn.story.favoriteColor != null) specialColors["Favorite"] = pawn.story.favoriteColor.color;
            if (pawn.story.SkinColorOverriden && pawn.story.skinColorBase != null)
                specialColors["Base"] = pawn.story.skinColorBase.Value;
            listing.ColorPickerLabeled("Skin Color", _skinColorHeight, ref skinColor, specialColors, availableColors,
                c => TrySetSkinColor(c, ref p), out _skinColorHeight);
            TrySetSkinColor(skinColor, ref p);
            listing.End();
        });
    }

    // We use a reference, so when this method is used inside an action, it will still update the pawn.
    private static void TrySetSkinColor(Color color, ref Pawn pawn, bool silent = true)
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
