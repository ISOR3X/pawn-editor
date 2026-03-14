using System;
using System.Collections.Generic;
using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_DefTable(SectionDef def) : SectionWorker(def)
{
    private const int RowCount = 11;
    private const float RowHeight = 30f;

    protected abstract DefTableDef TableDef { get; }
    protected abstract string Label { get; }
    protected abstract Def? DefaultDef { get; }
    protected abstract IEnumerable<Def> GetDefs();

    protected DefTableWorker Table => field ??= (DefTableWorker)Activator.CreateInstance(
        TableDef.workerClass, TableDef, (Func<IEnumerable<Def>>)GetDefs, DefaultDef);
    
    protected virtual bool CanShowTable(Pawn pawn) => Table.ThingListForReading.Count > 0;

    protected virtual string GetUnavailableLabel(Pawn pawn) =>
        $"No {Label.ToLower()} available for {pawn.Name.ToStringShort}";

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        listing.LabelH2(Label);
        var rowCount = Math.Min(Table.ThingListForReading.Count, RowCount);
        var tableRect = listing.GetRect(
            Table.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f);

        if (CanShowTable(pawn))
            Table.TableOnGUI(tableRect);
        else
            using (new TextBlock(TextAnchor.MiddleCenter))
                Verse.Widgets.Label(tableRect, GetUnavailableLabel(pawn).Colorize(ColoredText.SubtleGrayColor));
    }
}