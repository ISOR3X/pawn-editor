using HotSwap;
using PawnEditor.Table;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;
using Display = PawnEditor.TaffySharp.Display;

namespace PawnEditor;

[HotSwappable]
public class Window_Table<T>(
    Table<T> table,
    Pawn pawn) : Window
{
    public override Vector2 InitialSize => Page.StandardSize - new Vector2(128f, 128f);

    public override void DoWindowContents(Rect inRect)
    {
        _ = pawn;
        Taffy.Div(inRect, new Style { flexDirection = FlexDirection.Column, gap = Taffy.Gap(GenUI.GapSmall) },
            builder =>
            {
                builder.Div(new Style { flexGrow = 1f, gap = Taffy.Gap(GenUI.GapSmall) }, builder2 =>
                {
                    if (table.Filters.Count > 0)
                    {
                        builder2.Div(
                            new Style
                            {
                                size = new Size<Dimension>(200f, Dimension.AUTO), display = Display.Grid,
                                gridAutoRows = [TrackSizingFunction.Px(UIUtility.ButtonHeight)],
                                gap = Taffy.Gap(GenUI.GapTiny)
                            }, build: builder3 =>
                            {
                                foreach (var filter in table.Filters)
                                {
                                    filter.DrawFilter(builder3, table);
                                }
                            });
                    }

                    builder2.Item(new Style { flexGrow = 1f }, draw: table.Draw);
                });
                builder.Div(new Style { justifyContent = AlignContent.SpaceBetween }, builder4 =>
                {
                    // builder4.Text(table.Selected?.ToString() ?? "No item selected");
                    builder4.Button("Add");
                });
            });
    }
}