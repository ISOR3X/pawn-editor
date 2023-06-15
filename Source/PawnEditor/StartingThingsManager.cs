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

        Find.Scenario.parts.Add(new ScenPart_StartingThingsFromPawnEditor
        {
            def = new ScenPartDef
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
            _ => new List<Pawn>()
        };

    public static void AddPawn(PawnCategory category, Pawn pawn)
    {
        if (category == PawnCategory.Animals)
            startingAnimals.Add(pawn);
        else if (category == PawnCategory.Mechs)
            startingMechs.Add(pawn);
    }

    // TODO: Probably need to change this when I add the starting things editor
    public static IEnumerable<Thing> GetThings() => startingThings.Concat(startingThingsNear).Concat(startingThingsFar);

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

        public override IEnumerable<Thing> PlayerStartingThings()
        {
            Log.Message("Getting starting things");
            GenDebug.LogList(startingThings);
            GenDebug.LogList(startingAnimals);
            GenDebug.LogList(startingMechs);
            return startingThings.Concat(startingAnimals).Concat(startingMechs);
        }

        public override void GenerateIntoMap(Map map)
        {
            Log.Message("Generating into map!");
            GenDebug.LogList(startingThingsNear);
            GenDebug.LogList(startingThingsFar);
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

            foreach (var ((thingDef, stuff), count) in thingsNear)
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
}
