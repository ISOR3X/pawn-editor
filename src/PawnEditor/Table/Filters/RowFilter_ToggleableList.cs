using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

public abstract class RowFilter_ToggleableList<T, K>(string title, Func<K, string> searchProjection, List<K> options)
    : RowFilter<T> where K : class where T : class
{
    private readonly HashSet<K> _disabledOptions = [];
    protected List<K> allOptions = options;
    private bool _noneDisabled;

    private string _searchText = "";

    protected abstract IReadOnlyList<K>? GetOptions(T row);

    public override bool Passes(T row)
    {
        var options = GetOptions(row);
        if (options == null) return !_noneDisabled;
        return _disabledOptions.Count == 0 || options.Any(c => !_disabledOptions.Contains(c));
    }

    public override void DrawFilter(TaffyBuilder builder)
    {
        builder.Collapsible(title, col =>
        {
            col.Div(row2 =>
            {
                row2.Icon(TexButton.Search);
                row2.Input(ref _searchText, style: new StyleOverride { flexGrow = 1f });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });

            // null sentinel represents the "None" entry pinned at the top
            var filtered = new List<K?> { null };
            filtered.AddRange(allOptions
                .Where(c => _searchText.NullOrEmpty() ||
                            searchProjection(c).IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0));

            col.List(filtered, (rect, category) =>
            {
                var selected = category != null ? !_disabledOptions.Contains(category) : !_noneDisabled;
                var prev = selected;
                Verse.Widgets.CheckboxLabeled(rect, category != null ? searchProjection(category) : "None",
                    ref selected);

                if (prev == selected) return;
                if (category == null)
                {
                    _noneDisabled = !selected;
                    MarkDirty();
                }
                else
                {
                    if (selected) _disabledOptions.Remove(category);
                    else _disabledOptions.Add(category);
                }

                MarkDirty();
            }, maxItemsVisibleAtOnce: 6);

            col.Div(row =>
            {
                row.Button("Enable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    _disabledOptions.Clear();
                    _noneDisabled = false;
                    MarkDirty();
                });
                row.Button("Disable all", size: UIUtility.ComponentSize.Small, block: true, onClick: _ =>
                {
                    foreach (var c in allOptions) _disabledOptions.Add(c);
                    _noneDisabled = true;
                    MarkDirty();
                });
            }, new StyleOverride { gap = Void.Taffy.Gap(GenUI.GapTiny) });
        }, style: new StyleOverride { gap = Void.Taffy.Gap(0f, GenUI.GapTiny) });
    }
}