using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ListingMenu_Relations : ListingMenu<PawnRelationDef>
{
    private readonly Pawn _otherPawn;

    public ListingMenu_Relations(Pawn pawn, Pawn otherPawn, List<TFilter<PawnRelationDef>> filters = null)
        : base(DefDatabase<PawnRelationDef>.AllDefs.Where(rd => rd.CanAddRelation(pawn, otherPawn)).ToList(), r => r.LabelCap, def =>
            {
                if (def.implied && def.CanAddImpliedRelation(pawn, otherPawn, out var required, out var create, out var predicate))
                {
                    if (required > 0)
                    {
                        PawnEditor.AllPawns.UpdateCache(null, PawnCategory.All);
                        var list = PawnEditor.AllPawns.GetList();
                        list.Remove(pawn);
                        list.Remove(otherPawn);
                        if (predicate != null) list.RemoveAll(p => !predicate(p));
                        Find.WindowStack.Add(new ListingMenu_Pawns(list, pawn, "Add".Translate().CapitalizeFirst(), create, required, "Back".Translate(),
                            () => Find.WindowStack.Add(new ListingMenu_Relations(pawn, otherPawn, filters))));
                    }
                    else
                        create(new());
                }
                else def.AddDirectRelation(pawn, otherPawn);
            }, "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Relation".Translate(),
            r => r.description, null, filters, pawn, null, "Back".Translate(), () =>
            {
                PawnEditor.AllPawns.UpdateCache(null, PawnCategory.All);
                var list = PawnEditor.AllPawns.GetList();
                list.Remove(pawn);
                Find.WindowStack.Add(new ListingMenu_Pawns(list, pawn, "Next".Translate(),
                    p => Find.WindowStack.Add(new ListingMenu_Relations(pawn, p, filters))));
            }) =>
        _otherPawn = otherPawn;

    protected override string NextLabel =>
        Listing.Selected.implied && Listing.Selected.CanAddImpliedRelation(Pawn, _otherPawn, out var count, out _, out _) && count > 0
            ? "Next".Translate()
            : "Add".Translate().CapitalizeFirst();
}
