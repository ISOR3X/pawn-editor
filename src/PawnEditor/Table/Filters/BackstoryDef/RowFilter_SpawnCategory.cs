using RimWorld;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

public class RowFilter_SpawnCategory : RowFilter<BackstoryDef>
{
    private readonly HashSet<string> _disabledCategories = [];
    private string _searchText = "";
    private List<string> _spawnCategories = [];

    protected override void Initialize(IReadOnlyList<RimWorld.BackstoryDef> allRows)
    {
        _spawnCategories = [.. allRows.SelectMany(bd => bd.spawnCategories).Distinct().OrderBy(s => s)];
    }

    public override bool Passes(RimWorld.BackstoryDef row)
    {
        return _disabledCategories.Count == 0 ||
               _disabledCategories.All(c => !row.spawnCategories.Contains(c));
    }

    public override void DrawFilter(TaffyBuilder builder)
    {
        builder.Collapsible("Spawn category", col =>
        {
            col.Div(row2 =>
            {
                row2.Icon(TexButton.Search);
                row2.Input(ref _searchText, style: new StyleOverride { flexGrow = 1f });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });

            var filtered = _spawnCategories
                .Where(c => _searchText.NullOrEmpty() ||
                            c.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            col.List(filtered, (rect, category) =>
            {
                var selected = !_disabledCategories.Contains(category);
                var prev = selected;
                Verse.Widgets.CheckboxLabeled(rect, category.ReadableCamelCase(), ref selected);
                if (selected == prev) return;
                if (selected) _disabledCategories.Remove(category);
                else _disabledCategories.Add(category);
                MarkDirty();
            }, maxItemsVisibleAtOnce: 6);

            col.Div(row =>
            {
                row.Button("Enable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    _disabledCategories.Clear();
                    MarkDirty();
                });
                row.Button("Disable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    foreach (var c in _spawnCategories) _disabledCategories.Add(c);
                    MarkDirty();
                });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });
        }, style: new StyleOverride { gap = Void.Taffy.Gap(0f, GenUI.GapTiny) });
    }
}
