using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

[StaticConstructorOnStartup]
public class RowFilter_Style : IRowFilter<ThingDef>
{
    private readonly HashSet<StyleCategoryDef> _disabledStyleCategories = [];
    private List<StyleCategoryDef>? _styleCategories;
    private bool _noneDisabled;

    private string _searchText = "";

    private static readonly Dictionary<ThingDef, List<StyleCategoryDef>> ThingDefByCategory = [];

    public RowFilter_Style()
    {
        foreach (var styleCategoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
        foreach (var thingDefStyle in styleCategoryDef.thingDefStyles)
        {
            if (thingDefStyle.ThingDef == null) continue;
            if (!ThingDefByCategory.TryGetValue(thingDefStyle.ThingDef, out var list))
                ThingDefByCategory[thingDefStyle.ThingDef] = list = [];
            list.Add(styleCategoryDef);
        }
    }

    public bool Passes(ThingDef row, IContext? ctx)
    {
        if (!ThingDefByCategory.TryGetValue(row, out var categories))
            return !_noneDisabled;
        if (_disabledStyleCategories.Count == 0) return true;
        return categories.Any(c => !_disabledStyleCategories.Contains(c));
    }

    public void DrawFilter(TaffyBuilder builder, Table<ThingDef> table)
    {
        _styleCategories ??=
        [
            .. table.Rows
                .Where(t => ThingDefByCategory.ContainsKey(t) && ThingDefByCategory[t].Count > 0)
                .SelectMany(t => ThingDefByCategory[t])
                .Distinct()
                .OrderBy(c => c.label)
        ];

        builder.Collapsible("Style", col =>
        {
            col.Div(row2 =>
            {
                row2.Icon(TexButton.Search);
                row2.Input(ref _searchText, style: new StyleOverride { flexGrow = 1f });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });

            // null sentinel represents the "None" entry pinned at the top
            var filtered = new List<StyleCategoryDef?> { null };
            filtered.AddRange(_styleCategories
                .Where(c => _searchText.NullOrEmpty() ||
                            c.label.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0));

            col.List(filtered, (rect, category) =>
            {
                if (category == null)
                {
                    var selected = !_noneDisabled;
                    var prev = selected;
                    Verse.Widgets.CheckboxLabeled(rect, "None", ref selected);
                    if (selected == prev) return;
                    _noneDisabled = !selected;
                }
                else
                {
                    var selected = !_disabledStyleCategories.Contains(category);
                    var prev = selected;
                    Verse.Widgets.CheckboxLabeled(rect, category.LabelCap, ref selected);
                    if (selected == prev) return;
                    if (selected) _disabledStyleCategories.Remove(category);
                    else _disabledStyleCategories.Add(category);
                }
                table.SetDirty();
            }, maxItemsVisibleAtOnce: 6);

            col.Div(row =>
            {
                row.Button("Enable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    _disabledStyleCategories.Clear();
                    _noneDisabled = false;
                    table.SetDirty();
                });
                row.Button("Disable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    if (_styleCategories != null)
                        foreach (var c in _styleCategories)
                            _disabledStyleCategories.Add(c);
                    _noneDisabled = true;
                    table.SetDirty();
                });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });
        }, style: new StyleOverride { gap = Void.Taffy.Gap(0f, GenUI.GapTiny) });
    }
}