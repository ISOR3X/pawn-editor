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
            col.Row(ElementStyle.Default(), fitContent: true, gap: 8f, build: row =>
            {
                row.Item(style: ElementStyle.Default().With(width: 100f),
                    rect => { Verse.Widgets.DrawRectFast(rect, Color.red); });
                row.Button("+ Add",
                    onClick: () => _itemCount = Mathf.Min(_itemCount + 1, 50));
                row.Button("- Remove",
                    onClick: () => _itemCount = Mathf.Max(_itemCount - 1, 0));
                row.Label($"Items: {_itemCount}", ElementStyle.Fill);
                row.Item(style: ElementStyle.Default().With(width: 100f),
                    rect => { Verse.Widgets.DrawRectFast(rect, Color.red); });
            });
            col.Row(wrap: FlexWrap.Wrap, fitContent: true,
                build: inner =>
                {
                    for (var i = 0; i < _itemCount; i++)
                    {
                        var color = ItemColors[i % ItemColors.Length];
                        inner.Item(ElementStyle.FixedWidth(100f).With(height: 100f),
                            draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                    }
                });
            col.Row(wrap: FlexWrap.Wrap, fitContent: true,
                build: inner =>
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