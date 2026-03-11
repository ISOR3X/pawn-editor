using System.Collections.Generic;
using HotSwap;
using PawnEditor.Layout;
using Verse;
using L = PawnEditor.Layout.FlexLayoutHelper;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Appearance(SectionDef def) : SectionWorker(def)
{
    private float _sectionHeight = 100f;

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var layout = L.Col([
            L.Row([
                L.Cell(rect =>
                {
                    if (UIUtility.ButtonTextLabeled_WithIcon(rect, "Sex", pawn.gender.GetLabel().CapitalizeFirst(),
                            pawn.gender.GetIcon()))
                    {
                    }
                }, flexGrow: 1f, flexBasis: 0.3f),
                L.Cell(rect =>
                {
                    if (UIUtility.ButtonTextLabeled_WithIcon(rect, "Lifestage", pawn.DevelopmentalStage.ToString(),
                            pawn.DevelopmentalStage.Icon().Texture))
                    {
                    }
                }, flexGrow: 1f, flexBasis: 0.3f),
                L.Cell(rect =>
                {
                    if (UIUtility.ButtonTextLabeled_WithIcon(rect, "Xenotype", pawn.genes.XenotypeLabelCap,
                            pawn.genes.XenotypeIcon))
                    {
                    }
                }, flexGrow: 1f, flexBasis: 0.3f).When(ModsConfig.BiotechActive),
                L.Cell(rect =>
                {
                    if (UIUtility.ButtonTextLabeled(rect, "Race", pawn.kindDef.race.LabelCap))
                    {
                    }
                }, flexGrow: 1f, flexBasis: 0.3f).When(ModsConfig.BiotechActive && GetRacesForPawn(pawn).Any())
            ])
        ], 4f);

        _sectionHeight = FlexLayoutEngine.Draw(layout, listing.GetRect(_sectionHeight),
            (action, rect) =>
            {
                action(rect);
                return UIUtility.ButtonHeight;
            });
    }

    private static List<ThingDef> GetRacesForPawn(Pawn pawn)
    {
        return [];
    }
}