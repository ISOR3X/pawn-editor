using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_ThingTable(SectionDef def) : SectionWorker(def)
{
    private const int MaxVisibleRows = 11;
    private const float RowHeight = 30f;

    private Table<Thing>? _table;
    private Pawn? _lastPawn;

    protected abstract ThingTableDef TableDef { get; }
    protected abstract string Label { get; }
    protected abstract IEnumerable<Thing> GetThings();

    /// <summary>Called when a row is selected.</summary>
    protected virtual void OnSelectChanged(Thing? thing)
    {
    }

    /// <summary>Called on row hover in addition to the default tooltip.</summary>
    protected virtual void OnRowHover(Rect rect, Thing thing)
    {
    }

    private Table<Thing> GetOrCreateTable()
    {
        if (_table != null) return _table;

        _table = TableDef.CreateTable(
            GetThings(),
            onRowHover: (rect, thing, _) =>
            {
                if (Mouse.IsOver(rect))
                    TooltipHandler.TipRegion(rect, thing.GetTooltip());
                OnRowHover(rect, thing);
            },
            onSelectChanged: OnSelectChanged,
            searchProjection: TableDef.searchColumn != null ? t => t.LabelCap : null
        );
        return _table;
    }

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        if (_lastPawn != pawn)
        {
            _lastPawn = pawn;
            _table = null; // recreate so GetThings() is called with the new pawn
        }

        var table = GetOrCreateTable();

        var rowCount = Math.Clamp(table.FilteredRowCount, 1, MaxVisibleRows);
        var tableHeight = Table<Thing>.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f;

        // builder.Item(height: Text.LineHeight, draw: r => r.LabelH2(Label));
        builder.Item(table.Draw, new StyleOverride { height = tableHeight });
        // builder.Item(height: UIUtility.ButtonHeight, draw: DrawFooter);
    }

    protected virtual void DrawFooter(Rect r)
    {
        if (Verse.Widgets.ButtonText(r.TakeLeftPart(100f), "Add item"))
        {
        }
    }
}