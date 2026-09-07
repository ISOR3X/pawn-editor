using System;
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

    extension<T>(TaffyTree<T> tree) where T : struct
    {
        public TaffyNode NewNodeWithStyle(Action<TaffyStyleRef> configure, TaffyNode[]? children = null)
        {
            var node = tree.NewNode();
            configure(tree.GetStyle(node));
            if (children == null) return node;
            foreach (var child in children)
                tree.AppendChild(node, child);
            return node;
        }

        public ConditionalNode<T> NewConditionalChild(TaffyNode parent, Action<TaffyStyleRef> configure, bool startVisible = true)
        {
            var content = tree.NewNodeWithStyle(configure);
            if (startVisible)
                tree.AppendChild(parent, content);
            return new ConditionalNode<T>(tree, parent, content);
        }
    }
}

public class ConditionalNode<T> where T : struct
{
    private readonly TaffyTree<T> _tree;
    private readonly TaffyNode _parent;
    private readonly TaffyNode _content;

    internal ConditionalNode(TaffyTree<T> tree, TaffyNode parent, TaffyNode content)
    {
        _tree = tree;
        _parent = parent;
        _content = content;
    }

    public bool IsVisible => _tree.GetParent(_content).HasValue;
    public TaffyLayout GetParentLayout() => _tree.GetLayout(_parent);
    public TaffyLayout GetContentLayout() => _tree.GetLayout(_content);

    public void SetVisible(bool visible)
    {
        if (visible == IsVisible) return;
        if (visible) _tree.AppendChild(_parent, _content);
        else _tree.RemoveChild(_parent, _content);
    }

    public void Toggle() => SetVisible(!IsVisible);
}

public class Window_Benchmark : Window
{
    private readonly TaffyTree<TaffyHelper.TaffyContext> _tree;
    private readonly TaffyNode _rootNode;
    private readonly TaffyNode _buttonNode;
    private readonly ConditionalNode<TaffyHelper.TaffyContext> _conditional;

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

        _rootNode = _tree.NewNodeWithStyle(s =>
        {
            s.Display = TaffyDisplay.Flex;
            s.FlexDirection = TaffyFlexDirection.Column;
            s.Width = Dimension.Percent(1f);
            s.Height = Dimension.Auto();
        }, children: [textNode]);

        var dynNode = _tree.NewNodeWithStyle(s =>
        {
            s.Width = Dimension.Px(200f);
            s.Height = Dimension.Auto();
            s.FlexDirection = TaffyFlexDirection.Column;
        });
        _tree.AppendChild(_rootNode, dynNode);

        _buttonNode = _tree.NewNodeWithStyle(s =>
        {
            s.Width = Dimension.Px(200f);
            s.Height = Dimension.Px(30f);
        });
        _tree.AppendChild(dynNode, _buttonNode);

        _conditional = _tree.NewConditionalChild(dynNode, s =>
        {
            s.MinWidth = Dimension.Px(400f);
            s.Width = Dimension.Percent(1f);
            s.Height = Dimension.Px(200f);
        });

        var textNode2 = _tree.NewLeafWithContext(new TaffyHelper.TaffyContext
        { font = GameFont.Small, text = LoremIpsum });

        _tree.AppendChild(_rootNode, textNode2);
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.ComputeLayoutWithMeasure(_rootNode, inRect.width, inRect.height, TaffyHelper.MeasureText);

        var textNode = _tree.ChildAt(_rootNode, 0);
        var textNode2 = _tree.ChildAt(_rootNode, 2);
        var layout = _tree.GetLayout(textNode);
        var layout2 = _tree.GetLayout(textNode2);
        var ctx = _tree.GetNodeContext(textNode);
        var ctx2 = _tree.GetNodeContext(textNode2);
        if (ctx == null) return;
        if (ctx2 == null) return;

        var dynLayout = _conditional.GetParentLayout();
        var dynOffset = inRect.position + new Vector2(dynLayout.x, dynLayout.y);

        var r = layout.ToRect(inRect.position);
        var r2 = _tree.GetLayout(_buttonNode).ToRect(dynOffset);
        var r3 = layout2.ToRect(inRect.position);

        using (new TextBlock(ctx.Value.font, TextAnchor.UpperLeft))
        {
            Text.WordWrap = true;
            Verse.Widgets.Label(r, ctx.Value.text);
        }

        using (new TextBlock(ctx2.Value.font, TextAnchor.UpperLeft))
        {
            Text.WordWrap = true;
            Verse.Widgets.Label(r3, ctx2.Value.text);
        }

        if (Verse.Widgets.ButtonText(r2, _conditional.IsVisible ? "Hide" : "Show"))
            _conditional.Toggle();

        if (_conditional.IsVisible)
            Verse.Widgets.DrawRectFast(_conditional.GetContentLayout().ToRect(dynOffset), Color.blue);
    }
}
