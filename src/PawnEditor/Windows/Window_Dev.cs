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
        DrawDynamicLayout(inRect);
    }


    private static readonly Color[] ItemColors =
    [
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.cyan, Color.magenta, Color.white, Color.grey
    ];

    private int _itemCount = 3;

    private void DrawDynamicLayout(Rect inRect)
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
}