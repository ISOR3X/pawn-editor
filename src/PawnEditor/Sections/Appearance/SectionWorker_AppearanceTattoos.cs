using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class SectionWorker_AppearanceTattoos : SectionWorker
{
    private readonly DefTable _bodyDefTable;

    private readonly DefTable _faceDefTable;
    private readonly Listing_Horizontal _listing = new();

    // private float tattooColorHeight = 30f; // Tracks the height of the rows for the tattoo colors.

    public SectionWorker_AppearanceTattoos(SectionDef def) : base(def)
    {
        IEnumerable<TattooDef> allTattoos = DefDatabase<TattooDef>.AllDefsListForReading;
        var faceTattoos = allTattoos.Where(t => t.tattooType == TattooType.Face);
        var headTattoos = allTattoos.Where(t => t.tattooType == TattooType.Body);
        _faceDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass,
            TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => faceTattoos),
            TattooDefOf.NoTattoo_Face);
        _bodyDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => headTattoos),
            TattooDefOf.NoTattoo_Body);
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        _listing.Begin(inRect);
        var faceTattooRect = _listing.GetRect(6, _faceDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(faceTattooRect.TakeTopPart(UIUtility.ButtonHeight), "Face");
        _faceDefTable.TableOnGUI(faceTattooRect);

        var bodyTattooRect = _listing.GetRect(6, _bodyDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(bodyTattooRect.TakeTopPart(UIUtility.ButtonHeight), "Body");
        _bodyDefTable.TableOnGUI(bodyTattooRect);

        _listing.End();
        inRect.TakeTopPart(_listing.TotalHeight);
    }
}