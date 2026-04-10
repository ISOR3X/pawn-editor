using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class SectionWorker_DefTable(SectionDef def) : SectionWorker(def)
{
    private const int MaxVisibleRows = 11;
    private const float RowHeight = 30f;

    private Table<Def>? _table;
    private Pawn? _lastPawn;

    protected abstract DefTableDef TableDef { get; }
    protected abstract string Label { get; }
    protected abstract Def DefaultDef { get; }
    protected abstract IEnumerable<Def> GetDefs();

    protected virtual Def GetDefaultSelectedDef(Pawn p) => DefaultDef;
    protected virtual bool ShowTableForPawn(Pawn pawn) => true;

    protected virtual string GetUnavailableLabel(Pawn pawn) =>
        $"No {Label.ToLower()}s available for {pawn.Name.ToStringShort}";

    /// <summary>Called when a row is selected. Override to apply the def to the pawn.</summary>
    protected virtual void OnSelectChanged(Def? def)
    {
    }

    /// <summary>Called on row hover in addition to the default tooltip. Override to add custom visuals.</summary>
    protected virtual void OnRowHover(Rect rect, Def def)
    {
    }

    /// <summary>Returns the tooltip text shown when hovering a row.</summary>
    protected virtual string GetTooltipFor(Def def)
    {
        var text = def.LabelCap.Colorize(ColoredText.TipSectionTitleColor);
        if (def.description != null)
            text += "\n\n" + def.description;
        return text;
    }

    private Table<Def> GetOrCreateTable()
    {
        if (_table != null) return _table;

        _table = TableDef.CreateTable(
            GetDefs(),
            onRowHover: (rect, def, _) =>
            {
                if (Mouse.IsOver(rect))
                    TooltipHandler.TipRegion(rect, GetTooltipFor(def));
                OnRowHover(rect, def);
            },
            onSelectChanged: OnSelectChanged,
            searchProjection: TableDef.searchColumn != null ? def => def.LabelCap.RawText : null
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
            table.Selected = GetDefaultSelectedDef(pawn);
        }

        var rowCount = Math.Clamp(table.FilteredRowCount, 1, MaxVisibleRows);
        var tableHeight = Table<Def>.HeaderHeight + rowCount * RowHeight + UIUtility.ButtonHeight + 4f;

        builder.Item(r => r.LabelH2(Label), new StyleOverride { height = Text.LineHeight });
        builder.Item(r =>
        {
            if (ShowTableForPawn(pawn))
                table.Draw(r);
            else
                using (new TextBlock(TextAnchor.MiddleCenter))
                    Verse.Widgets.Label(r, GetUnavailableLabel(pawn).Colorize(ColoredText.SubtleGrayColor));
        }, new StyleOverride { height = tableHeight });
    }
}