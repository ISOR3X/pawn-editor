using System.Collections.Generic;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;
[Reloadable]
public class SectionWorker_AppearanceBasic(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var sexRect = listing.RectLabeled("Sex");
        Widgets.ButtonText(sexRect, pawn.gender.GetLabel().CapitalizeFirst());

        var lifestageRect = listing.RectLabeled("Lifestage");
        Widgets.ButtonText(lifestageRect, pawn.DevelopmentalStage.ToString());

        if (!ModsConfig.BiotechActive) return;

        var xenotypeRect = listing.RectLabeled("Xenotype");
        Widgets.ButtonText(xenotypeRect, pawn.genes.XenotypeLabelCap);

        var races = GetRacesForPawn(pawn);
        if (races.NullOrEmpty()) return;

        var raceRect = listing.RectLabeled("Race");
        Widgets.ButtonText(raceRect, pawn.kindDef.race.LabelCap);
    }

    private static List<ThingDef> GetRacesForPawn(Pawn pawn) => [];
}