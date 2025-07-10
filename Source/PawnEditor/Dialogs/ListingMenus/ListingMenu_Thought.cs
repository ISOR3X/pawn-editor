using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
[HotSwappable]
public class ListingMenu_Thoughts : ListingMenu<ThoughtDef>
{
    private static readonly List<ThoughtDef> moodMemoryDefs;
    private static readonly List<ThoughtDef> opinionMemoryDefs;
    private static IntRange maxMoodOffset = IntRange.One;

    private static readonly HashSet<string> extraNeedsOtherPawn = new()
    {
        "GotMarried",
        "DefeatedHostileFactionLeader",
        "BondedAnimalBanished",
        "FailedToRescueRelative",
        "RescuedRelative",
        "FailedRomanceAttemptOnMeLowOpinionMood",
        "Counselled",
        "Counselled_MoodBoost",
        "FailedConvertAbilityInitiator",
        "FailedConvertAbilityRecipient"
    };

    private static readonly Regex tagRe = new("{[a-zA-Z_]+}", RegexOptions.Compiled | RegexOptions.ECMAScript);

    static ListingMenu_Thoughts()
    {
        moodMemoryDefs = new();
        opinionMemoryDefs = new();

        foreach (var thoughtDef in DefDatabase<ThoughtDef>.AllDefsListForReading)
        {
            if (thoughtDef.IsMemory)
            {
                if (thoughtDef.IsSocial)
                    opinionMemoryDefs.Add(thoughtDef);
                else moodMemoryDefs.Add(thoughtDef);

                foreach (var thoughtStage in thoughtDef.stages)
                {
                    if (thoughtStage == null) continue;
                    var moodOffset = Mathf.FloorToInt(thoughtDef.IsSocial ? thoughtStage.baseOpinionOffset : thoughtStage.baseMoodEffect);
                    if (moodOffset > maxMoodOffset.max) maxMoodOffset.max = moodOffset;
                    if (moodOffset < maxMoodOffset.min) maxMoodOffset.min = moodOffset;
                }
            }

            if (thoughtDef.thoughtToMake != null) extraNeedsOtherPawn.Add(thoughtDef.thoughtToMake.defName);
        }
    }

    public ListingMenu_Thoughts(Pawn pawn, UITable<Pawn> table) : base(moodMemoryDefs, def => GetLabel(def, pawn), def => TryAdd(pawn, def, table),
        "PawnEditor.Choose".Translate() + " " + "PawnEditor.Thought".Translate(), def => def.description, null, GetFilters(), pawn) { }

    public ListingMenu_Thoughts(Pawn pawn, Pawn otherPawn, UITable<Pawn> table) : base(opinionMemoryDefs, def => GetLabel(def, pawn, otherPawn),
        def => TryAdd(pawn, def, table, otherPawn),
        "PawnEditor.Choose".Translate() + " " + "PawnEditor.Thought".Translate(), def => def.description, null, GetFilters(), pawn) { }

    protected override string NextLabel => RequiresOtherPawn(Listing.Selected) ? "Next".Translate() : base.NextLabel;

    private static bool RequiresOtherPawn(ThoughtDef def) => def.stackLimitForSameOtherPawn >= 0 || extraNeedsOtherPawn.Contains(def.defName);

    private static AddResult TryAdd(Pawn pawn, ThoughtDef def, UITable<Pawn> table, Pawn otherPawn = null)
    {
        if (!ThoughtUtility.CanGetThought(pawn, def))
            return "PawnEditor.CannotGetThought".Translate(pawn.NameShortColored, def.LabelCap);

        if (RequiresOtherPawn(def) && otherPawn == null)
        {
            PawnEditor.AllPawns.UpdateCache(null, PawnCategory.Humans);
            var list = PawnEditor.AllPawns.GetList();
            list.Remove(pawn);
            Find.WindowStack.Add(new ListingMenu_Pawns(list, pawn, "Add".Translate().CapitalizeFirst(), newOtherPawn => TryAdd(pawn, def, table, newOtherPawn),
                "Back".Translate(),
                () => Find.WindowStack.Add(new ListingMenu_Thoughts(pawn, table))));
            return true;
        }

        AddResult result = new SuccessInfo(() => Add(pawn, def, table, otherPawn));
        result = new ConfirmInfo("PawnEditor.ThoughtDuplicate".Translate(), "ThoughtDuplicate", result,
            pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(def) != null, null, true);
        result = new ConfirmInfo(ThoughtUtility.ThoughtNullifiedMessage(pawn, def) + ". " + "PawnEditor.AddThoughNullified".Translate(def.LabelCap),
            "ThoughtNullified", result, ThoughtUtility.ThoughtNullified(pawn, def));
        return result;
    }

