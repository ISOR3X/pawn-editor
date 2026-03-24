using HotSwap;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class Window_Dev : Window
{
    public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight) - new Vector2(128f, 128f);

    public Window_Dev()
    {
        resizeable = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // DrawGridLayout(inRect);
        DrawFlexLayout(inRect);
    }


    private static readonly Color[] ItemColors =
    [
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.cyan, Color.magenta, Color.white, Color.grey
    ];

    private int _itemCount = 3;

    private void DrawFlexLayout(Rect inRect)
    {
        Taffy.Column(inRect, col =>
        {
            col.Row(gap: 8f, grow: 0f, row =>
            {
                row.Item(width: 100f, height: 40f, draw: rect => Verse.Widgets.DrawRectFast(rect, Color.red));
                row.Item(width: 80f, height: 40f, draw: rect =>
                {
                    if (Verse.Widgets.ButtonText(rect, "+ Add")) _itemCount = Mathf.Min(_itemCount + 1, 50);
                });
                row.Item(width: 90f, height: 40f, draw: rect =>
                {
                    if (Verse.Widgets.ButtonText(rect, "- Remove")) _itemCount = Mathf.Max(_itemCount - 1, 0);
                });
                row.Item(grow: 1f, height: 40f, draw: rect => Verse.Widgets.Label(rect, $"Items: {_itemCount}"));
                row.Item(width: 100f, height: 40f, draw: rect => Verse.Widgets.DrawRectFast(rect, Color.gray));
            });
            col.Row(new Style { flexGrow = 1f, flexWrap = FlexWrap.Wrap }, row =>
            {
                for (var i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    row.Item(width: 100f, height: 100f, draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
            col.Row(new Style { flexWrap = FlexWrap.Wrap }, row =>
            {
                for (var i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    row.Item(width: 50f, height: 50f, draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
        });
    }

    private void DrawGridLayout(Rect inRect)
    {
        // 3-column grid: [1fr, 2fr, 1fr], 8px gap
        Taffy.Grid(inRect, [Taffy.Fr(), Taffy.Fr(2), Taffy.Fr()], gap: 8f, autoRowHeight: 40f, grid =>
        {
            // Row 1: three independent cells
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.red);
                Verse.Widgets.Label(r, "A (1fr)");
            });
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.green);
                Verse.Widgets.Label(r, "B (2fr)");
            });
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.blue);
                Verse.Widgets.Label(r, "C (1fr)");
            });

            // Row 2: one cell spanning all 3 columns
            grid.GridItem(colSpan: 3,
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.yellow);
                    Verse.Widgets.Label(r, "D — colSpan 3");
                });

            // Row 3: a 2-wide cell then a 1-wide cell
            grid.GridItem(colSpan: 2,
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.cyan);
                    Verse.Widgets.Label(r, "E — colSpan 2");
                });
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.magenta);
                Verse.Widgets.Label(r, "F");
            });

            // Row 4: G locked to col 2 (colStart=2); cursor was past col 2 so row advances.
            //         H auto-placed at col 3 (cursor continues forward). Col 1 left empty (sparse).
            grid.GridItem(colStart: 2,
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.grey);
                    Verse.Widgets.Label(r, "G (start=2)");
                });
            grid.GridItem(
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.white);
                    Verse.Widgets.Label(r, "H");
                });
        });
    }
}