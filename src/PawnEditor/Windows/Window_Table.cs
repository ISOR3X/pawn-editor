using HotSwap;
using PawnEditor.Table;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

[HotSwappable]
public class Window_Table<T> : OwnedWindow
{
    private readonly Table<T> _table;
    private readonly Action<T?>? _onAdd;
    private readonly Action<TaffyBuilder, T?>? _selectedItemSlot;

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
        Taffy.Div(inRect,
            builder =>
            {
                builder.Div(new Style { flexGrow = 1f, gap = Taffy.Gap(GenUI.Gap) }, builder2 =>
                {
                    if (_table.Filters.Count > 0)
                    {
                        builder2.Div(
                            new Style
                            {
                                size = new Size<Dimension>(200f, Dimension.AUTO), flexDirection = FlexDirection.Column,
                            }, build: builder3 =>
                            {
                                foreach (var filter in _table.Filters)
                                {
                                    filter.DrawFilter(builder3, _table);
                                }
                            });
                    }

                    builder2.Item(_table.Draw, new StyleOverride { flexGrow = 1f });
                });
                if (_selectedItemSlot != null || _onAdd != null)
                    builder.Div(
                        new Style { justifyContent = AlignContent.SpaceBetween, alignItems = AlignItems.Center },
                        builder4 =>
                        {
                            _selectedItemSlot?.Invoke(builder4, _table.SelectedItem);
                            builder4.Button("Add", onClick: _ => _onAdd?.Invoke(_table.SelectedItem),
                                size: UIUtility.ComponentSize.Large);
                        });
            }, new StyleOverride { flexDirection = FlexDirection.Column, gap = Taffy.Gap(GenUI.GapSmall) });
    }
}