    private static void Add(Pawn pawn, ThoughtDef def, UITable<Pawn> table, Pawn otherPawn = null)
    {
        if (!def.IsMemory || (RequiresOtherPawn(def) && otherPawn == null)) return;
        var thought = (Thought_Memory)ThoughtMaker.MakeThought(def);
        if (thought is Thought_MemoryRoyalTitle royalTitle)
            royalTitle.titleDef = DefDatabase<RoyalTitleDef>.AllDefsListForReading.RandomElement();
        else if (thought is Thought_KilledInnocentAnimal innocentAnimal)
            innocentAnimal.SetAnimal(pawn.MapHeld.mapPawns.SpawnedColonyAnimals.RandomElement());
        else if (thought is Thought_MemoryObservationTerror terror)
        {
            terror.Target = pawn.MapHeld.listerThings.AllThings.RandomElement();
            terror.intensity = Rand.Int;
        }
        else if (thought is Thought_PsychicHarmonizer harmonizer)
        {
            harmonizer.harmonizer = (otherPawn ?? pawn.MapHeld.mapPawns.AllPawns.RandomElement()).health.hediffSet.hediffs.RandomElement();
            otherPawn = null;
        }
        else if (thought is Thought_RelicAtRitual relic)
        {
            if (DefDatabase<ThingDef>.AllDefs
               .Where(thingDef => thingDef.relicChance != 0f && thingDef.GetCompProperties<CompProperties_GeneratedName>() != null)
               .TryRandomElement(out var relicDef))
                relic.relicName = CompGeneratedNames.GenerateName(relicDef.GetCompProperties<CompProperties_GeneratedName>());
        }
        else if (thought is Thought_TameVeneratedAnimalDied animalDied)
        {
            var kindDef = DefDatabase<PawnKindDef>.AllDefs.Where(kind => kind.RaceProps.Animal).RandomElement();
            animalDied.animalKindLabel = GenLabel.BestKindLabel(kindDef, kindDef.fixedGender ?? (Rand.Bool ? Gender.Female : Gender.Male));
        }
        else if (thought is Thought_WeaponTrait weaponTrait) weaponTrait.weapon = pawn.equipment.Primary;

        pawn.needs.mood.thoughts.memories.TryGainMemory(thought, otherPawn);

        table.ClearCache();
    }

    private static string GetLabel(ThoughtDef def, Pawn pawn, Pawn otherPawn = null)
    {
        var label = def.defName;
        if (!def.label.NullOrEmpty()) label = def.label;

        if (def.stages.FirstOrDefault(ts => ts != null) is { } stage)
        {
            if (!stage.label.NullOrEmpty()) label = stage.label;

            if (!stage.labelSocial.NullOrEmpty()) label = stage.labelSocial;
        }

//        Log.Message($"{tagRe} matches {label}: {tagRe.IsMatch(label)}");

        if (tagRe.IsMatch(label))
            label = tagRe.Replace(label, match => match.Value.Colorize(ColoredText.SubtleGrayColor));
        else if (RequiresOtherPawn(def))
            label = label.Formatted(otherPawn?.LabelShort ?? "", otherPawn);
        else
            label = label.Formatted(pawn.Named("PAWN"));

        return label.CapitalizeFirst();
    }

    private static List<Filter<ThoughtDef>> GetFilters() =>
        new()
        {
            new Filter_ModSource<ThoughtDef>(),
            new Filter_IntRange<ThoughtDef>("PawnEditor.MoodOffset".Translate(), maxMoodOffset, def =>
            {
                var range = IntRange.Zero;

                foreach (var thoughtStage in def.stages)
                {
                    var moodOffset = Mathf.FloorToInt(def.IsSocial ? thoughtStage.baseOpinionOffset : thoughtStage.baseMoodEffect);
                    if (moodOffset > maxMoodOffset.max) maxMoodOffset.max = moodOffset;
                    if (moodOffset < maxMoodOffset.min) maxMoodOffset.min = moodOffset;
                }

                return range;
            })
        };
}
