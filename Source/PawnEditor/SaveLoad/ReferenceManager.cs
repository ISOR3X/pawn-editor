using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace PawnEditor;

public static partial class SaveLoadUtility
{
    private static readonly Dictionary<(IExposable, string), (string, Type)> loadInfo = new();

    /// <summary>
    /// This is where Pawn Editor accesses the XML for saving and loading pawns. <paramref name="label"/> is the name of the xml node name, and <paramref name="refee"/> is the type of data loaded from that location.
    /// </summary>
    /// <param name="refee"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    public static bool InterceptReferences(ref ILoadReferenceable refee, string label)
    {
        if (!currentlyWorking) return true;
        if (savedItems.Contains(refee)) return true;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (refee == null) Scribe.saver.WriteElement(label, "");
            else if (Scribe.EnterNode(label))
            {
                Scribe.saver.WriteAttribute("Class", GenTypes.GetTypeNameWithoutIgnoredNamespaces(refee.GetType()));
                Scribe.saver.writer.WriteRaw(GetReferenceData(refee));
                Scribe.ExitNode();
            }
        }
        else if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            //setup xml data
            XmlNode xmlNode = Scribe.loader.curXmlParent?[label];
            var typeName = xmlNode?.Attributes?["Class"]?.Value;
            var data = xmlNode?.InnerText;
            
            if (data.NullOrEmpty()) refee = null;

