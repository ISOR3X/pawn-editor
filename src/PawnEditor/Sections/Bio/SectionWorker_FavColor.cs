using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_FavColor(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
                DoFavColorInputRect(UIUtility.RectLabeled(r, "Favorite color"), pawn);
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
