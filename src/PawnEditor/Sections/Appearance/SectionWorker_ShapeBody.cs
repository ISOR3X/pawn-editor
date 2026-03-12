using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_ShapeBody(SectionDef def) : SectionWorker(def)
{
    private Vector2 _scrollPositionBodyType = Vector2.zero;

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
            d => AppearanceUtility.BodyTypes[d], pawn.story.SkinColor, d => d.ReadableDefName());
    }
}