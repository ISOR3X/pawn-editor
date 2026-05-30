using PawnEditor.Table;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor;

public class Window_Table<T> : OwnedWindow
{
    private readonly IReadOnlyList<T>? _allRows;
    private readonly IReadOnlyList<RowFilter<T>> _filters = [];
    private readonly Action<T?>? _onAdd;
    private readonly Action<TaffyBuilder, T?>? _selectedItemSlot;
    private readonly Table<T> _table;

    public Window_Table(Table<T> table, Window? owner = null,
        Action<T?>? onAdd = null, Action<TaffyBuilder, T?>? selectedItemSlot = null
    ) : base(owner)
    {
        _table = table;
        _onAdd = onAdd;
        _selectedItemSlot = selectedItemSlot;

        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
    }

    public Window_Table(Table<T> table, IReadOnlyList<T> allRows, IReadOnlyList<RowFilter<T>>? filters,
        Window? owner = null, Action<T?>? onAdd = null, Action<TaffyBuilder, T?>? selectedItemSlot = null
    ) : this(table, owner, onAdd, selectedItemSlot)
    {
        _allRows = allRows;
        _filters = filters ?? [];
    }

    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    public override void PreOpen()
    {
        base.PreOpen();
        if (_allRows == null) return;
        foreach (var filter in _filters)
            filter.Setup(_allRows, OnFilterChanged);
        OnFilterChanged();
    }

    private void OnFilterChanged()
    {
        _table.UpdateRows(_allRows!.Where(row => _filters.All(f => f.Passes(row))));
    }

    public override void DoWindowContents(Rect inRect)
    {
        Void.Taffy.Div(inRect,
            builder =>
            {
                builder.Div(builder2 =>
                {
                    if (_filters.Count > 0)
                        builder2.Div(
                            builder3 =>
                            {
                                foreach (var filter in _filters) filter.DrawFilter(builder3);
                            }, new StyleOverride
                            {
                                width = Dimension.Px(200f), flexDirection = TaffyFlexDirection.Column
                            });

                    builder2.Item(r => _table.Draw(r), new StyleOverride { flexGrow = 1f });
                }, new StyleOverride { flexGrow = 1f, gap = Void.Taffy.Gap(GenUI.Gap) });
                if (_selectedItemSlot != null || _onAdd != null)
                    builder.Div(
                        builder4 =>
                        {
                            _selectedItemSlot?.Invoke(builder4, _table.SelectedItem);
                            builder4.Button("Add", onClick: _ => _onAdd?.Invoke(_table.SelectedItem),
                                size: UIUtility.ComponentSize.Large);
                        },
                        new StyleOverride
                            { justifyContent = TaffyAlignContent.SpaceBetween, alignItems = TaffyAlignItems.Center });
            }, new StyleOverride { flexDirection = TaffyFlexDirection.Column, gap = Void.Taffy.Gap(GenUI.GapSmall) });
    }
}