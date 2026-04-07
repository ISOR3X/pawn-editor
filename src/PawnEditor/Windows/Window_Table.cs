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
    private readonly Pawn _pawn;

    public Window_Table(Table<T> table,
        Pawn pawn,
        Window? owner = null) : base(owner)
    {
        _table = table;
        _pawn = pawn;
        // TODO: FIXME - Why does close on click outside not work?
        closeOnClickedOutside = true;
    }

    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    public override void DoWindowContents(Rect inRect)
    {
        Taffy.Div(inRect, new Style { flexDirection = FlexDirection.Column, gap = Taffy.Gap(GenUI.GapSmall) },
            builder =>
            {
                builder.Div(new Style { flexGrow = 1f, gap = Taffy.Gap(GenUI.GapSmall) }, builder2 =>
                {
                    if (_table.Filters.Count > 0)
                    {
                        builder2.Div(
                            new Style
                            {
                                size = new Size<Dimension>(200f, Dimension.AUTO), flexDirection = FlexDirection.Column,
                                gap = Taffy.Gap(GenUI.GapSmall)
                            }, build: builder3 =>
                            {
                                foreach (var filter in _table.Filters)
                                {
                                    filter.DrawFilter(builder3, _table);
                                }
                            });
                    }

                    builder2.Item(new Style { flexGrow = 1f }, draw: _table.Draw);
                });
                builder.Div(new Style { justifyContent = AlignContent.SpaceBetween }, builder4 =>
                {
                    // builder4.Text(table.Selected?.ToString() ?? "No item selected");
                    builder4.Button("Add");
                });
            });
    }
}