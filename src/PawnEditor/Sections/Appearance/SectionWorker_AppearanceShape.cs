using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[HotSwappable]
public class SectionWorker_AppearanceShape : SectionWorker
{
    readonly Listing_Horizontal listing = new Listing_Horizontal();

    Vector2 scrollPositionBodyType = Vector2.zero;
    Vector2 scrollPositionHeadType = Vector2.zero;

    private bool disableRestrictions; // TODO: Implement restrictions (e.g. for body types)

    private float skinColorHeight = 30f; // Tracks the height of the rows for the skin colors.

    public SectionWorker_AppearanceShape(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        listing.Begin(inRect);
        // Body type
        var rect1 = listing.GetRect(6, UIComponents.CarrouselCellHeight + UIUtility.ButtonHeight);
        UIComponents.WidgetLabel(rect1.TakeTopPart(UIUtility.ButtonHeight), "Body");
        UIComponents.Carrousel(rect1, DefDatabase<BodyTypeDef>.AllDefsListForReading.Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList(), ref scrollPositionBodyType,
            pawn.story.bodyType, d => AppearanceUtility.TrySetBodyType(d, pawn), d => AppearanceUtility.BodyTypes[d], pawn.story.SkinColor, d => d.defName);

        // Head type
        var rect2 = listing.GetRect(6, UIComponents.CarrouselCellHeight + UIUtility.ButtonHeight);
        UIComponents.WidgetLabel(rect2.TakeTopPart(UIUtility.ButtonHeight), "Head");
        UIComponents.Carrousel(rect2, DefDatabase<HeadTypeDef>.AllDefsListForReading.Where(d => AppearanceUtility.CanUseHeadType(d, pawn)).ToList(), ref scrollPositionHeadType,
            pawn.story.headType, d => AppearanceUtility.SetHeadType(d, pawn), d => d.GetGraphic(pawn, pawn.story.SkinColor).MatSouth.mainTexture, pawn.story.SkinColor,
            d => d.defName);

        Color skinColor = pawn.story.SkinColor;
        var availableColors = AppearanceUtility.GetSkinColorsFor(pawn);
        var oldColor = pawn.story.SkinColor;
        var specialColors = new Dictionary<string, Color>
        {
            { "Old", oldColor },
        };
        if (pawn.story.favoriteColor != null) specialColors["Favorite"] = pawn.story.favoriteColor.color;
        if (pawn.story.SkinColorOverriden && pawn.story.skinColorBase.HasValue) specialColors["Base"] = pawn.story.skinColorBase.Value;

        listing.ColorPickerLabeled("Skin Color", skinColorHeight, ref skinColor, specialColors, availableColors, c => AppearanceUtility.TrySetSkinColor(c, ref pawn),
            out skinColorHeight);


        AppearanceUtility.TrySetSkinColor(skinColor, ref pawn);

        listing.End();
        inRect.TakeTopPart(listing.totalHeight);
    }
}