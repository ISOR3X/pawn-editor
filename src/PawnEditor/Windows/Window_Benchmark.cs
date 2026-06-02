using System.Linq;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static class TaffyHelper
{
    extension(TaffyTree<string> tree)
    {
        public TaffyNode NewTextNode(string text) => tree.NewLeafWithContext(text);
    }

    public static TaffySize MeasureText(
        TaffyMeasureMode widthMode, float width,
        TaffyMeasureMode heightMode, float height,
        string? text)
    {
        if (text is null) return new TaffySize();
        using (new TextBlock(GameFont.Small))
        {
            switch (widthMode)
            {
                case TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent:
                    return new TaffySize { width = width, height = Text.CalcHeight(text, width) };
                case TaffyMeasureMode.MinContent:
                {
                    var minW = text.Split(' ').Select(w => Text.CalcSize(w).x).Prepend(0f).Max();
                    return new TaffySize { width = minW, height = Text.CalcHeight(text, minW) };
                }
                default:
                {
                    var sz = Text.CalcSize(text);
                    return new TaffySize { width = sz.x, height = sz.y };
                }
            }
        }
    }
}

public class Window_Benchmark : Window
{
    private readonly TaffyTree<string> _tree;
    private readonly TaffyNode _rootNode;

    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

    public Window_Benchmark()
    {
        resizeable = true;

        _tree = new TaffyTree<string>();

        var textNode = _tree.NewTextNode(LoremIpsum);

        var tempRoot = _tree.NewNode();
        var rootStyle = _tree.GetStyle(tempRoot);
        rootStyle.Display       = TaffyDisplay.Flex;
        rootStyle.FlexDirection = TaffyFlexDirection.Column;
        rootStyle.Width         = Dimension.Percent(1f);
        rootStyle.Height        = Dimension.Auto();

        _rootNode = _tree.NewWithChildren(rootStyle, [textNode]);
        _tree.RemoveNode(tempRoot);
    }

    public override void Close(bool doCloseSound = true)
    {
        _tree.Dispose();
        base.Close(doCloseSound);
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.ComputeLayoutWithMeasure(_rootNode, inRect.width, inRect.height, TaffyHelper.MeasureText);

        for (var i = 0; i < _tree.ChildCount(_rootNode); i++)
        {
            var layout = _tree.GetLayout(_tree.ChildAt(_rootNode, i));
            var r = new Rect(inRect.x + layout.x, inRect.y + layout.y, layout.width, layout.height);
            Verse.Widgets.DrawRectFast(r, Color.red);
            Verse.Widgets.DrawBox(r, 1, SolidColorMaterials.NewSolidColorTexture(Color.white));
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
            {
                Text.WordWrap = true;
                Verse.Widgets.Label(r, LoremIpsum);
            }
        }
    }
}
