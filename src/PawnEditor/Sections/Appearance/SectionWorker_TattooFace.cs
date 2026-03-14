using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor.Extensions;
using RimWorld;
using Verse;

namespace PawnEditor;

public class SectionWorker_TattooFace : SectionWorker
{
    private readonly DefTable _faceDefTable;

    public SectionWorker_TattooFace(SectionDef def) : base(def)
    {
        IEnumerable<TattooDef> allTattoos = DefDatabase<TattooDef>.AllDefsListForReading;
        var faceTattoos = allTattoos.Where(t => t.tattooType == TattooType.Face);
        _faceDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => faceTattoos), TattooDefOf.NoTattoo_Face);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var faceTattooRect = listing.GetRect(_faceDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        listing.LabelH2("Face");
        _faceDefTable.TableOnGUI(faceTattooRect);
    }
}