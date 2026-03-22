using FlexLayout;
using HotSwap;
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
        Flex.Column(inRect, col =>
        {
            col.Row(ElementStyle.FixedHeight(40f), gap: 8f, build: row =>
            {
                row.Button("+ Add",
                    onClick: () => _itemCount = Mathf.Min(_itemCount + 1, 50));
                row.Button("- Remove",
                    onClick: () => _itemCount = Mathf.Max(_itemCount - 1, 0));
                row.Label($"Items: {_itemCount}", ElementStyle.Fill);
            });
            col.Row(ElementStyle.Default().With(flexWrap: FlexWrap.Wrap, height: 40f), wrap: FlexWrap.Wrap,
                build: row2 =>
                {
                    for (var i = 0; i < 5; i++)
                    {
                        row2.Label($"Item {i}", new ElementStyle(flexBasis: 200f, maxWidth: 400f, minWidth: 100f));
                    }
                }   
            );
            // col.ScrollView(ElementStyle.Fill.With(flexDirection: FlexDirection.Row, columnGap: 4f, rowGap: 4f),
            //     build: inner =>
            //     {
            //         for (var i = 0; i < _itemCount; i++)
            //         {
            //             var color = ItemColors[i % ItemColors.Length];
            //             inner.Item(ElementStyle.FixedWidth(100f).With(height: 100f),
            //                 draw: rect => Verse.Widgets.DrawRectFast(rect, color));
            //         }
            //     });
            col.Row(ElementStyle.Default(), wrap: FlexWrap.Wrap, build: inner =>
            {
                for (var i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    inner.Item(ElementStyle.FixedWidth(100f).With(height: 100f),
                        draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
            col.Row(ElementStyle.Default(), wrap: FlexWrap.Wrap, build: inner =>
            {
                for (var i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    inner.Item(ElementStyle.FixedWidth(50f).With(height: 50f),
                        draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
        });
    }
}