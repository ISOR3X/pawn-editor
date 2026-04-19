using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table.Filters.BackstoryDef;

public class RowFilter_SpawnCategory : IRowFilter<RimWorld.BackstoryDef>
{
    private readonly HashSet<string> _disabledCategories = [];
    private string _searchText = "";

    private List<string>? SpawnCategories;

    public bool Passes(RimWorld.BackstoryDef row, IContext? ctx)
    {
        return _disabledCategories.Count == 0 ||
               _disabledCategories.All(c => !row.spawnCategories.Contains(c));
    }

    public void DrawFilter(TaffyBuilder builder, Table<RimWorld.BackstoryDef> table)
    {
        // TODO: This should probably be done in the constructor?
        SpawnCategories ??= table.Rows.SelectMany(bd => bd.spawnCategories).Distinct().OrderBy(s => s).ToList();

        builder.Collapsible("Spawn category", col =>
        {
            col.Div(row2 =>
            {
                row2.Icon(TexButton.Search);
                row2.Input(ref _searchText, style: new StyleOverride { flexGrow = 1f });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });

            var filtered = SpawnCategories
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
                table.SetDirty();
            });

            col.Div(row =>
            {
                row.Button("Enable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    _disabledCategories.Clear();
                    table.SetDirty();
                });
                row.Button("Disable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    foreach (var c in SpawnCategories) _disabledCategories.Add(c);
                    table.SetDirty();
                });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });
        }, style: new StyleOverride { gap = Void.Taffy.Gap(0f, GenUI.GapTiny) });
    }
}