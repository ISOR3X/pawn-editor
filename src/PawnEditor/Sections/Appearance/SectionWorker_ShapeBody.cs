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

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var capturedPawn = pawn;
        builder.Item(r =>
        {
            Widgets.WidgetLabel(r.TakeTopPart(UIUtility.ButtonHeight), "Body");
            Widgets.Carrousel(r,
                DefDatabase<BodyTypeDef>.AllDefsListForReading
                    .Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList(),
                ref _scrollPositionBodyType,
                pawn.story.bodyType, d => AppearanceUtility.TrySetBodyType(d, capturedPawn),
                d => AppearanceUtility.BodyTypes[d], pawn.story.SkinColor, d => d.ReadableDefName());
        }, new StyleOverride { height =  Widgets.CarrouselCellHeight + UIUtility.ButtonHeight});
    }
}
