using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static class RelationUtilities
{
    public static bool CanAddRelation(this PawnRelationDef def, Pawn pawn, Pawn otherPawn)
    {
        if (pawn == otherPawn) return false;
        if (def.implied) return CanAddImpliedRelation(def, pawn, otherPawn, out _, out _, out _);
        if (pawn.relations.DirectRelationExists(def, otherPawn))
            return false;
        return true;
    }

    public static bool CanAddImpliedRelation(this PawnRelationDef def, Pawn pawn, Pawn otherPawn, out int requiredCount,
        out Func<List<Pawn>, bool> createAction, out Func<Pawn, bool> predicate)
    {
        requiredCount = 0;
        createAction = null;
        predicate = null;
        if (def == PawnRelationDefOf.Sibling)
        {
            var father = pawn.GetFather();
            var mother = pawn.GetMother();
            var otherFather = otherPawn.GetFather();
            var otherMother = otherPawn.GetMother();
            if (father != null && otherFather != null && father == otherFather)
            {
                if (mother != null && otherMother != null && mother == otherMother) return false;
                requiredCount = 1;
                predicate = p => p.gender == Gender.Female;
                createAction = list =>
                {
                    pawn.SetMother(list[0]);
                    otherPawn.SetMother(list[0]);
                    return true;
                };
            }
            else if (mother != null && otherMother != null && mother == otherMother)
            {
                requiredCount = 1;
                predicate = p => p.gender == Gender.Male;
                createAction = list =>
                {
                    pawn.SetFather(list[0]);
                    otherPawn.SetFather(list[0]);
                    return true;
                };
            }
            else
            {
                requiredCount = 2;
                createAction = list =>
                {
                    if (list[0].gender == list[1].gender)
                    {
                        Messages.Message("PawnEditor.MustBeOneEachGender".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }

                    pawn.SetParent(list[0]);
                    otherPawn.SetParent(list[0]);
                    pawn.SetParent(list[1]);
                    otherPawn.SetParent(list[1]);

                    return true;
                };
            }

            return true;
        }

        return false;
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