            //loads a random faction for the pawn if the pawn's faction is set to "Random"
            else if(typeName == "Faction")
            {
                if (data == "Random")
                {
                    Faction faction;

                    if(Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out faction, false))
                    {
                        refee = faction;
                    }
                }
                else
                {
                    var type = typeName.NullOrEmpty() ? null : GenTypes.GetTypeInAnyAssembly(typeName);
                    loadInfo.Add((Scribe.loader.curParent, Scribe.loader.curPathRelToParent + '/' + label), (data, type));
                    if (type == null) return true;
                    refee = LoadReferenceData(data, type);
                }
            }
            else
            {
                var type = typeName.NullOrEmpty() ? null : GenTypes.GetTypeInAnyAssembly(typeName);
                loadInfo.Add((Scribe.loader.curParent, Scribe.loader.curPathRelToParent + '/' + label), (data, type));
                if (type == null) return true;
                refee = LoadReferenceData(data, type);
            }
        }
        else if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs
              && loadInfo.TryGetValue((Scribe.loader.curParent, Scribe.loader.curPathRelToParent + '/' + label), out var info))
        {
            var (data, type) = info;
            if (type == null) return true;
            refee = LoadReferenceData(data, type);
        }


        return false;
    }

    public static string GetReferenceData(ILoadReferenceable refee)
    {
        if (currentItem != null && currentItem.GetType() == refee.GetType() && currentItem.GetUniqueLoadID() == refee.GetUniqueLoadID()) return "__CURRENT";
        if (refee is Pawn refPawn && refPawn == currentPawn) return "__CURRENTPAWN";
        try
        {
            switch (refee)
            {
                case ApparelPolicy outfit:
                    return outfit.label;
                case DrugPolicy policy:
                    return policy.label;
                case FoodPolicy restriction:
                    return restriction.label;
                case Ideo ideo when currentPawn is { Faction.ideos: not null } ideoPawn && ideoPawn.Faction.ideos.PrimaryIdeo == ideo:
                    return "__FACTIONIDEO";
                case Ideo ideo:
                    foreach (var ideoFaction in Find.FactionManager.AllFactions)
                        if (ideoFaction.ideos != null && ideoFaction.ideos.PrimaryIdeo == ideo)
                            return GetReferenceData(ideoFaction) + ".IDEO";
                    return ideo.name;
                case Faction faction:
                    if (faction.IsPlayer) return "__PlayerFaction";
                    var list = Find.FactionManager.AllFactions.Where(f => f.def == faction.def).ToList();
                    return $"{faction.def.defName}.{list.IndexOf(faction)}";
                case Pawn pawn:
                    return pawn.Name == null ? pawn.KindLabel : pawn.Name.ToStringFull;
                case Precept precept:
                    return GetReferenceData(precept.ideo) + "." + precept.name;
                case Thing thing:
                    return thing.def.defName + "." + thing.Stuff + "." + thing.stackCount;
                case MapParent parent when parent == Find.CurrentMap.Parent:
                    return "__CURRENTMAPPARENT";
                case Job job:
                    return job.def.defName + ":"
                                           + LocalTargetInfoToString(job.targetA) + ","
                                           + LocalTargetInfoToString(job.targetB) + ","
                                           + LocalTargetInfoToString(job.targetC) + "." + job.count;
                case Ability ability:
                    return ability.sourcePrecept == null
                        ? ability.def.defName + "," + GetReferenceData(ability.pawn)
                        : ability.def.defName + "," + GetReferenceData(ability.pawn) + "," + GetReferenceData(ability.sourcePrecept);
                case Gene gene:
                    return gene.def.defName + "," + GetReferenceData(gene.pawn);
                case Battle or LogEntry or Tale:

                    Log.Warning(
                        $"[PawnEditor] Found reference to {GenTypes.GetTypeNameWithoutIgnoredNamespaces(refee.GetType())}, which will be ignored. This shouldn't cause issues on load.");
                    return null;
                default:
                    Log.Error($"Unhandled saving item {refee} with type {refee.GetType()}");
                    break;
            }
        }
        catch (Exception e) { Log.Error($"Error while getting reference data for {refee}: {e}"); }

        return "";
    }

    public static ILoadReferenceable LoadReferenceData(string data, Type type)
    {
        try
        {
            if (data == "__CURRENT") return currentItem;
            if (data == "__CURRENTPAWN") return currentPawn;
            if (data == "__CURRENTMAPPARENT") return Find.CurrentMap.Parent;
            if (type == typeof(ApparelPolicy)) return Current.Game?.outfitDatabase?.AllOutfits.FirstOrDefault(x => x.label == data);
            if (type == typeof(DrugPolicy)) return Current.Game?.drugPolicyDatabase?.AllPolicies.FirstOrDefault(x => x.label == data);
            if (type == typeof(FoodPolicy)) return Current.Game?.foodRestrictionDatabase?.AllFoodRestrictions.FirstOrDefault(x => x.label == data);
            if (type == typeof(ReadingPolicy)) return Current.Game?.readingPolicyDatabase?.AllReadingPolicies.FirstOrDefault(x => x.label == data);
            if (type == typeof(Ideo))
            {
                if (data == "__FACTIONIDEO") return currentPawn?.Faction?.ideos?.PrimaryIdeo;

                if (data.Contains("."))
                {
                    var arr = data.Split('.');
                    if (arr[1] == "IDEO") return (LoadReferenceData(arr[0], typeof(Faction)) as Faction)?.ideos?.PrimaryIdeo;
                }

                foreach (var ideo in Find.IdeoManager.ideos)
                    if (ideo.name == data)
                        return ideo;
            }

            if (type == typeof(Faction))
            {
                if (data == "__PlayerFaction") return Faction.OfPlayer;
                var arr = data.Split('.');
                var def = DefDatabase<FactionDef>.GetNamed(arr[0]);
                if (def != null)
                {
                    var list = Find.FactionManager.AllFactions.Where(f => f.def == def).ToList();
                    var ind = int.Parse(arr[1]);
                    if (ind < 0 || ind >= list.Count) return list.FirstOrDefault();
                    return list[ind];
                }
            }

            if (typeof(Pawn).IsAssignableFrom(type))
            {
                foreach (var pawn in PawnsFinder.All_AliveOrDead)
                    if ((pawn.Name == null ? pawn.KindLabel : pawn.Name.ToStringFull) == data)
                        return pawn;

                if (Find.GameInitData != null)
                    foreach (var pawn in Find.GameInitData.startingAndOptionalPawns)
                        if ((pawn.Name == null ? pawn.KindLabel : pawn.Name.ToStringFull) == data)
                            return pawn;

                Log.Warning($"[PawnEditor] Failed to find pawn with name {data}");
                return null;
            }

            if (typeof(Precept).IsAssignableFrom(type))
            {
                var arr = data.Split('.');
                var ideo = (Ideo)LoadReferenceData(arr[0], typeof(Ideo));
                foreach (var precept in ideo.precepts)
                    if (precept.name == arr[1])
                        return precept;
            }

            if (typeof(Thing).IsAssignableFrom(type))
            {
                var arr = data.Split('.');
                var def = DefDatabase<ThingDef>.GetNamed(arr[0]);
                var stuff = arr[1].NullOrEmpty() ? null : DefDatabase<ThingDef>.GetNamedSilentFail(arr[1]);
                var count = int.Parse(arr[2]);
                foreach (var obj in Scribe.loader.crossRefs.loadedObjectDirectory.allObjectsByLoadID.Values)
                    if (obj is Thing thing && thing.def == def && thing.Stuff == stuff && thing.stackCount == count)
                        return thing;

                var madeThing = ThingMaker.MakeThing(def, stuff);
                madeThing.stackCount = count;
                return madeThing;
            }

            if (typeof(Job) == type)
            {
                var arr1 = data.Split(':');
                var arr2 = arr1[1].Split('.');
                var arr3 = arr2[0].Split(',');
                var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(arr1[0]));
                job.count = int.Parse(arr2[1]);
                job.targetA = LocalTargetInfoFromString(arr3[0]);
                job.targetB = LocalTargetInfoFromString(arr3[1]);
                job.targetC = LocalTargetInfoFromString(arr3[2]);
                return job;
            }

            if (typeof(Ability).IsAssignableFrom(type))
            {
                var arr = data.Split(',');
                var def = DefDatabase<AbilityDef>.GetNamed(arr[0]);
                if (def == null) return null;
                var pawn = (Pawn)LoadReferenceData(arr[1], typeof(Pawn));
                if (pawn == null) return null;
                if (arr.Length == 2)
                    return AbilityUtility.MakeAbility(def, pawn);
                var precept = (Precept)LoadReferenceData(arr[2], typeof(Precept));
                return AbilityUtility.MakeAbility(def, pawn, precept);
            }

            if (typeof(Gene).IsAssignableFrom(type))
            {
                var arr = data.Split(',');
                var def = DefDatabase<GeneDef>.GetNamed(arr[0]);
                if (def == null) return null;
                var pawn = (Pawn)LoadReferenceData(arr[1], typeof(Pawn));
                if (pawn == null) return null;
                return GeneMaker.MakeGene(def, pawn);
            }
        }
        catch (Exception e) { Log.Error($"Error while loading reference to a {type.FullName} with data {data}: {e}"); }

        return null;
    }

    private static string LocalTargetInfoToString(LocalTargetInfo target)
    {
        if (!target.IsValid) return "";
        if (target.HasThing) return GetReferenceData(target.Thing);
        if (target.Cell.IsValid) return target.Cell.ToString();
        return "";
    }

    private static LocalTargetInfo LocalTargetInfoFromString(string str)
    {
        if (str.NullOrEmpty()) return LocalTargetInfo.Invalid;
        if (str.Length != 0 && str[0] == '(') return new(IntVec3.FromString(str));
        if (LoadReferenceData(str, typeof(Thing)) is Thing thing) return new(thing);
        return LocalTargetInfo.Invalid;
    }

    public static void InterceptIDList(ref List<string> targetLoadIDList)
    {
        var nodes = Scribe.loader.curXmlParent?.ChildNodes.Cast<XmlNode>().ToList();
        var usedIndices = new HashSet<int>();
        if (nodes == null || targetLoadIDList == null) return;
        for (var i = 0; i < targetLoadIDList.Count; i++)
        {
            var id = targetLoadIDList[i];
            for (var j = 0; j < nodes.Count; j++)
                if (!usedIndices.Contains(j) && nodes[j].InnerText == id)
                {
                    var typeName = nodes[j]?.Attributes?["Class"]?.Value;
                    var type = typeName.NullOrEmpty() ? null : GenTypes.GetTypeInAnyAssembly(typeName);
                    if (type != null)
                    {
                        var item = LoadReferenceData(id, type);
                        if (item == null)
                        {
                            Log.Error($"[PawnEditor] Failed to find item! data={id}, type={type}");
                            continue;
                        }

                        Scribe.loader.crossRefs.loadedObjectDirectory.RegisterLoaded(item);
                        targetLoadIDList[i] = item.GetUniqueLoadID();
                        usedIndices.Add(j);
                    }
                }
        }
    }
}
