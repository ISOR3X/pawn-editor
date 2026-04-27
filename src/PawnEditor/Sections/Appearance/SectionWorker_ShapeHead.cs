using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.Extensions;
using Void.XMLComponents;

namespace PawnEditor;

[UsedImplicitly]
public class SectionWorker_ShapeHead(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = "Head";
        layout.ComponentById<DivElement>("carrousel").Children = b =>
        {
            var currentHead = pawn.story.headType;
            var heads = DefDatabase<HeadTypeDef>.AllDefsListForReading
                .Where(d => AppearanceUtility.CanUseHeadType(d, pawn)).ToList();
            b.HorizontalList(heads,
                (r, td) =>
                {
                    Verse.Widgets.DrawHighlight(r);
                    Verse.Widgets.DrawHighlightIfMouseover(r);
                    if (Equals(td, currentHead)) Verse.Widgets.DrawHighlightSelected(r);
                    
                    if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, td.ReadableDefName());

                    if (Verse.Widgets.ButtonInvisible(r)) AppearanceUtility.TrySetHeadType(td, pawn);
                    
                    using (new GUIColor(pawn.story.SkinColor))
                        Verse.Widgets.DrawTextureFitted(r, td.GetGraphic(pawn, pawn.story.SkinColor).MatSouth.mainTexture ,1.6f);
                }, 64f, style: new StyleOverride { gap = Void.Taffy.Gap(4f)});
        };
    }
}