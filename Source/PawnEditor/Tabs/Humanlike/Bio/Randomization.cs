using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn)
    {
        yield return new("PawnEditor.All".Translate(), () => RandomizeAll(pawn));
        yield return new("Appearance".Translate(), () => RandomizeAppearance(pawn));
        // yield return new("PawnEditor.Shape".Translate(), () => RandomizeShape(pawn));
        yield return new("Relations".Translate(), () => RandomizeRelations(pawn));
        yield return new("Traits".Translate(), () => RandomizeTraits(pawn));
        yield return new("Skills".Translate(), () => RandomizeSkills(pawn));
        yield return new("Backstory".Translate(), () => RandomizeBackstory(pawn));
    }

    public static void RandomizeAll(Pawn pawn)
    {
        if (!PawnEditor.Pregame)
        {
            // Delete
            var oldPawn = pawn;
            var position = oldPawn.Position;
            var map = oldPawn.Map;
            PawnEditor.PawnList.OnDelete(oldPawn);
            // Replace
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawn.kindDef, PawnEditor.selectedFaction));
            PawnEditor.AddPawn(pawn, PawnEditor.selectedCategory).HandleResult();
            if (!PawnEditor.Pregame && map != null)
            {
                GenSpawn.Spawn(pawn, position, map);
                PawnEditor.PawnList.UpdateCache(PawnEditor.selectedFaction, PawnEditor.selectedCategory);
            }

            TabWorker_FactionOverview.RecachePawns(PawnEditor.selectedFaction);
        }
        else
        {
            var index = StartingPawnUtility.PawnIndex(pawn);
            StartingPawnUtility.RandomizePawn(index);
        }
    }

    public static void RandomizeBackstory(Pawn pawn)
    {
        if (pawn.story.adulthood != null) PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Adulthood, PawnBioAndNameGenerator.GetBackstoryCategoryFiltersFor(pawn, pawn.Faction.def), pawn.Faction.def);
        if (pawn.story.childhood != null) PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Childhood, PawnBioAndNameGenerator.GetBackstoryCategoryFiltersFor(pawn, pawn.Faction.def), pawn.Faction.def);
    }

    public static void RandomizeAppearance(Pawn pawn)
    {
        pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
        if (ModLister.IdeologyInstalled)
        {
            pawn.style.FaceTattoo = PawnStyleItemChooser.RandomTattooFor(pawn, TattooType.Face);
            pawn.style.BodyTattoo = PawnStyleItemChooser.RandomTattooFor(pawn, TattooType.Body);
            pawn.style.beardDef = PawnStyleItemChooser.RandomBeardFor(pawn);
        }

        if (pawn.genes.GetMelaninGene() is { } geneDef1 && pawn.genes.GetGene(geneDef1) is { } gene1) pawn.genes.RemoveGene(gene1);
        var geneDef4 = PawnSkinColors.RandomSkinColorGene(pawn);
        if (geneDef4 != null) pawn.genes.AddGene(geneDef4, false);
        if (pawn.genes.GetHairColorGene() is { } geneDef2 && pawn.genes.GetGene(geneDef2) is { } gene2) pawn.genes.RemoveGene(gene2);
        var geneDef5 = PawnHairColors.RandomHairColorGene(pawn.story.SkinColorBase);
        if (geneDef5 != null) pawn.genes.AddGene(geneDef5, false);
        else
        {
            pawn.story.HairColor = PawnHairColors.RandomHairColor(pawn, pawn.story.SkinColorBase, pawn.ageTracker.AgeBiologicalYears);
            Log.Error("No hair color gene for " + pawn.LabelShort + ". Getting random color as fallback.");
        }

        RandomizeShape(pawn);
    }

    public static void RandomizeShape(Pawn pawn)
    {
        var headTypes = DefDatabase<HeadTypeDef>.AllDefs.Where(h => h.gender != pawn.gender.Opposite() && h.randomChosen);
        if (HARCompat.Active)
        {
            headTypes = HARCompat.FilterHeadTypes(headTypes, pawn);
            // HAR doesn't like head types not matching genders
            headTypes = headTypes.Where(type => type.gender == Gender.None || type.gender == pawn.gender);
        }

        pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);
        pawn.story.headType = headTypes.RandomElement();
        pawn.gender = Rand.Bool ? Gender.Male : Gender.Female;

        pawn.drawer.renderer.SetAllGraphicsDirty();
        PortraitsCache.SetDirty(pawn);
    }

    private static void RandomizeTraits(Pawn pawn)
    {
        var traitRequirements = (pawn.kindDef.forcedTraits ?? Enumerable.Empty<TraitRequirement>()).Concat(pawn.story.AllBackstories.SelectMany(
                backstory => backstory.forcedTraits ?? Enumerable.Empty<BackstoryTrait>(),
                (_, backstoryTrait) => new TraitRequirement { def = backstoryTrait.def, degree = backstoryTrait.degree }))
            .ToList();
        var forcedTraits = pawn.story.traits.allTraits
            .Where(trait => trait.sourceGene != null || traitRequirements.Any(req => req.def == trait.def && req.degree == trait.degree))
            .ToHashSet();
        foreach (var trait in pawn.story.traits.allTraits.Except(forcedTraits).ToList()) pawn.story.traits.RemoveTrait(trait, true);
        var num = Mathf.Min(GrowthUtility.GrowthMomentAges.Length, PawnGenerator.TraitsCountRange.RandomInRange);
        var ageBiologicalYears = pawn.ageTracker.AgeBiologicalYears;
        var num2 = 3;
        while (num2 <= ageBiologicalYears && pawn.story.traits.allTraits.Count < num)
        {
            if (GrowthUtility.IsGrowthBirthday(num2))
            {
                var trait = PawnGenerator.GenerateTraitsFor(pawn, 1, null, true).FirstOrFallback();
                if (trait != null) pawn.story.traits.GainTrait(trait);
            }

            num2++;
        }

        if (PawnGenerator.HasSexualityTrait(pawn)) return;

        if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn)
            || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
            pawn.story.traits.GainTrait(new(TraitDefOf.Gay, PawnGenerator.RandomTraitDegree(TraitDefOf.Gay)));

        if (!ModsConfig.BiotechActive || pawn.ageTracker.AgeBiologicalYears >= 13) PawnGenerator.TryGenerateSexualityTraitFor(pawn, true);
    }

    private static void RandomizeRelations(Pawn pawn)
    {
        var request = new PawnGenerationRequest(pawn.kindDef, pawn.Faction);
        pawn.relations.ClearAllRelations();
        PawnGenerator.GeneratePawnRelations(pawn, ref request);
    }

    private static void RandomizeSkills(Pawn pawn)
    {
        foreach (var skillRecord in pawn.skills.skills) skillRecord.passion = Passion.None;
        PawnGenerator.GenerateSkills(pawn, new(pawn.kindDef, pawn.Faction));
    }
}