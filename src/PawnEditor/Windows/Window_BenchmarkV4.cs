using Taffy;
using UnityEngine;
using Verse;
using Void.v4;

namespace PawnEditor;

/// <summary>
///     Same scenario as <see cref="Window_Benchmark" /> (two text blocks, a fixed-size sidebar
///     with a toggle button, and a box that appears/disappears) but built on the new
///     <see cref="VoidTree" /> reconciled-tree core, through its scoped <see cref="ChildBuilder" />
///     API. The toggle button exercises keyed child add/remove through the automatic
///     commit-on-scope-exit in <see cref="ChildBuilder.Div" /> instead of the bespoke
///     <c>ConditionalNode</c> helper the v1 benchmark uses — this is deliberately the same content
///     so the two windows are comparable.
/// </summary>
public class Window_BenchmarkV4 : Window
{
    private readonly RenderTree _tree = new();
    private bool _showBox;

    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

    public Window_BenchmarkV4()
    {
        resizeable = true;
    }

    public override void PostClose()
    {
        base.PostClose();
        // _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.Build(
            root =>
            {
                root.Text("text1", LoremIpsum);

                root.Div("dyn-col",
                    dyn =>
                    {
                        dyn.Div("toggle-btn",
                            draw: r =>
                            {
                                if (Verse.Widgets.ButtonText(r, _showBox ? "Hide" : "Show"))
                                    _showBox = !_showBox;
                            },
                            style: new StyleOverride { width = Dimension.Px(200f), height = Dimension.Px(30f) });

                        if (_showBox)
                            dyn.Div("box", draw: r => Verse.Widgets.DrawRectFast(r, Color.blue), style: new StyleOverride
                            {
                                minWidth = Dimension.Px(400f),
                                width = Dimension.Percent(1f),
                                height = Dimension.Px(200f)
                            });
                    }, style: new StyleOverride
                    {
                        width = Dimension.Px(200f),
                        height = Dimension.Auto(),
                        flexDirection = TaffyFlexDirection.Column
                    });

                root.Text("text2", LoremIpsum);
            }, new StyleOverride
            {
                display = TaffyDisplay.Flex,
                flexDirection = TaffyFlexDirection.Column,
                width = Dimension.Percent(1f),
                height = Dimension.Auto()
            });

        _tree.Draw(inRect);
    }
}
