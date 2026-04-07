using HotSwap;
using RimWorld;
using Taffy;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public class RowFilter_BackstoryDefSpawnCategory : IRowFilter<BackstoryDef>
{
    private readonly HashSet<string> _selectedCategories = [];
    private string _searchText = "";

    private static readonly List<string> SpawnCategories =
        DefDatabase<BackstoryDef>.AllDefs.SelectMany(bd => bd.spawnCategories).Distinct().OrderBy(s => s).ToList();

    public bool Passes(BackstoryDef row, ITableContext? ctx) =>
        _selectedCategories.Count == 0 ||
        _selectedCategories.All(c => row.spawnCategories.Contains(c));

    public void DrawFilter(TaffyBuilder builder, Table<BackstoryDef> table)
    {
        builder.Collapsible("Spawn category", col =>
        {
            col.Input(ref _searchText,
                style: new Style { size = new Size<Dimension>(Dimension.Percent(1f), Dimension.AUTO) });

            var filtered = SpawnCategories
                .Where(c => _searchText.NullOrEmpty() ||
                            c.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            col.List(filtered, (rect, category) =>
            {
                var selected = _selectedCategories.Contains(category);
                var prev = selected;
                Verse.Widgets.CheckboxLabeled(rect, category.ReadableCamelCase(), ref selected);
                if (selected == prev) return;
                if (selected) _selectedCategories.Add(category);
                else _selectedCategories.Remove(category);
                table.SetDirty();
            });

            col.Row(new Style { gap = Taffy.Gap(GenUI.GapTiny) }, row =>
            {
                row.Button("Enable all", block: true, onClick: _ =>
                    {
                        foreach (var c in SpawnCategories) _selectedCategories.Add(c);
                        table.SetDirty();
                    });
                row.Button("Disable all", block: true, onClick: _ =>
                {
                    _selectedCategories.Clear();
                    table.SetDirty();
                });
            });
        });
    }
}