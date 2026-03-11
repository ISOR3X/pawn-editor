using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor.Extensions;
using RimWorld;
using Verse;

namespace PawnEditor;
public class SectionWorker_AppearanceTattoos : SectionWorker
{
    private readonly DefTable _bodyDefTable;
    private readonly DefTable _faceDefTable;

    public SectionWorker_AppearanceTattoos(SectionDef def) : base(def)
    {
        IEnumerable<TattooDef> allTattoos = DefDatabase<TattooDef>.AllDefsListForReading;
        var faceTattoos = allTattoos.Where(t => t.tattooType == TattooType.Face);
        var bodyTattoos = allTattoos.Where(t => t.tattooType == TattooType.Body);
        _faceDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => faceTattoos), TattooDefOf.NoTattoo_Face);
        _bodyDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => bodyTattoos), TattooDefOf.NoTattoo_Body);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var faceTattooRect = listing.GetRect(_faceDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        Widgets.WidgetLabel(faceTattooRect.TakeTopPart(UIUtility.ButtonHeight), "Face");
        _faceDefTable.TableOnGUI(faceTattooRect);

        var bodyTattooRect = listing.GetRect(_bodyDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        Widgets.WidgetLabel(bodyTattooRect.TakeTopPart(UIUtility.ButtonHeight), "Body");
        _bodyDefTable.TableOnGUI(bodyTattooRect);
    }
}