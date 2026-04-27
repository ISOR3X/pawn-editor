using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.Extensions;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_ShapeBody(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<TextElement>("text").Content = "Body";
        layout.ComponentById<DivElement>("carrousel").Children = b =>
        {
            var currentHead = pawn.story.bodyType;
            var heads = DefDatabase<BodyTypeDef>.AllDefsListForReading
                .Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList();
            b.HorizontalList(heads,
                (r, td) =>
                {
                    Verse.Widgets.DrawHighlight(r);
                    Verse.Widgets.DrawHighlightIfMouseover(r);
                    if (Equals(td, currentHead)) Verse.Widgets.DrawHighlightSelected(r);

                    if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, td.ReadableDefName());

                    if (Verse.Widgets.ButtonInvisible(r)) AppearanceUtility.TrySetBodyType(td, pawn);
                    
                    // -10f to compensate for the off-centered body textures.
                    using (new GUIColor(pawn.story.SkinColor))
                        Verse.Widgets.DrawTextureFitted(r with {y = r.y - 8f}, AppearanceUtility.BodyTypes[td], 1.6f);
                }, 64f, style: new StyleOverride { gap = Void.Taffy.Gap(4f) });
        };
    }
}