using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_AppearanceBasic : SectionWorker
{
    private readonly Listing_Horizontal listing = new();

    public SectionWorker_AppearanceBasic(SectionDef def) : base(def)
    {
        listing.Spacing = new Vector2(32f, 16f);
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        listing.Begin(inRect);
        listing.ButtonTextLabeled("Sex", pawn.gender.GetLabel().CapitalizeFirst(), 4);
        listing.ButtonTextLabeled("Lifestage", pawn.DevelopmentalStage.ToString(), 4);
        if (ModsConfig.BiotechActive)
        {
            listing.ButtonTextLabeled("Xenotype", pawn.genes.XenotypeLabelCap, 4);

            var races = GetRacesForPawn(pawn);
            if (!races.NullOrEmpty()) listing.ButtonTextLabeled("Race", pawn.kindDef.race.LabelCap, 4);
        }

        listing.End();
        inRect.TakeTopPart(listing.TotalHeight);
    }

    private static List<ThingDef> GetRacesForPawn(Pawn pawn)
    {
        return [];
    }
}