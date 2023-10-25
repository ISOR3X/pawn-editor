using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Relations : ListingMenu<PawnRelationDef>
{
    private static readonly List<PawnRelationDef> relationDefs;
    
    static ListingMenu_Relations()
    {
        relationDefs = DefDatabase<PawnRelationDef>.AllDefsListForReading;
    }
    
    public ListingMenu_Relations(Pawn pawn, Pawn otherPawn, Action<PawnRelationDef> action, List<TFilter<PawnRelationDef>> filters = null) 
        : base(GetPossibleRelations(pawn, otherPawn), r => r.LabelCap, action, "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Relation".Translate(), 
            r => r.description, null, filters, pawn)
    {
    }

    private static List<PawnRelationDef> GetPossibleRelations(Pawn pawn, Pawn otherPawn)
    {
        return relationDefs.Where(rd => AddDirectRelation(pawn, rd, otherPawn)).ToList();
    }
    
    // Direct copy of AddDirectRelation from Pawn_RelationsTracker.cs
    private static bool AddDirectRelation(Pawn pawn, PawnRelationDef def, Pawn otherPawn)
    {
        if (def.implied)
            return false;
        else if (otherPawn == pawn)
            return false;
        else if (pawn.relations.DirectRelationExists(def, otherPawn))
        {
            return false;
        }
        else
        {
            int startTicks = Current.ProgramState == ProgramState.Playing ? Find.TickManager.TicksGame : 0;
            def.Worker.OnRelationCreated(pawn, otherPawn);
            pawn.relations.directRelations.Add(new DirectPawnRelation(def, otherPawn, startTicks));
            otherPawn.relations.pawnsWithDirectRelationsWithMe.Add(pawn);
            if (def.reflexive)
            {
                otherPawn.relations.directRelations.Add(new DirectPawnRelation(def, pawn, startTicks));
                pawn.relations.pawnsWithDirectRelationsWithMe.Add(otherPawn);
            }
            pawn.relations.GainedOrLostDirectRelation();
            otherPawn.relations.GainedOrLostDirectRelation();
            if (Current.ProgramState != ProgramState.Playing)
                return true;
            if (!pawn.Dead && pawn.health != null)
            {
                for (int index = pawn.health.hediffSet.hediffs.Count - 1; index >= 0; --index)
                    pawn.health.hediffSet.hediffs[index].Notify_RelationAdded(otherPawn, def);
            }
            if (otherPawn.Dead || otherPawn.health == null)
                return true;
            for (int index = otherPawn.health.hediffSet.hediffs.Count - 1; index >= 0; --index)
                otherPawn.health.hediffSet.hediffs[index].Notify_RelationAdded(pawn, def);
            
            return true;
        }
    }
}