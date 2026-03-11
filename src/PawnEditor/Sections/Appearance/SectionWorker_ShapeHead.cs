using System.Linq;
using JetBrains.Annotations;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
[Reloadable]
public class SectionWorker_ShapeHead(SectionDef def) : SectionWorker(def)
{
    private Vector2 _scrollPositionHeadType = Vector2.zero;

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var capturedPawn = pawn;

        var rect2 = listing.GetRect(Widgets.CarrouselCellHeight + UIUtility.ButtonHeight);
        Widgets.WidgetLabel(rect2.TakeTopPart(UIUtility.ButtonHeight), "Head");
        Widgets.Carrousel(rect2,
            DefDatabase<HeadTypeDef>.AllDefsListForReading
                .Where(d => AppearanceUtility.CanUseHeadType(d, pawn)).ToList(),
            ref _scrollPositionHeadType,
            pawn.story.headType, d => AppearanceUtility.SetHeadType(d, capturedPawn),
            d => d.GetGraphic(capturedPawn, capturedPawn.story.SkinColor).MatSouth.mainTexture,
            pawn.story.SkinColor, d => d.defName);
    }
}