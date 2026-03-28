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

    private Pawn? _lastPawn;

    protected abstract DefTableDef TableDef { get; }
    protected abstract string Label { get; }
    protected abstract Def DefaultDef { get; }
    protected abstract IEnumerable<Def> GetDefs();

    protected virtual Def GetDefaultSelectedDef(Pawn p) => DefaultDef;

    private DefTableWorker Table => field ??= (DefTableWorker)Activator.CreateInstance(
        TableDef.workerClass, TableDef, (Func<IEnumerable<Def>>)GetDefs, DefaultDef);

    protected virtual bool ShowTableForPawn(Pawn pawn) => true;

    protected virtual string GetUnavailableLabel(Pawn pawn) =>
        $"No {Label.ToLower()}s available for {pawn.Name.ToStringShort}";

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        if (_lastPawn != pawn)
        {
            _lastPawn = pawn;
            Table.SetDirty();
            Table.Selected = GetDefaultSelectedDef(pawn);
        }

        var rowCount = Math.Clamp(Table.ThingListForReading.Count, 1, RowCount);
        var tableHeight = Table.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f;

        col.Item(height: Text.LineHeight, draw: r => r.LabelH2(Label));
        col.Item(height: tableHeight, draw: r =>
        {
            if (ShowTableForPawn(pawn))
                Table.TableOnGUI(r);
            else
                using (new TextBlock(TextAnchor.MiddleCenter))
                    Verse.Widgets.Label(r, GetUnavailableLabel(pawn).Colorize(ColoredText.SubtleGrayColor));
        });
    }
}
