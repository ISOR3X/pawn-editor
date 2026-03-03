using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class SectionWorker_AppearanceTattoos : SectionWorker
{
    readonly Listing_Horizontal listing = new Listing_Horizontal();

    private DefTable faceDefTable;
    private DefTable bodyDefTable;

    private float tattooColorHeight = 30f; // Tracks the height of the rows for the tattoo colors.

    public SectionWorker_AppearanceTattoos(SectionDef def) : base(def)
    {
        IEnumerable<TattooDef> allTattoos = DefDatabase<TattooDef>.AllDefsListForReading;
        IEnumerable<TattooDef> faceTattoos = allTattoos.Where(t => t.tattooType == TattooType.Face);
        IEnumerable<TattooDef> headTattoos = allTattoos.Where(t => t.tattooType == TattooType.Body);
        faceDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Hairs.workerClass, TableDefOf.PawnEditor_Hairs, (Func<IEnumerable<Def>>)(() => faceTattoos),
            TattooDefOf.NoTattoo_Face);
        bodyDefTable = (DefTable_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass, TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => headTattoos),
            TattooDefOf.NoTattoo_Body);
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        listing.Begin(inRect);
        var faceTattooRect = listing.GetRect(6, height: faceDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(faceTattooRect.TakeTopPart(UIUtility.ButtonHeight), "Face");
        faceDefTable.TableOnGUI(faceTattooRect);

        var bodyTattooRect = listing.GetRect(6, height: bodyDefTable.HeaderHeight + 12 * 30f + UIUtility.ButtonHeight + 4f);
        UIComponents.WidgetLabel(bodyTattooRect.TakeTopPart(UIUtility.ButtonHeight), "Body");
        bodyDefTable.TableOnGUI(bodyTattooRect);

        listing.End();
        inRect.TakeTopPart(listing.totalHeight);
    }
}