using Taffy;
using UnityEngine;
using Verse;
using Void.v2;

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
public class Window_BenchmarkV2 : Window
{
    private readonly VoidTree _tree = new();
    private bool _showBox;

    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

    public Window_BenchmarkV2()
    {
        resizeable = true;
    }

    public override void PostClose()
    {
        base.PostClose();
        _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.Build(
            new VStyle(Display: TaffyDisplay.Flex, FlexDirection: TaffyFlexDirection.Column,
                Width: Dimension.Percent(1f), Height: Dimension.Auto()),
            root =>
            {
                root.Text("text1", LoremIpsum);

                root.Div("dyn-col",
                    new VStyle(Width: Dimension.Px(200f), Height: Dimension.Auto(),
                        FlexDirection: TaffyFlexDirection.Column),
                    dyn =>
                    {
                        dyn.Item("toggle-btn",
                            new VStyle(Width: Dimension.Px(200f), Height: Dimension.Px(30f)),
                            draw: r =>
                            {
                                if (Verse.Widgets.ButtonText(r, _showBox ? "Hide" : "Show"))
                                    _showBox = !_showBox;
                            });

                        if (_showBox)
                            dyn.Item("box",
                                new VStyle(MinWidth: Dimension.Px(400f), Width: Dimension.Percent(1f),
                                    Height: Dimension.Px(200f)),
                                draw: r => Verse.Widgets.DrawRectFast(r, Color.blue));
                    });

                root.Text("text2", LoremIpsum);
            });

        _tree.ComputeLayout(inRect.width, inRect.height, VoidTree.DefaultMeasure);
        _tree.Draw(inRect);
    }
}
