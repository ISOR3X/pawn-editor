using Taffy;
using UnityEngine;
using Verse;
using Void;

namespace PawnEditor;

/// <summary>
///     Window that renders 100 flex-wrapped nodes to benchmark the ctaffy layout engine.
/// </summary>
public class Window_Benchmark : Window
{
    private readonly TaffyTree _tree;
    private readonly TaffyNode _root;
    private readonly TaffyNode[] _children = new TaffyNode[100];

    public Window_Benchmark()
    {
        resizeable = true;

        _tree = new TaffyTree();

        for (var i = 0; i < _children.Length; i++)
        {
            var node = _tree.NewNode();
            var s = _tree.GetStyle(node);
            s.Width = Dimension.Px(100f);
            s.Height = Dimension.Px(100f);
            _children[i] = node;
        }

        _root = _tree.NewNode();
        {
            var s = _tree.GetStyle(_root);
            s.Display = TaffyDisplay.Flex;
            s.FlexWrap = TaffyFlexWrap.Wrap;
            s.ColumnGap = Dimension.Px(10f);
            s.RowGap = Dimension.Px(10f);
        }

        foreach (var child in _children)
            _tree.AppendChild(_root, child);
    }

    public override void Close(bool doCloseSound = true)
    {
        _tree.Dispose();
        base.Close(doCloseSound);
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.ComputeLayout(_root, inRect.width, inRect.height);

        for (var i = 0; i < _children.Length; i++)
        {
            var layout = _tree.GetLayout(_children[i]);
            Verse.Widgets.DrawRectFast(
                new Rect(inRect.x + layout.x, inRect.y + layout.y, layout.width, layout.height),
                Color.red);
        }
    }
}
