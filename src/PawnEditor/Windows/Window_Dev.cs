using FlexLayout;
using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class Window_Dev : Window
{
    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    public override void DoWindowContents(Rect inRect)
    {
        DrawDynamicLayout(inRect);
    }
    

    private static readonly Color[] ItemColors =
    {
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.cyan, Color.magenta, Color.white, Color.grey,
    };

    private int _itemCount = 3;

    public void DrawDynamicLayout(Rect inRect)
    {
        Flex.Column(inRect, gap: 8f, col =>
        {
            col.Row(ElementStyle.FixedHeight(40f), gap: 8f, build: row =>
            {
                row.Button("+ Add",
                    onClick: () => _itemCount = Mathf.Min(_itemCount + 1, 50),
                    style: ElementStyle.FixedWidth(100f));
                row.Button("- Remove",
                    onClick: () => _itemCount = Mathf.Max(_itemCount - 1, 0),
                    style: ElementStyle.FixedWidth(100f));
                row.Label($"Items: {_itemCount}", ElementStyle.Fill);
            });

            col.ScrollView(ElementStyle.Fill, gap: 4f, build: inner =>
            {
                for (int i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    inner.Item(ElementStyle.FixedHeight(100f),
                        draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
        });
    }
}