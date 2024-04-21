using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ListingMenu_Relations : ListingMenu<PawnRelationDef>
{
    private readonly Pawn _otherPawn;

    public ListingMenu_Relations(Pawn pawn, Pawn otherPawn, UITable<Pawn> table, List<Filter<PawnRelationDef>> filters = null)
        : base(DefDatabase<PawnRelationDef>.AllDefs.Where(rd => rd.CanAddRelation(pawn, otherPawn)).ToList(), 
            r => r.GetGenderSpecificLabelCap(otherPawn), def =>
            {
                Func<List<Pawn>, AddResult> createInt = _ => new SuccessInfo(() => def.AddDirectRelation(pawn, otherPawn));
                var required = 0;
                Func<Pawn, bool> predicate = null;
                var highlightGender = false;

                if (def.implied && !def.CanAddImpliedRelation(pawn, otherPawn, out required, out createInt, out predicate, 
                    out highlightGender)) return false;

                AddResult Create(List<Pawn> list) =>
                    new ConfirmInfo("PawnEditor.RelationExists".Translate(pawn.NameShortColored, def.LabelCap), 
                    "RelationExists", createInt(list), !def.implied && pawn.relations.GetDirectRelationsCount(def) > 0);

                if (required > 0)
                {
                    var list = PawnEditor.AllPawns.GetList();
                    list.Remove(pawn);
                    list.Remove(otherPawn);
                    if (predicate != null) list.RemoveAll(p => !predicate(p));
                    PawnEditor.AllPawns.UpdateCache(null, PawnCategory.Humans);
                    var pawnsToSelect = new List<Pawn>();
                    var title = "PawnEditor.Pawn".Translate();

                    if (def == PawnRelationDefOf.Sibling)
                    {
                        TryAddParent(pawn.GetMother(), pawnsToSelect, list);
                        TryAddParent(pawn.GetFather(), pawnsToSelect, list);
                        title = "PawnEditor.Parent".Translate();
                    }

                    Find.WindowStack.Add(new ListingMenu_Pawns(list, pawnsToSelect, title, pawn, "Add".Translate().CapitalizeFirst(), Create,
                        new IntRange(0, required), "Back".Translate(), () => Find.WindowStack.Add(
                            new ListingMenu_Relations(pawn, otherPawn, table, filters)), highlightGender));
                    return true;
                }

                table.ClearCache();
                return Create(new());
            }, "PawnEditor.Choose".Translate() + " " + "PawnEditor.Relation".Translate(),
            r => r.description, null, filters, pawn, null, "Back".Translate(), () =>
            {
                PawnEditor.AllPawns.UpdateCache(null, PawnCategory.Humans);
                var list = PawnEditor.AllPawns.GetList();
                list.Remove(pawn);
                Find.WindowStack.Add(new ListingMenu_Pawns(list, pawn, "Next".Translate(),
                    p =>
                    {
                        Find.WindowStack.Add(new ListingMenu_Relations(pawn, p, table, filters));
                        return true;
                    }));
            }) =>
        _otherPawn = otherPawn;

    private static void TryAddParent(Pawn parent, List<Pawn> pawnsToSelect, List<Pawn> list)
    {
        if (parent != null)
        {
            pawnsToSelect.Add(parent);
            if (list.Contains(parent) is false)
            {
                list.Add(parent);
            }
        }
    }

    protected override string NextLabel =>
        Listing.Selected.implied && Listing.Selected.CanAddImpliedRelation(Pawn, _otherPawn, out var count, out _, out _, out _) && count > 0
            ? "Next".Translate()
            : "Add".Translate().CapitalizeFirst();

    protected override void DrawSelected(ref Rect inRect)
    {
        Widgets.DrawTextureFitted(new(inRect.x, inRect.y, 32f, 32f), PawnEditor.GetPawnTex(_otherPawn, new(25, 25), Rot4.South, cameraZoom: 2f), .8f);
        inRect.xMin += 32f;


        var labelStr = $"{"StartingPawnsSelected".Translate()}: ";
        var labelWidth = Text.CalcSize(labelStr).x;
        var selectedStr = _otherPawn.Name.ToStringShort + ", " + (Listing.Selected != null
            ? (TaggedString)Listing.LabelGetter(Listing.Selected)
            : "None".Translate().Colorize(ColoredText.SubtleGrayColor));
        Widgets.Label(inRect, labelStr.Colorize(ColoredText.SubtleGrayColor));
        inRect.xMin += labelWidth;
        Widgets.Label(inRect, selectedStr);
    }
}
