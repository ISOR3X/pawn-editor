using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class Window_AddItem : Window
{
    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    private readonly IReadOnlyList<(string Label, Func<TableFilter> Factory)> _availableFilters;
    private readonly List<TableFilter> _activeFilters = [];

    // The filter adapters list is kept in sync with _activeFilters and passed to the table.
    private readonly List<IRowFilter<Def>> _filterAdapters = [];

    private readonly Table<Def> _table;

    public Window_AddItem(
        DefTableDef tableDef,
        Func<IEnumerable<Def>> rowsGetter,
        IEnumerable<(string Label, Func<TableFilter> Factory)>? availableFilters = null)
    {
        _availableFilters = availableFilters?.ToList() ?? [];
        _table = tableDef.CreateTable(
            rowsGetter(),
            filters: _filterAdapters,
            searchProjection: def => def.LabelCap.RawText
        );
    }

    public override void DoWindowContents(Rect inRect)
    {
        var footerRect = inRect.TakeBottomPart(UIUtility.ButtonHeight);
        inRect.Gap();
        var leftRect = inRect.TakeLeftPart(200f);
        inRect.Indent();

        var listing = new Listing_Standard();
        listing.Begin(leftRect);
        if (listing.ButtonText("Add filter"))
        {
            var opts = _availableFilters
                .Select(f => new FloatMenuOption(f.Label, () =>
                {
                    var filter = f.Factory();
                    _activeFilters.Add(filter);
                    _filterAdapters.Add(new FilterAdapter(filter));
                    _table.SetDirty();
                }))
                .ToList();
            if (opts.Count > 0) Find.WindowStack.Add(new FloatMenu(opts));
            else Messages.Message("No filters available", MessageTypeDefOf.RejectInput);
        }

        foreach (var filter in _activeFilters.ToList())
            filter.DrawFilter(listing, _table.SetDirty, () =>
            {
                var idx = _activeFilters.IndexOf(filter);
                _activeFilters.Remove(filter);
                _filterAdapters.RemoveAt(idx);
                _table.SetDirty();
            });
        listing.End();

        _table.Draw(inRect);

        Verse.Widgets.Label(footerRect, _table.Selected?.LabelCap ?? "No item selected");
    }

    private sealed class FilterAdapter(TableFilter filter) : IRowFilter<Def>
    {
        public bool Passes(Def row, ITableContext? ctx) => filter.Matches(row);
        public void DrawFilter(TaffyBuilder builder, Table<Def> table) { }
    }
}
