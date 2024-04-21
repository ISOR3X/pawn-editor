using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class RelationUtilities
{
    public static bool CanAddRelation(this PawnRelationDef def, Pawn pawn, Pawn otherPawn)
    {
        if (pawn == otherPawn) return false;
        if (def.implied) return CanAddImpliedRelation(def, pawn, otherPawn, out _, out _, out _, out _);
        if (pawn.relations.DirectRelationExists(def, otherPawn))
            return false;
        return true;
    }

    public static bool CanAddImpliedRelation(this PawnRelationDef def, Pawn pawn, Pawn otherPawn, out int requiredCount,
        out Func<List<Pawn>, AddResult> createAction, out Func<Pawn, bool> predicate, out bool highlightGender)
    {
        requiredCount = 0;
        createAction = null;
        predicate = null;
        highlightGender = false;
        if (def == PawnRelationDefOf.Sibling)
        {
            var father = pawn.GetFather();
            var mother = pawn.GetMother();
            var otherFather = otherPawn.GetFather();
            var otherMother = otherPawn.GetMother();
            highlightGender = true;
            if (father != null && otherFather != null && father == otherFather)
            {
                if (mother != null && otherMother != null && mother == otherMother) return false;
                requiredCount = 1;
                predicate = p => p.gender == Gender.Female;
                createAction = list => new SuccessInfo(() =>
                {
                    if (list.Count >= 1)
                    {
                        pawn.SetMother(list[0]);
                        otherPawn.SetMother(list[0]);
                    }
                    else
                    {
                        AddSiblingRelationship(def, pawn, otherPawn);
                    }
                    TabWorker_Table<Pawn>.ClearCacheFor<TabWorker_Social>();
                });
            }
            else if (mother != null && otherMother != null && mother == otherMother)
            {
                requiredCount = 1;
                predicate = p => p.gender == Gender.Male;
                createAction = list => new SuccessInfo(() =>
                {
                    if (list.Count >= 1)
                    {
                        pawn.SetFather(list[0]);
                        otherPawn.SetFather(list[0]);
                    }
                    else
                    {
                        AddSiblingRelationship(def, pawn, otherPawn);
                    }
                    TabWorker_Table<Pawn>.ClearCacheFor<TabWorker_Social>();
                });
            }
            else
            {
                requiredCount = 2;
                createAction = list => list.Count == 2 && list[0].gender == list[1].gender
                    ? "PawnEditor.MustBeOneEachGender".Translate()
                    : new SuccessInfo(() =>
                    {
                        Log.Message("3: " + list.ToStringSafeEnumerable() + " - pawn: " + pawn + " - otherPawn: " + otherPawn); ;
                        if (list.Count >= 1)
                        {
                            pawn.SetParent(list[0]);
                            otherPawn.SetParent(list[0]);
                        }
                        if (list.Count == 2)
                        {
                            pawn.SetParent(list[1]);
                            otherPawn.SetParent(list[1]);
                        }
                        if (!list.Any())
                        {
                            AddSiblingRelationship(def, pawn, otherPawn);
                        }
                        TabWorker_Table<Pawn>.ClearCacheFor<TabWorker_Social>();
                    });
            }

            return true;
        }

        return false;
    }

    private static void AddSiblingRelationship(PawnRelationDef def, Pawn pawn, Pawn otherPawn)
    {
        def.AddDirectRelation(pawn, otherPawn);
        var request = new PawnGenerationRequest();
        def.Worker.CreateRelation(pawn, otherPawn, ref request);
        TrySyncParents(pawn, otherPawn);
        TrySyncParents(otherPawn, pawn);
    }

    private static void TrySyncParents(Pawn pawn, Pawn otherPawn)
    {
        var mother = pawn.GetMother();
        if (mother != null && otherPawn.GetMother() != mother)
        {
            otherPawn.SetMother(mother);
        }
        var father = pawn.GetFather();
        if (father != null && otherPawn.GetFather() != father)
        {
            otherPawn.SetFather(father);
        }
    }

    // Copy of AddDirectRelation from Pawn_RelationsTracker.cs
    public static void AddDirectRelation(this PawnRelationDef def, Pawn pawn, Pawn otherPawn)
    {
        var startTicks = Current.ProgramState == ProgramState.Playing ? Find.TickManager.TicksGame : 0;
        def.Worker.OnRelationCreated(pawn, otherPawn);
        pawn.relations.directRelations.Add(new(def, otherPawn, startTicks));
        otherPawn.relations.pawnsWithDirectRelationsWithMe.Add(pawn);
        if (def.reflexive)
        {
            otherPawn.relations.directRelations.Add(new(def, pawn, startTicks));
            pawn.relations.pawnsWithDirectRelationsWithMe.Add(otherPawn);
        }
        pawn.relations.GainedOrLostDirectRelation();
        otherPawn.relations.GainedOrLostDirectRelation();

        if (Current.ProgramState != ProgramState.Playing)
            return;
        if (!pawn.Dead && pawn.health != null)
            for (var index = pawn.health.hediffSet.hediffs.Count - 1; index >= 0; --index)
                pawn.health.hediffSet.hediffs[index].Notify_RelationAdded(otherPawn, def);
        if (otherPawn.Dead || otherPawn.health == null)
            return;
        for (var index = otherPawn.health.hediffSet.hediffs.Count - 1; index >= 0; --index)
            otherPawn.health.hediffSet.hediffs[index].Notify_RelationAdded(pawn, def);
    }

    public static void SetParent(this Pawn pawn, Pawn newParent)
    {
        if (newParent == null)
        {
            if (pawn.relations.DirectRelations.FirstOrDefault(dr => dr.def == PawnRelationDefOf.Parent) is { } relation)
                pawn.relations.RemoveDirectRelation(relation);
            return;
        }

        switch (newParent.gender)
        {
            case Gender.None:
                pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, newParent);
                break;
            case Gender.Male:
                pawn.SetFather(newParent);
                break;
            case Gender.Female:
                pawn.SetMother(newParent);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
