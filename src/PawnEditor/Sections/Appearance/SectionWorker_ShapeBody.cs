using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[Reloadable]
public class SectionWorker_ShapeBody(SectionDef def) : SectionWorker(def)
{
    private Vector2 _scrollPositionBodyType = Vector2.zero;
    private float _skinColorHeight = 30f;

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var capturedPawn = pawn;

        var rect1 = listing.GetRect(Widgets.CarrouselCellHeight + UIUtility.ButtonHeight);
        Widgets.WidgetLabel(rect1.TakeTopPart(UIUtility.ButtonHeight), "Body");
        Widgets.Carrousel(rect1,
            DefDatabase<BodyTypeDef>.AllDefsListForReading
                .Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList(),
            ref _scrollPositionBodyType,
            pawn.story.bodyType, d => AppearanceUtility.TrySetBodyType(d, capturedPawn),
            d => AppearanceUtility.BodyTypes[d], pawn.story.SkinColor, d => d.defName);

        var skinColor = pawn.story.SkinColor;
        var availableColors = AppearanceUtility.GetSkinColorsFor(pawn);
        var specialColors = new Dictionary<string, Color> { { "Old", pawn.story.SkinColor } };
        if (pawn.story.favoriteColor != null) specialColors["Favorite"] = pawn.story.favoriteColor.color;
        if (pawn.story.SkinColorOverriden && pawn.story.skinColorBase != null)
            specialColors["Base"] = pawn.story.skinColorBase.Value;

        listing.ColorPickerLabeled("Skin Color", _skinColorHeight, ref skinColor, specialColors, availableColors,
            c => AppearanceUtility.TrySetSkinColor(c, ref capturedPawn), out _skinColorHeight);
        // AppearanceUtility.TrySetSkinColor(skinColor, ref pawn);
    }
}