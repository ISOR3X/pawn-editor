using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
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
            default:
                AccessTools.Field(r.GetType(), "loadID")?.SetValue(r, Find.UniqueIDsManager.GetNextThingID()); break;
        }
    }

    private static IEnumerable<FieldInfo> GetAllFields(Type type)
    {
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly
                                                                | BindingFlags.NonPublic | BindingFlags.Public))
                yield return f;
    }

    private sealed class IdentityComparer : IEqualityComparer<object>
    {
        internal static readonly IdentityComparer Instance = new();
        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
        int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}