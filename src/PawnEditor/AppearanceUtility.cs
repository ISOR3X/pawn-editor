using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static class AppearanceUtility
{
    public static readonly Dictionary<BodyTypeDef, Texture2D> BodyTypes;
    public static readonly Dictionary<Color, GeneDef> ColorsFromGenes;

    static AppearanceUtility()
    {
        BodyTypes = DefDatabase<BodyTypeDef>.AllDefs.ToDictionary(def => def,
            def => (Texture2D)GraphicDatabase
                .Get<Graphic_Multi>(def.bodyNakedGraphicPath, ShaderUtility.GetSkinShaderAbstract(true, false),
                    Vector2.one, Color.white)
                .MatSouth.mainTexture);

        ColorsFromGenes = PawnSkinColors.SkinColorGenesInOrder
            .Where(g => g.skinColorBase.HasValue || g.skinColorOverride.HasValue)
            .ToDictionary(g => g.skinColorBase ?? g.skinColorOverride!.Value, g => g);
    }

    public static bool CanUseHeadType(HeadTypeDef headTypeDef, Pawn pawn)
    {
        if (ModsConfig.BiotechActive && !headTypeDef.requiredGenes.NullOrEmpty())
        {
            if (pawn.genes == null) return false;

            if (headTypeDef.requiredGenes.Any(requiredGene => !pawn.genes.HasActiveGene(requiredGene))) return false;
        }

        if (headTypeDef.gender != 0) return headTypeDef.gender == pawn.gender;

        return headTypeDef.randomChosen;
    }

    public static bool CanUseBodyType(BodyTypeDef bodyTypeDef, Pawn pawn)
    {
        return AllBodyTypesFor(pawn).Contains(bodyTypeDef);
    }

    private static List<BodyTypeDef> AllBodyTypesFor(Pawn pawn)
    {
        // REF: PawnGenerator.GetBodyTypeFor(Pawn pawn)
        var bodyTypeDefList = new List<BodyTypeDef>();
        if (ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile())
            bodyTypeDefList.Add(pawn.DevelopmentalStage == DevelopmentalStage.Baby
                ? BodyTypeDefOf.Baby
                : BodyTypeDefOf.Child);
        if (ModsConfig.BiotechActive && pawn.genes != null)
        {
            List<Gene> genesListForReading = pawn.genes.GenesListForReading;
            bodyTypeDefList.AddRange(genesListForReading
                .Where(t => t.def.bodyType.HasValue)
                .Select(t => t.def.bodyType!.Value.ToBodyType(pawn)));
        }

        if (pawn.story.Adulthood != null) bodyTypeDefList.Add(pawn.story.Adulthood.BodyTypeFor(pawn.gender));

        bodyTypeDefList.Add(BodyTypeDefOf.Thin);
        bodyTypeDefList.Add(pawn.gender != Gender.Female ? BodyTypeDefOf.Male : BodyTypeDefOf.Female);
        return bodyTypeDefList;
    }

    public static void SetHeadType(HeadTypeDef headTypeDef, Pawn pawn)
    {
        if (pawn.story.headType == headTypeDef) return;
        pawn.story.headType = headTypeDef;
        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }

    public static void TrySetBodyType(BodyTypeDef bodyTypeDef, Pawn pawn)
    {
        if (pawn.story.bodyType == bodyTypeDef) return;
        pawn.story.bodyType = bodyTypeDef;
        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }


    public static void TrySetHairColor(Color color, Pawn pawn)
    {
        if (pawn.story.HairColor == color) return;
        pawn.story.HairColor = color;
        pawn.style.Notify_StyleItemChanged();
    }

    public static List<Color> GetSkinColorsFor(Pawn pawn)
    {
        var colorList = new List<Color>();
        if (pawn.genes.GenesListForReading.Any(g => g.def.skinIsHairColor))
            colorList.AddRange(GetHairColorsFor(pawn));
        else
            colorList.AddRange(ColorsFromGenes.Keys);

        return colorList;
    }

    public static List<Color> GetHairColorsFor(Pawn pawn)
    {
        var colorList = new List<Color>();

        foreach (var allDef in DefDatabase<ColorDef>.AllDefs)
        {
            var color = allDef.color;
            if (allDef.displayInStylingStationUI && !colorList.Any(x => x.WithinDiffThresholdFrom(color, 0.15f)))
                colorList.Add(color);
        }

        colorList.SortByColor(x => x);

        return colorList;
    }

    public static void TrySetBeardFor(BeardDef beardDef, Pawn pawn)
    {
        pawn.style.beardDef = beardDef;
        pawn.style.Notify_StyleItemChanged();
    }

    public static void TrySetHairFor(HairDef hairDef, Pawn pawn)
    {
        pawn.story.hairDef = hairDef;
        pawn.style.Notify_StyleItemChanged();
    }

    public static void TrySetTattooFor(TattooDef tattooDef, Pawn pawn)
    {
        if (tattooDef.tattooType == TattooType.Body)
            pawn.style.BodyTattoo = tattooDef;
        else
            pawn.style.FaceTattoo = tattooDef;
        pawn.style.Notify_StyleItemChanged();
    }
}