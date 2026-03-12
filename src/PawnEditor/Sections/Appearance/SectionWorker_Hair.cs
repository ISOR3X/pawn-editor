using System;
using System.Collections.Generic;
using PawnEditor.Extensions;
using RimWorld;
using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_Hair : SectionWorker
{
    private const int RowCount = 11;
    private const float RowHeight = 30f;
    private readonly DefTable _hairDefTable;

    public SectionWorker_Hair(SectionDef def) : base(def)
    {
        var hairs = DefDatabase<HairDef>.AllDefs;
        _hairDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => hairs), HairDefOf.Bald);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        listing.LabelH2("Hair");
        var hairTableRect =
            listing.GetRect(_hairDefTable.HeaderHeight + (RowCount + 1) * RowHeight + UIUtility.ButtonHeight + 4f);

        _hairDefTable.TableOnGUI(hairTableRect);
    }
}