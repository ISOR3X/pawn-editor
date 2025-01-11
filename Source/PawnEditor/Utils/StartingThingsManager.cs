using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class StartingThingsManager
{
    private static readonly List<Pawn> startingAnimals = new();
    private static readonly List<Pawn> startingMechs = new();
    private static readonly List<Thing> startingThings = new();
    private static readonly List<Thing> startingThingsNear = new();
    private static readonly List<Thing> startingThingsFar = new();
    private static readonly List<ScenPart> removedParts = new();
    private static Scenario cachedScenario;

    /// <summary>
    /// Load all items from the cached scenario into the static starting things lists, Starting, Near, and Far items, animals, and mechs.
    /// </summary>
    public static void ProcessScenario()
    {
        startingAnimals.Clear();
        startingMechs.Clear();
        startingThings.Clear();
        startingThingsNear.Clear();
        startingThingsFar.Clear();
        removedParts.Clear();

        foreach (var part in Find.Scenario.AllParts)
            switch (part)
            {
                case ScenPart_StartingAnimal:
                    startingAnimals.AddRange(part.PlayerStartingThings().OfType<Pawn>());
                    Find.Scenario.RemovePart(part);
                    removedParts.Add(part);
                    break;
                case ScenPart_StartingMech:

                    startingMechs.AddRange(part.PlayerStartingThings().OfType<Pawn>());
                    Find.Scenario.RemovePart(part);
                    removedParts.Add(part);
                    break;
                case ScenPart_StartingThing_Defined:
                    
                    startingThings.AddRange(part.PlayerStartingThings());
                    Find.Scenario.RemovePart(part);
                    removedParts.Add(part);
                    break;
                case ScenPart_ScatterThingsNearPlayerStart near:
                    {
                        var thing = ThingMaker.MakeThing(near.thingDef, near.stuff);
                        thing.stackCount = near.count;
                        startingThingsNear.Add(thing);
                        Find.Scenario.RemovePart(part);
                        removedParts.Add(part);
                        break;
                    }
                case ScenPart_ScatterThingsAnywhere far:
                    {
                        var thing = ThingMaker.MakeThing(far.thingDef, far.stuff);
                        thing.stackCount = far.count;
                        startingThingsFar.Add(thing);
                        Find.Scenario.RemovePart(part);
                        removedParts.Add(part);
                        break;
                    }
            }

        Find.Scenario.parts.Add(new ScenPart_StartingThingsFromPawnEditor()
        {
            def = new()
            {
                defName = "PawnEditor_Hook",
                category = ScenPartCategory.StartingImportant,
                label = "starting everything",
                scenPartClass = typeof(ScenPart_StartingThingsFromPawnEditor),
                generated = true
            }
        });
        cachedScenario = Find.Scenario;
    }

    public static void RestoreScenario()
    {
        if (cachedScenario != null)
        {
            startingAnimals.Clear();
            startingMechs.Clear();
            startingThings.Clear();
            startingThingsNear.Clear();
            startingThingsFar.Clear();

            cachedScenario.parts.RemoveAll(part => part is ScenPart_StartingThingsFromPawnEditor);

            cachedScenario.parts.AddRange(removedParts);

            removedParts.Clear();
            cachedScenario = null;
        }
    }

    public static List<Pawn> GetPawns(PawnCategory category) =>
        category switch
        {
            PawnCategory.Animals => startingAnimals,
            PawnCategory.Mechs => startingMechs,
            _ => new()
        };

    public static void AddPawn(PawnCategory category, Pawn pawn)
    {
        if (category == PawnCategory.Animals)
            startingAnimals.Add(pawn);
        else if (category == PawnCategory.Mechs)
            startingMechs.Add(pawn);
    }
    public static void RemovePawn(PawnCategory category, Pawn pawn)
    {
        if (category == PawnCategory.Animals)
            startingAnimals.Remove(pawn);
        else if (category == PawnCategory.Mechs)
            startingMechs.Remove(pawn);
    }

    public static void RemoveItemFromStartingThingsNear(Thing thing)
    {
        startingThingsNear.Remove(thing);
    }
    public static void RemoveItemFromStartingThingsFar(Thing thing)
    {
        startingThingsFar.Remove(thing);
    }public static void RemoveItemFromStartingThings(Thing thing)
    {
        startingThings.Remove(thing);
    }
    public static List<Thing> GetStartingThings() => startingThings;
    public static List<Thing> GetStartingThingsNear() => startingThingsNear;
    public static List<Thing> GetStartingThingsFar() => startingThingsFar;

    public class ScenPart_StartingThingsFromPawnEditor : ScenPart
    {
        public override string Summary(Scenario scen) =>
            ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro) +
            ScenSummaryList.SummaryWithList(scen, "MapScatteredWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "PlayerStartsWith")
            {
                foreach (var thing in startingThings) yield return GenLabel.ThingLabel(thing, thing.stackCount, false).CapitalizeFirst();

                foreach (var mech in startingMechs) yield return "Mechanoid".Translate().CapitalizeFirst() + ": " + mech.LabelCap;

                foreach (var animal in startingAnimals) yield return animal.LabelCap;

                foreach (var thing in startingThingsNear) yield return GenLabel.ThingLabel(thing, thing.stackCount, false).CapitalizeFirst();
            }
            else if (tag == "MapScatteredWith")
                foreach (var thing in startingThingsFar)
                    yield return GenLabel.ThingLabel(thing, thing.stackCount, false).CapitalizeFirst();
        }

        public override IEnumerable<Thing> PlayerStartingThings() => startingThings.Concat(startingAnimals).Concat(startingMechs);

        public override void GenerateIntoMap(Map map)
        {
            var thingsNear = new Dictionary<(ThingDef, ThingDef), int>();
            var thingsFar = new Dictionary<(ThingDef, ThingDef), int>();

            foreach (var thing in startingThingsNear)
            {
                var key = (thing.def, thing.Stuff);
                if (!thingsNear.TryGetValue(key, out var value)) value = 0;
                value += thing.stackCount;
                thingsNear[key] = value;
            }

            foreach (var thing in startingThingsFar)
            {
                var key = (thing.def, thing.Stuff);
                if (!thingsFar.TryGetValue(key, out var value)) value = 0;
                value += thing.stackCount;
                thingsFar[key] = value;
            }

            foreach (var ((thingDef, stuff), count) in thingsNear)
                new GenStep_ScatterThings
                {
                    nearPlayerStart = true,
                    allowFoggedPositions = false,
                    thingDef = thingDef,
                    stuff = stuff,
                    count = count,
                    spotMustBeStandable = true,
                    minSpacing = 5f,
                    clusterSize = thingDef.category == ThingCategory.Building ? 1 : 4,
                    allowRoofed = true
                }.Generate(map, default);

            foreach (var ((thingDef, stuff), count) in thingsFar)
                new GenStep_ScatterThings
                {
                    nearPlayerStart = false,
                    allowFoggedPositions = true,
                    thingDef = thingDef,
                    stuff = stuff,
                    count = count,
                    spotMustBeStandable = true,
                    minSpacing = 5f,
                    clusterSize = thingDef.category == ThingCategory.Building ? 1 : 4,
                    allowRoofed = true
                }.Generate(map, default);
        }
    }

    public class PawnPossesions : IExposable
    {
        public List<ThingDefCount> possessions = new List<ThingDefCount>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref possessions, nameof(possessions), LookMode.Deep);
        }
    }

    public class StartingPreset : IExposable
    {
        private List<Pawn> animals;
        private List<Pawn> humans;
        private List<Pawn> mechs;
        public Dictionary<Pawn, PawnPossesions> startingPossessions = new();
        private int takingCount;
        private List<Thing> thingsFar;
        private List<Thing> thingsNear;
        private List<Thing> thingsPossessions;

        public StartingPreset()
        {
            humans = Find.GameInitData.startingAndOptionalPawns.ListFullCopy();
            foreach (var possesion in Find.GameInitData.startingPossessions)
            {
                startingPossessions[possesion.Key] = new PawnPossesions
                {
                    possessions = possesion.Value,
                };
            }
            takingCount = Find.GameInitData.startingPawnCount;
            animals = startingAnimals.ListFullCopy();
            mechs = startingMechs.ListFullCopy();
            thingsPossessions = startingThings.ListFullCopy();
            thingsNear = startingThingsNear.ListFullCopy();
            thingsFar = startingThingsFar.ListFullCopy();
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref humans, nameof(humans), LookMode.Deep);
            Scribe_Collections.Look(ref animals, nameof(animals), LookMode.Deep);
            Scribe_Collections.Look(ref mechs, nameof(mechs), LookMode.Deep);
            Scribe_Collections.Look(ref thingsPossessions, nameof(thingsPossessions), LookMode.Deep);
            Scribe_Collections.Look(ref thingsNear, nameof(thingsNear), LookMode.Deep);
            Scribe_Collections.Look(ref thingsFar, nameof(thingsFar), LookMode.Deep);
            Scribe_Values.Look(ref takingCount, nameof(takingCount));
            Scribe_Collections.Look(ref startingPossessions, nameof(startingPossessions), LookMode.Deep, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) Apply();
        }

        public void Apply()
        {
            Find.GameInitData.startingAndOptionalPawns.Clear();
            Find.GameInitData.startingAndOptionalPawns.AddRange(humans);
            Find.GameInitData.startingPossessions = new Dictionary<Pawn, List<ThingDefCount>>();
            foreach (var pawn in Find.GameInitData.startingAndOptionalPawns)
            {
                Find.GameInitData.startingPossessions[pawn] = new List<ThingDefCount>();
            }
            if (startingPossessions != null)
            {
                foreach (var possesion in startingPossessions)
                {
                    Find.GameInitData.startingPossessions[possesion.Key] = possesion.Value.possessions;
                }
            }
            Find.GameInitData.startingPawnCount = takingCount;
            startingAnimals.Clear();
            startingMechs.Clear();
            startingThings.Clear();
            startingThingsNear.Clear();
            startingThingsFar.Clear();
            startingAnimals.AddRange(animals);
            startingMechs.AddRange(mechs);
            startingThings.AddRange(thingsPossessions);
            startingThingsNear.AddRange(thingsNear);
            startingThingsFar.AddRange(thingsFar);
            PawnEditor.RecachePawnList();
        }
    }
}
