using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PawnEditor;

public static class PawnFixup
{
    public static void FixupLoadedPawn(Pawn pawn)
    {
        pawn.relations?.directRelations.RemoveAll(r => r.otherPawn == null);
        if (pawn.mindState != null) pawn.mindState.duty = null;
        if (pawn.jobs != null)
        {
            pawn.jobs.ClearQueuedJobs();
            pawn.jobs.curJob = null;
            pawn.jobs.curDriver = null;
        }

        pawn.ownership?.UnclaimAll();
    }

    /// <summary>
    /// Using reflection, find fields that require a loadID and assign a new one.
    /// </summary>
    public static void ReassignIDs(Pawn pawn)
    {
        var visited = new HashSet<object>(IdentityComparer.Instance);
        Walk(pawn);
        return;

        void Walk(object? obj)
        {
            if (obj == null || !visited.Add(obj)) return;
            if (obj is ILoadReferenceable r) AssignNewID(r);
            foreach (var field in GetAllFields(obj.GetType()))
            {
                object? val;
                try
                {
                    val = field.GetValue(obj);
                }
                catch
                {
                    continue;
                }

                switch (val)
                {
                    case IExposable exposable:
                        Walk(exposable);
                        break;
                    case IEnumerable<IExposable> collection:
                        foreach (var item in collection) Walk(item);
                        break;
                }
            }
        }
    }

    private static void AssignNewID(ILoadReferenceable r)
    {
        switch (r)
        {
            case Thing t: t.thingIDNumber = Find.UniqueIDsManager.GetNextThingID(); break;
            case Hediff h: h.loadID = Find.UniqueIDsManager.GetNextHediffID(); break;
            case Gene g: g.loadID = Find.UniqueIDsManager.GetNextGeneID(); break;
            case Lord l: l.loadID = Find.UniqueIDsManager.GetNextLordID(); break;
            case Bill b: b.loadID = Find.UniqueIDsManager.GetNextBillID(); break;
            case Job j: j.loadID = Find.UniqueIDsManager.GetNextJobID(); break;
            default:
                // Default value is used as a fallback, for example in the case that mods add a pawn component with a loadID.
                var loadIDField = AccessTools.Field(r.GetType(), "loadID");
                if (loadIDField != null)
                {
                    // Log.Warning($"No explicit load id for {r.GetType().Name}");
                    // Some loadIDs expect a string instead of an int.
                    var newID = Find.UniqueIDsManager.GetNextThingID();
                    if (loadIDField.FieldType == typeof(string))
                        loadIDField.SetValue(r, r.GetType().Name + "_" + newID);
                    else
                        loadIDField.SetValue(r, newID);
                }

                break;
        }
    }

    private static IEnumerable<FieldInfo> GetAllFields(Type type)
    {
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly
                                                                | BindingFlags.NonPublic | BindingFlags.Public))
                yield return f;
    }

    /// <summary>
    /// Compare equality using instances instead of values to avoid revisiting nodes.
    /// The object graph being walked in <see cref="ReassignIDs"/> may hold circular references and this prevents that from the walk looping infinitely.
    /// </summary>
    private sealed class IdentityComparer : IEqualityComparer<object>
    {
        internal static readonly IdentityComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}