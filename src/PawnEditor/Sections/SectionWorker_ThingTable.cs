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
    protected virtual void OnSelectChanged(Thing? thing) { }

    /// <summary>Called on row hover in addition to the default tooltip.</summary>
    protected virtual void OnRowHover(Rect rect, Thing thing) { }

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
        var table = GetOrCreateTable();

        if (_lastPawn != pawn)
        {
            _lastPawn = pawn;
            table.SetDirty();
        }

        var rowCount = Math.Clamp(table.FilteredRowCount, 1, MaxVisibleRows);
        var tableHeight = Table<Thing>.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f;

        builder.Item(height: Text.LineHeight, draw: r => r.LabelH2(Label));
        builder.Item(height: tableHeight, draw: r => table.Draw(r));
        builder.Item(height: UIUtility.ButtonHeight, draw: DrawFooter);
    }

    protected virtual void DrawFooter(Rect r)
    {
        if (Verse.Widgets.ButtonText(r.TakeLeftPart(100f), "Add item"))
        {
            Find.WindowStack.Add(new Window_AddItem(
                TableDefOf.PawnEditor_DefTable_ThingDef,
                () => DefDatabase<ThingDef>.AllDefs
                    .Where(td => td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult))
                    .Cast<Def>(),
                [
                    ("Content source", () => new DefTableFilter_ContentSource()),
                    ("Stuff category", () => new DefTableFilter_StuffCategory())
                ]
            ));
        }
    }
}
