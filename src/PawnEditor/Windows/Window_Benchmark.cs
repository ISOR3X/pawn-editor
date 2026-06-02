using System.Linq;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static class TaffyHelper
{
    public static Rect ToRect(this TaffyLayout layout, Vector2 positionOffset)
    {
        return new Rect(positionOffset.x + layout.x, positionOffset.y + layout.y, layout.width, layout.height);
    }

    public struct TaffyContext(string text, GameFont font = GameFont.Small)
    {
        public GameFont font = font;
        public string text = text;
    }

    public static TaffySize MeasureText(
        TaffyMeasureMode widthMode, float width,
        TaffyMeasureMode heightMode, float height,
        TaffyContext? context)
    {
        Log.Message("MeasureText");
        if (!context.HasValue) return new TaffySize();
        using (new TextBlock(context.Value.font))
        {
            switch (widthMode)
            {
                case TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent:
                    return new TaffySize { width = width, height = Text.CalcHeight(context.Value.text, width) };
                case TaffyMeasureMode.MinContent:
                {
                    var minW = context.Value.text.Split(' ').Select(w => Text.CalcSize(w).x).Prepend(0f).Max();
                    return new TaffySize { width = minW, height = Text.CalcHeight(context.Value.text, minW) };
                }
                default:
                {
                    var sz = Text.CalcSize(context.Value.text);
                    return new TaffySize { width = sz.x, height = sz.y };
                }
            }
        }
    }
}

public class Window_Benchmark : Window
{
    private readonly TaffyTree<TaffyHelper.TaffyContext> _tree;
    private readonly TaffyNode _rootNode;
    private TaffyNode _dynNode;
    private TaffyNode _contentNode;

    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

    public Window_Benchmark()
    {
        resizeable = true;

        _tree = new TaffyTree<TaffyHelper.TaffyContext>();

        var textNode = _tree.NewLeafWithContext(new TaffyHelper.TaffyContext
            { font = GameFont.Small, text = LoremIpsum });

        var tempRoot = _tree.NewNode();
        var rootStyle = _tree.GetStyle(tempRoot);
        rootStyle.Display = TaffyDisplay.Flex;
        rootStyle.FlexDirection = TaffyFlexDirection.Column;
        rootStyle.Width = Dimension.Percent(1f);
        rootStyle.Height = Dimension.Auto();

        _rootNode = _tree.NewWithChildren(rootStyle, [textNode]);
        _tree.RemoveNode(tempRoot);

        _dynNode = _tree.NewNode();
        // GetStyle returns a reference, so no need to use SetStyle unless we want to also mark dirty.
        var ds = _tree.GetStyle(_dynNode);
        ds.Width = Dimension.Px(200f);
        ds.Height = Dimension.Auto();
        ds.FlexDirection = TaffyFlexDirection.Column;

        _tree.AppendChild(_rootNode, _dynNode);

        var buttonNode = _tree.NewNode();
        _tree.AppendChild(_dynNode, buttonNode);
        var bs = _tree.GetStyle(buttonNode);
        bs.Width = Dimension.Px(200f);
        bs.Height = Dimension.Px(30f);
        _tree.SetStyle(buttonNode, bs);

        _contentNode = _tree.NewNode();
        _tree.AppendChild(_dynNode, _contentNode);
        var cs = _tree.GetStyle(_contentNode);
        cs.MinWidth = Dimension.Px(400f);
        cs.Width = Dimension.Percent(1f);
        cs.Height = Dimension.Px(200f);
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.ComputeLayoutWithMeasure(_rootNode, inRect.width, inRect.height, TaffyHelper.MeasureText);

        // Word wrapping
        var textNode = _tree.ChildAt(_rootNode, 0);
        var layout = _tree.GetLayout(textNode);
        var ctx = _tree.GetNodeContext(textNode);
        if (ctx == null) return;

        // Conditional rendering
        var dynLayout = _tree.GetLayout(_dynNode);
        var dynOffset = inRect.position + new Vector2(dynLayout.x, dynLayout.y);

        var btnNode = _tree.ChildAt(_dynNode, 0);
        var btnLayout = _tree.GetLayout(btnNode);

        var r = layout.ToRect(inRect.position);
        var r2 = btnLayout.ToRect(dynOffset);

        using (new TextBlock(ctx.Value.font, TextAnchor.UpperLeft))
        {
            Text.WordWrap = true;
            Verse.Widgets.Label(r, ctx.Value.text);
        }

        var contentAttached = _tree.GetParent(_contentNode).HasValue;
        if (Verse.Widgets.ButtonText(r2, contentAttached ? "Hide" : "Show"))
        {
            if (contentAttached)
                _tree.RemoveChild(_dynNode, _contentNode);
            else
                _tree.AppendChild(_dynNode, _contentNode);
        }

        if (contentAttached)
        {
            var r3 = _tree.GetLayout(_contentNode).ToRect(dynOffset);
            Verse.Widgets.DrawRectFast(r3, Color.blue);
        }
        
    }
}