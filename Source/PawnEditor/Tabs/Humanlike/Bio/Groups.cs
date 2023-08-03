using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    private void DoGroups(Rect inRect, Pawn pawn)
    {
        Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.Groups".Translate().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 6;

        var faction = "Faction".Translate();
        var ideo = "DifficultyIdeologySection".Translate();
        var certainty = "Certainty".Translate().CapitalizeFirst();
        var title = "PawnEditor.EmpireTitle".Translate();
        var honor = "PawnEditor.Honor".Translate();
        var leftWidth = UIUtility.ColumnWidth(3, faction, ideo, certainty, title, honor);
        if (pawn.Faction != null)
        {
            var factionRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(factionRect.TakeLeftPart(leftWidth), faction);
            if (Widgets.ButtonText(factionRect, "PawnEditor.SelectFaction".Translate()))
                Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsVisibleInViewOrder.Select(newFaction =>
                        new FloatMenuOption(newFaction.Name, delegate
                        {
                            pawn.SetFaction(newFaction);
                            PawnEditor.RecachePawnList();
                        }, newFaction.def.FactionIcon, newFaction.Color))
                   .ToList()));
            factionRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            factionRect.TakeLeftPart(leftWidth);
            Widgets.DrawHighlight(factionRect);
            Widgets.DrawHighlightIfMouseover(factionRect);
            GUI.color = pawn.Faction.Color;
            GUI.DrawTexture(factionRect.TakeLeftPart(30).ContractedBy(6), pawn.Faction.def.FactionIcon);
            GUI.color = Color.white;
            using (new TextBlock(TextAnchor.MiddleLeft))
                Widgets.Label(factionRect, pawn.Faction.Name);

            inRect.yMin += 4;
        }

        if (ModsConfig.IdeologyActive && pawn.Ideo != null)
        {
            var ideoRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(ideoRect.TakeLeftPart(leftWidth), ideo);
            if (Widgets.ButtonText(ideoRect, "PawnEditor.SelectIdeo".Translate()))
                Find.WindowStack.Add(new FloatMenu(Find.IdeoManager.IdeosInViewOrder.Select(newIdeo =>
                        new FloatMenuOption(newIdeo.name, delegate { pawn.ideo.SetIdeo(newIdeo); }, newIdeo.Icon, newIdeo.Color))
                   .ToList()));

            ideoRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            ideoRect.TakeLeftPart(leftWidth);
            Widgets.DrawHighlight(ideoRect);
            Widgets.DrawHighlightIfMouseover(ideoRect);
            GUI.color = pawn.Ideo.Color;
            GUI.DrawTexture(ideoRect.TakeLeftPart(30).ContractedBy(6), pawn.Ideo.Icon);
            GUI.color = Color.white;
            using (new TextBlock(TextAnchor.MiddleLeft))
                Widgets.Label(ideoRect, pawn.Ideo.name);

            var certaintyRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(certaintyRect.TakeLeftPart(leftWidth), certainty);
            pawn.ideo.certaintyInt = Widgets.HorizontalSlider_NewTemp(certaintyRect, pawn.ideo.certaintyInt, 0, 1, true, pawn.ideo.Certainty.ToStringPercent(),
                "0%", "100%");
        }

        var empire = Faction.OfEmpire;
        if (ModsConfig.RoyaltyActive && empire != null)
        {
            var titleRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(titleRect.TakeLeftPart(leftWidth), title);
            var curTitle = pawn.royalty.GetCurrentTitle(empire);
            if (Widgets.ButtonText(titleRect, curTitle?.GetLabelCapFor(pawn) ?? "None".Translate()))
            {
                var list = new List<FloatMenuOption>
                {
                    new("None".Translate(), () => { pawn.royalty.SetTitle(empire, null, false, false, false); })
                };
                list.AddRange(empire.def.RoyalTitlesAllInSeniorityOrderForReading.Select(royalTitle =>
                    new FloatMenuOption(royalTitle.GetLabelCapFor(pawn), () => pawn.royalty.SetTitle(empire, royalTitle, true, false, false))));
                Find.WindowStack.Add(new FloatMenu(list));
            }

            if (curTitle != null)
            {
                var honorRect = inRect.TakeTopPart(30);
                inRect.yMin += 4;
                using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(honorRect.TakeLeftPart(leftWidth), honor);
                float favor = pawn.royalty.GetFavor(empire);
                Widgets.HorizontalSlider(honorRect, ref favor, new FloatRange(0, curTitle.GetNextTitle(empire).favorCost - 1), favor.ToString());
                pawn.royalty.SetFavor(empire, (int)favor, false);
            }
        }

        inRect.xMin -= 2;
        Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.Extras".Translate().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 2;
        inRect.yMin += 4;

        var text = "PawnEditor.FavColor".Translate();
        var colorRect = inRect.TakeTopPart(30);
        inRect.yMin += 4;
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(colorRect.TakeLeftPart(Text.CalcSize(text).x), text);
        Widgets.DrawBoxSolid(colorRect.TakeRightPart(30).ContractedBy(2.5f), pawn.story.favoriteColor ?? Color.white);
        if (Widgets.ButtonText(colorRect, "PawnEditor.PickColor".Translate())) { }
    }
}
