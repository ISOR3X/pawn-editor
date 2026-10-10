using UnityEngine;
using Verse;
using Void.Native;
using static Void.Native.Components;

namespace PawnEditor.v2;

/// <summary>
///     A list of options to switch on and off, with a search box. A row passes if it has at least one option that is
///     switched on, and rows without options pass while "None" is on.
/// </summary>
public abstract class RowFilter_ToggleableList<T, K> : RowFilter<T> where K : class where T : class
{
    private static readonly Style GapStyle = new() { gap = new TaffyAxes(Dimension.Px(GenUI.GapTiny)) };

    private static readonly Style SearchStyle = new() { flexGrow = 1f };

    private static readonly Style ContentStyle = new()
    {
        gap = new TaffyAxes(Dimension.Px(0f), Dimension.Px(GenUI.GapTiny))
    };

    private readonly HashSet<K> _disabledOptions = [];
    private readonly Func<K, string> _searchProjection;
    private readonly string _title;
    private bool _noneDisabled;

    private string _searchText = "";
    protected List<K> allOptions;

    protected RowFilter_ToggleableList(string title, Func<K, string> searchProjection, List<K> options)
    {
        _title = title;
        _searchProjection = searchProjection;
        allOptions = options;
    }

    protected abstract IReadOnlyList<K>? GetOptions(T row);

    public override bool Passes(T row)
    {
        var options = GetOptions(row);
        if (options == null) return !_noneDisabled;
        return _disabledOptions.Count == 0 || options.Any(c => !_disabledOptions.Contains(c));
    }

    public override void DrawFilter(UIBranch builder)
    {
        builder.Collapsible(_title, col =>
        {
            col.Div(row =>
            {
                row.Icon(TexButton.Search);
                row.Input(_searchText, value => _searchText = value, style: SearchStyle);
            }, style: GapStyle);

            // null stands for the "None" entry pinned at the top
            var filtered = new List<K?> { null };
            filtered.AddRange(allOptions
                .Where(c => _searchText.NullOrEmpty() ||
                            _searchProjection(c).IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0));

            // The checkboxes are drawn by the rows themselves, so the rows have no content.
            col.List(filtered, (_, _) => { }, maxItemsVisibleAtOnce: 6,
                rowDraw: (category, _) => rect => DrawOption(rect, category));

            col.Div(row =>
            {
                row.Button("Enable all", size: ComponentSize.Small, block: true, onClick: _ =>
                {
                    _disabledOptions.Clear();
                    _noneDisabled = false;
                    MarkDirty();
                });
                row.Button("Disable all", size: ComponentSize.Small, block: true, onClick: _ =>
                {
                    foreach (var c in allOptions) _disabledOptions.Add(c);
                    _noneDisabled = true;
                    MarkDirty();
                });
            }, style: GapStyle);
        }, style: ContentStyle);
    }

    private void DrawOption(Rect rect, K? category)
    {
        var selected = category != null ? !_disabledOptions.Contains(category) : !_noneDisabled;
        var previous = selected;
        Verse.Widgets.CheckboxLabeled(rect, category != null ? _searchProjection(category) : "None", ref selected);

        if (previous == selected) return;
        if (category == null)
        {
            _noneDisabled = !selected;
        }
        else
        {
            if (selected) _disabledOptions.Remove(category);
            else _disabledOptions.Add(category);
        }

        MarkDirty();
    }
}
