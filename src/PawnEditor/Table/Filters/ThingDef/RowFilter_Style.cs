using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

[StaticConstructorOnStartup]
public class RowFilter_Style : RowFilter<ThingDef>
{
    private readonly HashSet<StyleCategoryDef> _disabledStyleCategories = [];
    private List<StyleCategoryDef> _styleCategories = [];
    private bool _noneDisabled;

    private string _searchText = "";

    private static readonly Dictionary<ThingDef, List<StyleCategoryDef>> ThingDefByCategory;

    static RowFilter_Style()
    {
        ThingDefByCategory = [];
        foreach (var styleCategoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
        foreach (var thingDefStyle in styleCategoryDef.thingDefStyles)
        {
            if (thingDefStyle.ThingDef == null) continue;
            if (!ThingDefByCategory.TryGetValue(thingDefStyle.ThingDef, out var list))
                ThingDefByCategory[thingDefStyle.ThingDef] = list = [];
            list.Add(styleCategoryDef);
        }
    }

    protected override void Initialize(IReadOnlyList<ThingDef> allRows)
    {
        _styleCategories = [.. allRows
            .Where(t => ThingDefByCategory.ContainsKey(t) && ThingDefByCategory[t].Count > 0)
            .SelectMany(t => ThingDefByCategory[t])
            .Distinct()
            .OrderBy(c => c.label)];
    }

    public override bool Passes(ThingDef row)
    {
        if (!ThingDefByCategory.TryGetValue(row, out var categories))
            return !_noneDisabled;
        if (_disabledStyleCategories.Count == 0) return true;
        return categories.Any(c => !_disabledStyleCategories.Contains(c));
    }

    public override void DrawFilter(TaffyBuilder builder)
    {
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
                MarkDirty();
            }, maxItemsVisibleAtOnce: 6);

            col.Div(row =>
            {
                row.Button("Enable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    _disabledStyleCategories.Clear();
                    _noneDisabled = false;
                    MarkDirty();
                });
                row.Button("Disable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    foreach (var c in _styleCategories) _disabledStyleCategories.Add(c);
                    _noneDisabled = true;
                    MarkDirty();
                });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });
        }, style: new StyleOverride { gap = Void.Taffy.Gap(0f, GenUI.GapTiny) });
    }
}
