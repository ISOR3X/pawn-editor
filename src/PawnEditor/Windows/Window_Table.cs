using PawnEditor.Table;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

public class Window_Table<T> : OwnedWindow
{
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

    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    public override void DoWindowContents(Rect inRect)
    {
        Void.Taffy.Div(inRect,
            builder =>
            {
                builder.Div(builder2 =>
                {
                    if (_table.Filters.Count > 0)
                        builder2.Div(
                            builder3 =>
                            {
                                foreach (var filter in _table.Filters) filter.DrawFilter(builder3, _table);
                            }, new StyleOverride
                            {
                                width = 200f, flexDirection = FlexDirection.Column
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
                            { justifyContent = AlignContent.SpaceBetween, alignItems = AlignItems.Center });
            }, new StyleOverride { flexDirection = FlexDirection.Column, gap = Void.Taffy.Gap(GenUI.GapSmall) });
    }
}