using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Apparel(SectionDef def) : SectionWorker(def)
{
    private const int RowCount = 11;
    private const float RowHeight = 30f;

    private readonly ThingTableWorker _apparelTable = (ThingTableWorker)Activator.CreateInstance(
        TableDefOf.PawnEditor_Apparel.workerClass,
        TableDefOf.PawnEditor_Apparel,
        (Func<IEnumerable<Thing>>)GetCurrentApparel,
        null);

    private Pawn? _lastPawn;

    private static IEnumerable<Thing> GetCurrentApparel()
    {
        var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        return pawn?.apparel.WornApparel.Cast<Thing>() ?? [];
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        if (_lastPawn != pawn)
        {
            _lastPawn = pawn;
            _apparelTable.SetDirty();
        }

        listing.LabelH2("Apparel");
        var tableRect = listing.GetRect(
            _apparelTable.HeaderHeight + (RowCount + 1) * RowHeight + UIUtility.ButtonHeight + 4f);
        _apparelTable.TableOnGUI(tableRect);
    }
}