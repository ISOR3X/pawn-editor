using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class TabWorker_Health : TabWorker<Pawn>
{
    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);
        DoCapacities(headerRect, pawn);
    }

    private void DoCapacities(Rect inRect, Pawn pawn)
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

                GUI.color = Color.white;
                Widgets.Label(new Rect(rect.x, rect.y, rect.width * 0.65f, 30f),
                    pawnCapacityDef.GetLabelFor(pawn.RaceProps.IsFlesh, pawn.RaceProps.Humanlike).CapitalizeFirst());
                GUI.color = efficiencyLabel.Second;
                Widgets.Label(new Rect(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.35f, 30f), efficiencyLabel.First);
                if (Mouse.IsOver(rect))
                    TooltipHandler.TipRegion(rect, () => pawn.Dead ? "" : HealthCardUtility.GetPawnCapacityTip(pawn, activityLocal),
                        pawn.thingIDNumber ^ pawnCapacityDef.index);
            }

        listing.End();
    }
}
