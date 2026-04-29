using RimWorld;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;

namespace PawnEditor;

public class SectionWorker_ShapeBody(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var currentBody = pawn.story.bodyType;
        var bodies = DefDatabase<BodyTypeDef>.AllDefsListForReading
            .Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList();
        layout.ComponentById<TextElement>("text").Content = "Body";
        layout.ComponentById<DivElement>("carrousel").Children = b =>
        {
            b.HorizontalList(bodies,
                (r, td) =>
                {
                    Verse.Widgets.DrawHighlight(r);
                    Verse.Widgets.DrawHighlightIfMouseover(r);
                    if (Equals(td, currentBody)) Verse.Widgets.DrawHighlightSelected(r);

                    if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, td.ReadableDefName());

                    if (Verse.Widgets.ButtonInvisible(r)) AppearanceUtility.TrySetBodyType(td, pawn);

                    // -10f to compensate for the off-centered body textures.
                    using (new GUIColor(pawn.story.SkinColor))
                    {
                        Verse.Widgets.DrawTextureFitted(r with { y = r.y - 8f }, AppearanceUtility.BodyTypes[td], 1.6f);
                    }
                }, 64f, style: new StyleOverride { gap = Void.Taffy.Gap(4f) });
        };
        layout.ComponentById<ButtonElement>("next").OnClick = _ => StepBodyType();
        layout.ComponentById<ButtonElement>("prev").OnClick = _ => StepBodyType(-1);

        return;

        void StepBodyType(int step = 1)
        {
            var idx = bodies.IndexOf(currentBody);
            var count = bodies.Count;

            var newIndex = ((idx + step) % count + count) % count;

            AppearanceUtility.TrySetBodyType(bodies[newIndex], pawn);
        }
    }
}