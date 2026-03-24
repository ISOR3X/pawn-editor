// Flexbox layout tests ported from taffy/tests/xml/flex/*.xml
// Each test matches a reference XML file in the Taffy test suite.

using PawnEditor.TaffySharp;
using Xunit;

namespace TaffySharpTests;

// ─────────────────────────────────────────────────────────────────────────────
// flex-grow / flex-basis
// ─────────────────────────────────────────────────────────────────────────────

public class FlexGrowTests
{
    // Reference: flex_basis_flex_grow_row__border_box_ltr.xml
    // Root 100×100; child0 flex-grow=1 flex-basis=50; child1 flex-grow=1
    // Free space = 100 - 50 = 50; split evenly → child0 gets +25, child1 gets +25
    // child0: w=75, child1: w=25
    [Fact]
    public void FlexBasisFlexGrowRow()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style
        {
            flexGrow  = 1,
            flexBasis = T.Px(50),
        });
        var child1 = tree.NewLeaf(new Style
        {
            flexGrow = 1,
        });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child0, child1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),   x:  0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child0), x:  0, y:  0, w:  75, h: 100, "child0");
        T.AssertLayout(tree.Layout(child1), x: 75, y:  0, w:  25, h: 100, "child1");
    }

    // Reference: flex_basis_flex_grow_column__border_box_ltr.xml
    [Fact]
    public void FlexBasisFlexGrowColumn()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = T.Px(50) });
        var child1 = tree.NewLeaf(new Style { flexGrow = 1 });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size          = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child0, child1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),   x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child0), x: 0, y:  0, w: 100, h:  75, "child0");
        T.AssertLayout(tree.Layout(child1), x: 0, y: 75, w: 100, h:  25, "child1");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// flex-shrink
// ─────────────────────────────────────────────────────────────────────────────

public class FlexShrinkTests
{
    // Reference: flex_basis_flex_shrink_row__border_box_ltr.xml
    // Root 100×100; child0 flex-basis=100; child1 flex-basis=50
    // Overflow=50; default shrink=1 each; child0 shrinks 100/150*50≈33, child1 50/150*50≈17
    // child0: w≈67, child1: w≈33
    [Fact]
    public void FlexBasisFlexShrinkRow()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style { flexBasis = T.Px(100) });
        var child1 = tree.NewLeaf(new Style { flexBasis = T.Px(50) });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child0, child1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),   x:  0, y: 0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child0), x:  0, y: 0, w:  67, h: 100, "child0");
        T.AssertLayout(tree.Layout(child1), x: 67, y: 0, w:  33, h: 100, "child1");
    }

    // Reference: flex_basis_flex_shrink_column__border_box_ltr.xml
    [Fact]
    public void FlexBasisFlexShrinkColumn()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style { flexBasis = T.Px(100) });
        var child1 = tree.NewLeaf(new Style { flexBasis = T.Px(50) });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size          = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child0, child1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),   x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child0), x: 0, y:  0, w: 100, h:  67, "child0");
        T.AssertLayout(tree.Layout(child1), x: 0, y: 67, w: 100, h:  33, "child1");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// justify-content
// ─────────────────────────────────────────────────────────────────────────────

public class JustifyContentTests
{
    // Reference: justify_content_row_center__border_box_ltr.xml
    // Root 100×100; three children each 10px wide; free space=70 → 35 leading
    [Fact]
    public void JustifyContentCenter()
    {
        var tree   = new TaffyTree();
        Style child = new() { size = new Size<Dimension>(T.Px(10), Dimension.Auto()) };

        var c0   = tree.NewLeaf(child);
        var c1   = tree.NewLeaf(child);
        var c2   = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            justifyContent = AlignContent.Center,
            size           = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y: 0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x: 35, y: 0, w:  10, h: 100, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 45, y: 0, w:  10, h: 100, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 55, y: 0, w:  10, h: 100, "c2");
    }

    // Reference: justify_content_row_space_between__border_box_ltr.xml
    // Root 100×100; three children each 10px; free space=70 → gap=35 between items
    [Fact]
    public void JustifyContentSpaceBetween()
    {
        var tree  = new TaffyTree();
        Style child = new() { size = new Size<Dimension>(T.Px(10), Dimension.Auto()) };

        var c0   = tree.NewLeaf(child);
        var c1   = tree.NewLeaf(child);
        var c2   = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            justifyContent = AlignContent.SpaceBetween,
            size           = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y: 0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y: 0, w:  10, h: 100, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 45, y: 0, w:  10, h: 100, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 90, y: 0, w:  10, h: 100, "c2");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Empty container / single child
// ─────────────────────────────────────────────────────────────────────────────

public class BasicFlexTests
{
    [Fact]
    public void EmptyFlexContainer()
    {
        var tree = new TaffyTree();
        var root = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(200), T.Px(100)),
        });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 200, h: 100, "root");
    }

    [Fact]
    public void SingleChildFillsContainer()
    {
        var tree  = new TaffyTree();
        var child = tree.NewLeaf(new Style { flexGrow = 1 });
        var root  = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(50)),
        }, new[] { child });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),  x: 0, y: 0, w: 100, h: 50, "root");
        T.AssertLayout(tree.Layout(child), x: 0, y: 0, w: 100, h: 50, "child");
    }

    [Fact]
    public void ThreeEqualChildrenRow()
    {
        var tree = new TaffyTree();
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(300), T.Px(100)),
        }, new[]
        {
            tree.NewLeaf(new Style { flexGrow = 1 }),
            tree.NewLeaf(new Style { flexGrow = 1 }),
            tree.NewLeaf(new Style { flexGrow = 1 }),
        });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),                    x:   0, y: 0, w: 300, h: 100, "root");
        T.AssertLayout(tree.Layout(tree.ChildAt(root, 0)),   x:   0, y: 0, w: 100, h: 100, "c0");
        T.AssertLayout(tree.Layout(tree.ChildAt(root, 1)),   x: 100, y: 0, w: 100, h: 100, "c1");
        T.AssertLayout(tree.Layout(tree.ChildAt(root, 2)),   x: 200, y: 0, w: 100, h: 100, "c2");
    }
}
