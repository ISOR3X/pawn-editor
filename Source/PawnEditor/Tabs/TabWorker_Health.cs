using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Health : TabWorker<Pawn>
{
    private Vector2 scrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);
        headerRect.xMin += 10;
        DoCapacities(headerRect, pawn);
        DoBottomOptions(rect.TakeBottomPart(40), pawn);
        DoHediffs(rect, pawn);
    }

    private static void DoBottomOptions(Rect inRect, Pawn pawn)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5), "PawnEditor.AddHediff".Translate())) { }

        if (Widgets.ButtonText(inRect.TakeLeftPart(100).ContractedBy(5), "PawnEditor.TendAll".Translate()))
        {
            var i = 0;
            foreach (var hediff in pawn.health.hediffSet.GetHediffsTendable()) hediff.Tended(1, 1, ++i);
        }

        if (Widgets.ButtonText(inRect.TakeLeftPart(250).ContractedBy(5), "PawnEditor.RemoveNegative.Hediffs".Translate()))
        {
            var bad = pawn.health.hediffSet.hediffs.Where(hediff => hediff.def.isBad).ToList();
            foreach (var hediff in bad) pawn.health.RemoveHediff(hediff);
        }

        Widgets.CheckboxLabeled(inRect.ContractedBy(5), "PawnEditor.ShowHidden.Hediffs".Translate(), ref HealthCardUtility.showAllHediffs,
            placeCheckboxNearText: true);
    }

    private void DoHediffs(Rect inRect, Pawn pawn)
    {
        var hediffs = HealthCardUtility.VisibleHediffs(pawn, true).OrderByDescending(hediff => HealthCardUtility.GetListPriority(hediff.Part)).ToList();
        var viewRect = new Rect(0, 0, inRect.width - 20, hediffs.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        const int partWidth = 220;
        var headerRect = viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium));
        using (new TextBlock(GameFont.Medium)) Widgets.Label(headerRect.TakeLeftPart(partWidth + 42), "Health".Translate());
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(headerRect, "PawnEditor.HediffType".Translate());
        for (var i = 0; i < hediffs.Count; i++)
        {
            var rect = viewRect.TakeTopPart(30);
            rect.xMin += 10;
            var fullRect = new Rect(rect);
            if (i % 2 == 1) Widgets.DrawLightHighlight(fullRect);
            var hediff = hediffs[i];
            if (Widgets.ButtonImage(rect.TakeRightPart(30).ContractedBy(2.5f), TexButton.DeleteX)) pawn.health.RemoveHediff(hediff);
            rect.xMax -= 4;
            if (Widgets.ButtonText(rect.TakeRightPart(100), "Edit".Translate() + "...")) { }

            rect.xMin += 2;
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(fullRect);
                TooltipHandler.TipRegion(fullRect, hediff.GetTooltip(pawn, HealthCardUtility.showHediffsDebugInfo));
            }

            GUI.DrawTexture(rect.TakeLeftPart(30).ContractedBy(10f), BaseContent.GreyTex);
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                if (hediff.Part != null)
                    Widgets.Label(rect.TakeLeftPart(partWidth), hediff.Part.LabelCap.Colorize(
                        HealthUtility.GetPartConditionLabel(pawn, hediff.Part).Second));
                else
                    Widgets.Label(rect.TakeLeftPart(partWidth), "WholeBody".Translate().Colorize(HealthUtility.RedColor));

                Widgets.Label(rect, hediff.LabelCap.Colorize(hediff.LabelColor));
            }
        }

        Widgets.EndScrollView();
    }

    private static void DoCapacities(Rect inRect, Pawn pawn)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ColumnWidth /= 2;
        IEnumerable<PawnCapacityDef> source;
        if (pawn.def.race.Humanlike)
            source = from x in DefDatabase<PawnCapacityDef>.AllDefs
                where x.showOnHumanlikes
                select x;
        else if (pawn.def.race.Animal)
            source = from x in DefDatabase<PawnCapacityDef>.AllDefs
                where x.showOnAnimals
                select x;
        else
            source = from x in DefDatabase<PawnCapacityDef>.AllDefs
                where x.showOnMechanoids
                select x;

        foreach (var pawnCapacityDef in from act in source
                 orderby act.listOrder
                 select act)
            if (PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, pawnCapacityDef))
            {
                var activityLocal = pawnCapacityDef;
                var efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(pawn, pawnCapacityDef);
                var rect = listing.GetRect(20);
                if (Mouse.IsOver(rect))
                {
                    GUI.color = HealthCardUtility.HighlightColor;
                    GUI.DrawTexture(rect, TexUI.HighlightTex);
                }

                Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.65f, 30f),
                    pawnCapacityDef.GetLabelFor(pawn.RaceProps.IsFlesh, pawn.RaceProps.Humanlike).CapitalizeFirst());
                Widgets.Label(new Rect(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.35f, 30f), efficiencyLabel.First.Colorize(efficiencyLabel.Second));
                if (Mouse.IsOver(rect))
                    TooltipHandler.TipRegion(rect, () => pawn.Dead ? "" : HealthCardUtility.GetPawnCapacityTip(pawn, activityLocal),
                        pawn.thingIDNumber ^ pawnCapacityDef.index);
            }

        listing.End();
    }
}
