using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor.Extensions;
using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_TattooBody : SectionWorker
{
    private readonly DefTable _bodyDefTable;

    public SectionWorker_TattooBody(SectionDef def) : base(def)
    {
        IEnumerable<TattooDef> allTattoos = DefDatabase<TattooDef>.AllDefsListForReading;
        var bodyTattoos = allTattoos.Where(t => t.tattooType == TattooType.Body);
        _bodyDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => bodyTattoos), TattooDefOf.NoTattoo_Body);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var bodyTattooRect = listing.GetRect(_bodyDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        listing.LabelH2("Body");
        _bodyDefTable.TableOnGUI(bodyTattooRect);
    }
}