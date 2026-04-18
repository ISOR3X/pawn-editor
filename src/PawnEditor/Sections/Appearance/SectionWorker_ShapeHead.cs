using HotSwap;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Void;
using Void.Extensions;

namespace PawnEditor;

[UsedImplicitly]
[HotSwappable]
public class SectionWorker_ShapeHead(SectionDef def) : SectionWorker(def)
{
    private Vector2 _scrollPositionHeadType = Vector2.zero;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var capturedPawn = pawn;
        builder.Item(r =>
        {
            Void.Widgets.WidgetLabel(r.TakeTopPart(UIUtility.ButtonHeight), "Head");
            Widgets.Carrousel(r,
                DefDatabase<HeadTypeDef>.AllDefsListForReading
                    .Where(d => AppearanceUtility.CanUseHeadType(d, pawn)).ToList(),
                ref _scrollPositionHeadType,
                pawn.story.headType, d => AppearanceUtility.SetHeadType(d, capturedPawn),
                d => d.GetGraphic(capturedPawn, capturedPawn.story.SkinColor).MatSouth.mainTexture,
                pawn.story.SkinColor, d => d.ReadableDefName());
        }, new StyleOverride { height = Widgets.CarrouselCellHeight + UIUtility.ButtonHeight });
    }
}