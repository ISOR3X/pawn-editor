using JetBrains.Annotations;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;

namespace PawnEditor;

[UsedImplicitly]
public class SectionWorker_ShapeHead(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var currentHead = pawn.story.headType;
        var heads = DefDatabase<HeadTypeDef>.AllDefsListForReading
            .Where(d => AppearanceUtility.CanUseHeadType(d, pawn)).ToList();
        layout.ComponentById<TextElement>("text").Content = "Head";
        layout.ComponentById<DivElement>("carrousel").Children = b =>
        {
            b.HorizontalList(heads,
                (r, td) =>
                {
                    Verse.Widgets.DrawHighlight(r);
                    Verse.Widgets.DrawHighlightIfMouseover(r);
                    if (Equals(td, currentHead)) Verse.Widgets.DrawHighlightSelected(r);

                    if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, td.ReadableDefName());

                    if (Verse.Widgets.ButtonInvisible(r)) AppearanceUtility.TrySetHeadType(td, pawn);

                    using (new GUIColor(pawn.story.SkinColor))
                        Verse.Widgets.DrawTextureFitted(r,
                            td.GetGraphic(pawn, pawn.story.SkinColor).MatSouth.mainTexture, 1.6f);
                }, 64f, style: new StyleOverride { gap = Void.Taffy.Gap(4f) });
        };
        layout.ComponentById<ButtonElement>("next").OnClick = _ => StepBodyType();
        layout.ComponentById<ButtonElement>("prev").OnClick = _ => StepBodyType(-1);

        return;

        void StepBodyType(int step = 1)
        {
            var idx = heads.IndexOf(currentHead);
            var count = heads.Count;

            var newIndex = ((idx + step) % count + count) % count;

            AppearanceUtility.TrySetHeadType(heads[newIndex], pawn);
        }
    }
}