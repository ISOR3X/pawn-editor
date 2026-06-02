using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class VoidTree<T> : TaffyTree<T> where T : class
{
    private Dictionary<string, TaffyNode> _nodesById = [];

    public void SetNodeId(TaffyNode node, string id)
    {
        _nodesById.Add(id, node);
    }

    public TaffyNode? GetNodeById(string id)
    {
        return _nodesById.TryGetValue(id, out var node) ? node : null;
    }
}

public class FakePawn(string name, string gender, int age, string backStory)
{
    private static int _nextId = 0;
    public readonly float uniqueId = _nextId++;
    public string name = name;
    public string gender = gender;
    public int age = age;
    public string backStory = backStory;

    private static readonly string[] Names = ["Alice", "Bob", "Charlie", "Diana", "Eve", "Frank"];
    private static readonly string[] Genders = ["Male", "Female"];
    private static readonly string[] BackStories =
    [
        "A former soldier who turned to farming.",
        "A wandering merchant with a troubled past.",
        "A scholar exiled from their homeland.",
        "A survivor of a colony ship crash."
    ];

    public static FakePawn GenerateRandomPawn()
    {
        var rng = new System.Random();
        return new FakePawn(
            Names[rng.Next(Names.Length)],
            Genders[rng.Next(Genders.Length)],
            rng.Next(18, 65),
            BackStories[rng.Next(BackStories.Length)]
        );
    }
}

public abstract record NodeContext;
public record TextContext(string Text, GameFont Font = GameFont.Small) : NodeContext;
public record FloatContext(float Id) : NodeContext;

public static class TaffyHelper
{
    public static Rect ToRect(this TaffyLayout layout, Vector2 positionOffset)
    {
        return new Rect(positionOffset.x + layout.x, positionOffset.y + layout.y, layout.width, layout.height);
    }

    public static TaffySize MeasureText(
        TaffyMeasureMode widthMode, float width,
        TaffyMeasureMode heightMode, float height,
        NodeContext? context)
    {
        if (context is not TextContext tCtx) return new TaffySize();
        using (new TextBlock(tCtx.Font))
        {
            switch (widthMode)
            {
                case TaffyMeasureMode.Exact or TaffyMeasureMode.FitContent:
                    return new TaffySize { width = width, height = Text.CalcHeight(tCtx.Text, width) };
                case TaffyMeasureMode.MinContent:
                    {
                        var minW = tCtx.Text.Split(' ').Select(w => Text.CalcSize(w).x).Prepend(0f).Max();
                        return new TaffySize { width = minW, height = Text.CalcHeight(tCtx.Text, minW) };
                    }
                default:
                    {
                        var sz = Text.CalcSize(tCtx.Text);
                        return new TaffySize { width = sz.x, height = sz.y };
                    }
            }
        }
    }

    extension<T>(TaffyTree<T> tree) where T : class
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

public class ConditionalNode<T> where T : class
{
    private readonly TaffyTree<T> _tree;
    public readonly TaffyNode _parent;
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
    private readonly VoidTree<NodeContext> _tree;

    private static FakePawn pawn1 = FakePawn.GenerateRandomPawn();
    private static FakePawn pawn2 = FakePawn.GenerateRandomPawn();
    private static FakePawn pawn3 = FakePawn.GenerateRandomPawn();
    private static FakePawn selectedPawn = pawn1; // TODO: weak reference

    private readonly TaffyNode _buttonNode;
    private readonly ConditionalNode<NodeContext> _conditional;


    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

    public Window_Benchmark()
    {
        resizeable = true;

        _tree = new VoidTree<NodeContext>();

        using var btnStyle = new TaffyStyleOwned(r =>
        {
            r.Width = Dimension.Px(200f);
            r.Height = Dimension.Px(30f);
        });

        var _rootNode = _tree.NewLeafWithContext(new FloatContext(selectedPawn.uniqueId));
        _tree.SetNodeId(_rootNode, "root");
        
        var s = _tree.GetStyle(_rootNode);
        s.Display = TaffyDisplay.Flex;
        s.FlexDirection = TaffyFlexDirection.Column;
        s.Width = Dimension.Percent(1f);
        s.Height = Dimension.Auto();

        var textNode = _tree.NewLeafWithContext(new TextContext(LoremIpsum));
        _tree.SetNodeId(textNode, "text");
        _tree.AppendChild(_rootNode, textNode);

        // button with dynamic text?
        var switchPawnBtnNode = _tree.NewLeafWithContext(new TextContext(selectedPawn.name));
        _tree.SetNodeId(switchPawnBtnNode, "btn2");
        _tree.SetStyle(switchPawnBtnNode, btnStyle);

        _tree.AppendChild(_rootNode, switchPawnBtnNode);

        // Conditional node
        var dynNode = _tree.NewNodeWithStyle(s =>
        {
            s.Width = Dimension.Px(200f);
            s.Height = Dimension.Auto();
            s.FlexDirection = TaffyFlexDirection.Column;
        });
        _tree.AppendChild(_rootNode, dynNode);

        _buttonNode = _tree.NewNode();
        _tree.SetStyle(_buttonNode, btnStyle);
        _tree.AppendChild(dynNode, _buttonNode);

        _conditional = _tree.NewConditionalChild(dynNode, s =>
        {
            s.MinWidth = Dimension.Px(400f);
            s.Width = Dimension.Percent(1f);
            s.Height = Dimension.Px(200f);
        });
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.ComputeLayoutWithMeasure(_tree.GetNodeById("root")!.Value, inRect.width, inRect.height, TaffyHelper.MeasureText);

        var textNode = _tree.GetNodeById("text")!;
        var layout = _tree.GetLayout(textNode.Value);
        var ctx = _tree.GetNodeContext(textNode.Value);
        if (ctx is TextContext tCtx)
        {
            var r = layout.ToRect(inRect.position);
            using (new TextBlock(tCtx.Font, TextAnchor.UpperLeft))
            {
                Text.WordWrap = true;
                Verse.Widgets.Label(r, tCtx.Text);
            }
        }

        var dynLayout = _conditional.GetParentLayout();
        var dynOffset = inRect.position + new Vector2(dynLayout.x, dynLayout.y);

        var r2 = _tree.GetLayout(_buttonNode).ToRect(dynOffset);
        var r3 = _tree.GetLayout(_tree.GetNodeById("btn2")!.Value).ToRect(inRect.position);

        Log.Message(r2.position + "-" + r2.size);

        if (Verse.Widgets.ButtonText(r2, _conditional.IsVisible ? "Hide" : "Show"))
            _conditional.Toggle();

        if (Verse.Widgets.ButtonText(r3, selectedPawn?.name))
        {

        }

        if (_conditional.IsVisible)
            Verse.Widgets.DrawRectFast(_conditional.GetContentLayout().ToRect(dynOffset), Color.blue with { a = 0.5f });
    }
}
