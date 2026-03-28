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

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        var capturedPawn = pawn;
        col.Item(height: Widgets.CarrouselCellHeight + UIUtility.ButtonHeight, draw: r =>
        {
            Widgets.WidgetLabel(r.TakeTopPart(UIUtility.ButtonHeight), "Body");
            Widgets.Carrousel(r,
                DefDatabase<BodyTypeDef>.AllDefsListForReading
                    .Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList(),
                ref _scrollPositionBodyType,
                pawn.story.bodyType, d => AppearanceUtility.TrySetBodyType(d, capturedPawn),
                d => AppearanceUtility.BodyTypes[d], pawn.story.SkinColor, d => d.ReadableDefName());
        });
    }
}
