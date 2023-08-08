using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Social : TabWorker<Pawn>
{
    private Vector2 scrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);
        DoRelations(rect, pawn);
    }

    private void DoRelations(Rect inRect, Pawn pawn)
    {
        var relations = pawn.relations.DirectRelations.ToList();
        var viewRect = new Rect(0, 0, inRect.width - 20, relations.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        const float nameWidth = 220;
        const float opinionWidth = 140;
        var headerRect = viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium));
        using (new TextBlock(GameFont.Medium)) Widgets.Label(headerRect.TakeLeftPart(nameWidth + 42), "Health".Translate());
        headerRect.xMax -= 134;
        using (new TextBlock(TextAnchor.MiddleCenter))
            Widgets.Label(headerRect.TakeRightPart(opinionWidth), "PawnEditor.Opinion".Translate());
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(headerRect, "PawnEditor.HediffType".Translate());

        for (var i = 0; i < relations.Count; i++)
        {
            var rect = viewRect.TakeTopPart(30);
            rect.xMin += 10;
            var fullRect = new Rect(rect);
            if (i % 2 == 1) Widgets.DrawLightHighlight(fullRect);
            var relation = relations[i];
            if (Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(2.5f), TexButton.DeleteX)) pawn.relations.RemoveDirectRelation(relation);
            rect.xMax -= 4;
            if (Widgets.ButtonText(rect.TakeRightPart(100), "Edit".Translate() + "...")) { }

            rect.xMin += 2;
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(fullRect);
                TooltipHandler.TipRegion(fullRect, null);
            }

            var opinionRect = rect.TakeRightPart(opinionWidth).ContractedBy(10, 0);
            var opinionOf = relation.otherPawn.relations.OpinionOf(pawn);
            var opinionFrom = pawn.relations.OpinionOf(relation.otherPawn);
            using (new TextBlock(TextAnchor.MiddleLeft))
                Widgets.Label(opinionRect,
                    opinionOf.ToStringWithSign().Colorize(opinionOf < 0 ? ColorLibrary.RedReadable : opinionOf > 0 ? ColorLibrary.Green : Color.white));
            using (new TextBlock(TextAnchor.MiddleRight))
                Widgets.Label(opinionRect,
                    $"({opinionFrom.ToStringWithSign()})".Colorize((opinionOf < 0 ? ColorLibrary.RedReadable :
                        opinionOf > 0 ? ColorLibrary.Green : Color.white).SaturationChanged(-0.25f)));

            GUI.DrawTexture(rect.TakeLeftPart(30).ContractedBy(10f), BaseContent.GreyTex);
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(rect.TakeLeftPart(nameWidth), relation.otherPawn.Name.ToStringShort);
                Widgets.Label(rect, relation.def.GetGenderSpecificLabelCap(relation.otherPawn).Colorize(ColoredText.SubtleGrayColor));
            }
        }

        Widgets.EndScrollView();
    }
}
