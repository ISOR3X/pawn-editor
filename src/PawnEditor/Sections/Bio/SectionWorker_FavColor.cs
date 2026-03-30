using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_FavColor(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var currentColor = pawn.story.favoriteColor?.color ?? Color.white;
        builder.Text("Favorite color", style: new Style { margin = new(0, GenUI.GapLabel, 0, 0) });
        builder.Button("Choose color...",
            onClick: _ =>
            {
                Find.WindowStack.Add(new Dialog_ColorPicker(c => pawn.story.favoriteColor?.color = c, currentColor));
            });
        builder.Item(new Style { size = new Size<Dimension>(UIUtility.ButtonHeight, UIUtility.ButtonHeight) }, r =>
        {
            Verse.Widgets.DrawLightHighlight(r);
            Verse.Widgets.DrawRectFast(r.ContractedBy(GenUI.GapTiny), pawn.story.favoriteColor?.color ?? Color.white);
        });
    }

    private static void DoFavColorInputRect(Rect inRect, Pawn pawn)
    {
        inRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        var oldColor = pawn.story.favoriteColor?.color ?? Color.white;
        var favColorRect = inRect.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);

        Verse.Widgets.DrawLightHighlight(favColorRect);
        favColorRect = favColorRect.ContractedBy(2f);
        Verse.Widgets.DrawRectFast(favColorRect, pawn.story.favoriteColor?.color ?? Color.white);
        inRect.xMax -= 2f;
        if (Verse.Widgets.ButtonText(inRect, "Choose color"))
            Find.WindowStack.Add(new Dialog_ColorPicker(c => pawn.story.favoriteColor?.color = c, oldColor));
    }
}