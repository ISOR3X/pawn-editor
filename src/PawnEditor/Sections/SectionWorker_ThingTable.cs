using System;
using System.Collections.Generic;
using HotSwap;
using PawnEditor.Extensions;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_ThingTable(SectionDef def) : SectionWorker(def)
{
    private const int RowCount = 11;
    private const float RowHeight = 30f;

    private Pawn? _lastPawn;

    protected abstract ThingTableDef TableDef { get; }
    protected abstract string Label { get; }
    protected abstract IEnumerable<Thing> GetThings();

    private ThingTableWorker Table => field ??= (ThingTableWorker)Activator.CreateInstance(
        TableDef.workerClass, TableDef, (Func<IEnumerable<Thing>>)GetThings, null);

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        if (_lastPawn != pawn)
        {
            _lastPawn = pawn;
            Table.SetDirty();
        }

        listing.LabelH2(Label);
        var rowCount = Math.Clamp(Table.ThingListForReading.Count, 1, RowCount);
        var tableRect = listing.GetRect(
            Table.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f);

        Table.TableOnGUI(tableRect);
    }
}