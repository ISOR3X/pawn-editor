using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Void.Native;
using static Void.Native.Components;

namespace PawnEditor.v2;

/// <summary>
///     A window to pick a row from a table: the table in the middle, <see cref="RowFilter{TRow}" />s to its left, a
///     search box below it and, along the bottom, a slot for the selected row and an Add button.
///     <para>
///         The window owns the view: it filters, searches and sorts <c>rows</c> and keeps the selection. The table
///         component only draws what it is given.
///     </para>
/// </summary>
public class Window_Table<T> : OwnedWindow
{
    private const float RowHeight = 30f;
    private const float FiltersWidth = 200f;
    private const float SearchWidth = 180f;

    private static readonly Style RootStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        width = Dimension.Percent(1f),
        height = Dimension.Percent(1f),
        gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall))
    };

    private static readonly Style MainStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        // Takes what the bottom bar leaves, whatever the height of the table's rows is.
        flexGrow = 1f,
        flexBasis = Dimension.Px(0f),
        minHeight = Dimension.Px(0f),
        gap = new TaffyAxes(Dimension.Px(GenUI.Gap))
    };

    private static readonly Style FiltersStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        width = Dimension.Px(FiltersWidth),
        flexShrink = 0f
    };

    private static readonly Style TableAreaStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        flexGrow = 1f,
        minWidth = Dimension.Px(0f),
        minHeight = Dimension.Px(0f),
        gap = new TaffyAxes(Dimension.Px(GenUI.GapTiny))
    };

    private static readonly Style SearchStyle = new()
    {
        width = Dimension.Px(SearchWidth),
        height = Dimension.Px(Void.UIUtility.ButtonHeight),
        flexShrink = 0f,
        alignSelf = TaffyAlignItems.FlexEnd
    };

    private static readonly Style BottomBarStyle = new()
    {
        display = TaffyDisplay.Flex,
        justifyContent = TaffyAlignContent.SpaceBetween,
        alignItems = TaffyAlignItems.Center,
        flexShrink = 0f
    };

    private readonly IReadOnlyList<TableColumn<T>> _columns;
    private readonly IReadOnlyList<RowFilter<T>> _filters;
    private readonly Action<T>? _onAdd;
    private readonly Action<Rect, T>? _onRowHover;
    private readonly IReadOnlyList<T> _rows;
    private readonly QuickSearchWidget? _search;
    private readonly Func<T, string>? _searchText;
    private readonly Action<UIBranch, T?>? _selectedItemSlot;
    private readonly UITree _tree = new();

    private bool _hasSelection;
    private T? _selected;
    private TableSorting? _sorting;
    private List<T> _view = [];
    private bool _viewDirty = true;

    /// <param name="rows">Everything that can be picked, before filtering.</param>
    /// <param name="columns">
    ///     The columns. A column with a <see cref="TableColumn{TRow}.Sort" /> can be sorted by clicking its header.
    /// </param>
    /// <param name="owner">The window this one belongs to; it closes along with it.</param>
    /// <param name="filters">Shown in a panel to the left of the table.</param>
    /// <param name="searchText">
    ///     The text of a row that the search box looks in. Without it there is no search box.
    /// </param>
    /// <param name="onAdd">Called with the selected row when Add is clicked, which needs a selection.</param>
    /// <param name="selectedItemSlot">Shown in the bottom bar, next to Add. Gets the selected row, if there is one.</param>
    /// <param name="onRowHover">Called every frame the mouse is over a row, for tooltips.</param>
    public Window_Table(IReadOnlyList<T> rows, IReadOnlyList<TableColumn<T>> columns, Window? owner = null,
        IReadOnlyList<RowFilter<T>>? filters = null, Func<T, string>? searchText = null, Action<T>? onAdd = null,
        Action<UIBranch, T?>? selectedItemSlot = null, Action<Rect, T>? onRowHover = null) : base(owner)
    {
        _rows = rows;
        _columns = columns;
        _filters = filters ?? [];
        _searchText = searchText;
        _search = searchText != null ? new QuickSearchWidget() : null;
        _onAdd = onAdd;
        _selectedItemSlot = selectedItemSlot;
        _onRowHover = onRowHover;

        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        resizeable = true;
        doCloseX = true;
    }

    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    public override void PreOpen()
    {
        base.PreOpen();
        foreach (var filter in _filters) filter.Setup(_rows, MarkViewDirty);
    }

    public override void PostClose()
    {
        base.PostClose();
        _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (_viewDirty) RebuildView();

        _tree.Build(root =>
        {
            root.Div(main =>
            {
                if (_filters.Count > 0)
                    main.Div(panel =>
                    {
                        foreach (var filter in _filters) filter.DrawFilter(panel);
                    }, style: FiltersStyle);

                main.Div(area =>
                {
                    // The table fills what the search box leaves, so it can only guess how many rows that is at first.
                    area.Table(_view, _columns, RowHeight, Mathf.CeilToInt(inRect.height / RowHeight),
                        highlightRow: IsSelected, onRowClick: Select, onRowHover: OnRowHover,
                        sorting: _sorting, onSortingChange: SetSorting, fill: true);

                    if (_search != null)
                        area.Div(draw: r => _search.OnGUI(r, MarkViewDirty), style: SearchStyle);
                }, style: TableAreaStyle);
            }, style: MainStyle);

            if (_selectedItemSlot != null || _onAdd != null)
                root.Div(bar =>
                {
                    _selectedItemSlot?.Invoke(bar, _hasSelection ? _selected : default);
                    bar.Button("Add", size: ComponentSize.Large, disabled: !_hasSelection,
                        onClick: _ =>
                        {
                            if (_hasSelection) _onAdd?.Invoke(_selected!);
                        });
                }, style: BottomBarStyle);
        }, RootStyle);

        _tree.Draw(inRect);
    }

    private void RebuildView()
    {
        _viewDirty = false;

        var passing = _rows.Where(row => _filters.All(filter => filter.Passes(row)));

        var search = _search?.filter.Text;
        if (_searchText != null && !search.NullOrEmpty())
            passing = passing.Where(row =>
                _searchText(row)?.IndexOf(search!, StringComparison.OrdinalIgnoreCase) >= 0);

        // A new list, so a tree that was built from the old one is not changed under it.
        _view = _sorting?.Apply(passing, _columns) ?? [.. passing];
    }

    private void MarkViewDirty()
    {
        _viewDirty = true;
    }

    private void SetSorting(TableSorting? sorting)
    {
        _sorting = sorting;
        _viewDirty = true;
    }

    private bool IsSelected(T row)
    {
        return _hasSelection && EqualityComparer<T>.Default.Equals(row, _selected!);
    }

    private void Select(T row)
    {
        _selected = row;
        _hasSelection = true;
        SoundDefOf.Click.PlayOneShotOnCamera();
    }

    private void OnRowHover(Rect rect, T row)
    {
        MouseoverSounds.DoRegion(rect);
        _onRowHover?.Invoke(rect, row);
    }
}
