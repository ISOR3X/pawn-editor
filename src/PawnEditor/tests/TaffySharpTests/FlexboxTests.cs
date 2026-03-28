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

// ─────────────────────────────────────────────────────────────────────────────
// flex-direction: column
// ─────────────────────────────────────────────────────────────────────────────

public class FlexDirectionColumnTests
{
    // Reference: flex_direction_column__border_box_ltr.xml
    // Column 100×100; three 10px-high children stack vertically and stretch to full width.
    [Fact]
    public void FlexDirectionColumn()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) });
        var c1   = tree.NewLeaf(new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) });
        var c2   = tree.NewLeaf(new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size          = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x: 0, y:  0, w: 100, h:  10, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 0, y: 10, w: 100, h:  10, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 0, y: 20, w: 100, h:  10, "c2");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// flex-wrap
// ─────────────────────────────────────────────────────────────────────────────

public class FlexWrapTests
{
    // Reference: wrap_row__border_box_ltr.xml
    // 4 children (w=31,32,33,34) in 100px flex-wrap:wrap row.
    // Row 1: 31+32+33=96 fits, 96+34>100 → child3 wraps to row 2.
    [Fact]
    public void WrapRowBasic()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(31), T.Px(30)) });
        var c1   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(32), T.Px(30)) });
        var c2   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(33), T.Px(30)) });
        var c3   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(34), T.Px(30)) });
        var root = tree.NewWithChildren(new Style
        {
            flexWrap = FlexWrap.Wrap,
            size     = new Size<Dimension>(T.Px(100), Dimension.Auto()),
        }, new[] { c0, c1, c2, c3 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y:  0, w: 100, h: 60, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y:  0, w:  31, h: 30, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 31, y:  0, w:  32, h: 30, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 63, y:  0, w:  33, h: 30, "c2");
        T.AssertLayout(tree.Layout(c3),   x:  0, y: 30, w:  34, h: 30, "c3");
    }

    // Reference: wrap_row_align_items_center__border_box_ltr.xml
    // 4×30px-wide children (h=10,20,30,30) in flex-wrap:wrap align-items:center 100px row.
    // Row 0 tallest=30; children vertically centered within that row height.
    [Fact]
    public void WrapRowAlignItemsCenter()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(10)) });
        var c1   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(20)) });
        var c2   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(30)) });
        var c3   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(30)) });
        var root = tree.NewWithChildren(new Style
        {
            flexWrap   = FlexWrap.Wrap,
            alignItems = AlignItems.Center,
            size       = new Size<Dimension>(T.Px(100), Dimension.Auto()),
        }, new[] { c0, c1, c2, c3 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y:  0, w: 100, h: 60, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y: 10, w:  30, h: 10, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 30, y:  5, w:  30, h: 20, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 60, y:  0, w:  30, h: 30, "c2");
        T.AssertLayout(tree.Layout(c3),   x:  0, y: 30, w:  30, h: 30, "c3");
    }

    // Reference: wrap_reverse_column__border_box_ltr.xml
    // 4 children (w=30, h=31/32/33/34) in flex-direction:column flex-wrap:wrap-reverse 100×100.
    // First 3 fit (31+32+33=96 ≤ 100) → placed in rightmost column (x=70).
    // 4th wraps to the next column leftward (x=20).
    [Fact]
    public void WrapReverseColumn()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(31)) });
        var c1   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(32)) });
        var c2   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(33)) });
        var c3   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(34)) });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            flexWrap      = FlexWrap.WrapReverse,
            size          = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2, c3 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x: 70, y:  0, w:  30, h:  31, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 70, y: 31, w:  30, h:  32, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 70, y: 63, w:  30, h:  33, "c2");
        T.AssertLayout(tree.Layout(c3),   x: 20, y:  0, w:  30, h:  34, "c3");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// align-items / align-self / align-content
// ─────────────────────────────────────────────────────────────────────────────

public class AlignItemsTests
{
    // Reference: align_items_stretch__border_box_ltr.xml
    // Default align-items:stretch — single 10px-wide child fills cross axis (h=100).
    [Fact]
    public void AlignItemsStretch()
    {
        var tree  = new TaffyTree();
        var child = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(10), Dimension.Auto()) });
        var root  = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),  x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child), x: 0, y:  0, w:  10, h: 100, "child");
    }

    // Reference: align_self_center__border_box_ltr.xml
    // align-self:center on a 10×10 child inside a 100×100 row container → y=45.
    [Fact]
    public void AlignSelfCenter()
    {
        var tree  = new TaffyTree();
        var child = tree.NewLeaf(new Style
        {
            alignSelf = AlignItems.Center,
            size      = new Size<Dimension>(T.Px(10), T.Px(10)),
        });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),  x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child), x: 0, y: 45, w:  10, h:  10, "child");
    }

    // Reference: align_baseline__border_box_ltr.xml
    // align-items:baseline; child0=50×50, child1=50×20.
    // Leaf nodes: baseline = bottom of content box.
    // child0 baseline at y+h=50; child1 placed so its baseline also = 50 → y=30.
    [Fact]
    public void AlignBaseline()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(50), T.Px(50)) });
        var c1   = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(50), T.Px(20)) });
        var root = tree.NewWithChildren(new Style
        {
            alignItems = AlignItems.Baseline,
            size       = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y:  0, w:  50, h:  50, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 50, y: 30, w:  50, h:  20, "c1");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// justify-content (column)
// ─────────────────────────────────────────────────────────────────────────────

public class JustifyContentColumnTests
{
    // Reference: justify_content_column_center__border_box_ltr.xml
    // Column 100×100; three 10px-high children; free space=70 → 35px leading/trailing.
    [Fact]
    public void JustifyContentColumnCenter()
    {
        var tree  = new TaffyTree();
        Style child = new() { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) };
        var c0   = tree.NewLeaf(child);
        var c1   = tree.NewLeaf(child);
        var c2   = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            flexDirection  = FlexDirection.Column,
            justifyContent = AlignContent.Center,
            size           = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x: 0, y: 35, w: 100, h:  10, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 0, y: 45, w: 100, h:  10, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 0, y: 55, w: 100, h:  10, "c2");
    }

    // Reference: justify_content_column_space_between__border_box_ltr.xml
    // Column 100×100; three 10px-high children; space-between puts gaps between items.
    [Fact]
    public void JustifyContentColumnSpaceBetween()
    {
        var tree  = new TaffyTree();
        Style child = new() { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) };
        var c0   = tree.NewLeaf(child);
        var c1   = tree.NewLeaf(child);
        var c2   = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            flexDirection  = FlexDirection.Column,
            justifyContent = AlignContent.SpaceBetween,
            size           = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x: 0, y:  0, w: 100, h:  10, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 0, y: 45, w: 100, h:  10, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 0, y: 90, w: 100, h:  10, "c2");
    }

    // Reference: justify_content_column_space_evenly__border_box_ltr.xml
    // Column 100×100; three 10px-high children; space-evenly: 70px / 4 gaps ≈ 17.5.
    [Fact]
    public void JustifyContentColumnSpaceEvenly()
    {
        var tree  = new TaffyTree();
        Style child = new() { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) };
        var c0   = tree.NewLeaf(child);
        var c1   = tree.NewLeaf(child);
        var c2   = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            flexDirection  = FlexDirection.Column,
            justifyContent = AlignContent.SpaceEvenly,
            size           = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x: 0, y: 18, w: 100, h:  10, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 0, y: 45, w: 100, h:  10, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 0, y: 73, w: 100, h:  10, "c2");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// padding / gap
// ─────────────────────────────────────────────────────────────────────────────

public class PaddingAndGapTests
{
    // Reference: padding_flex_child__border_box_ltr.xml
    // 100×100 container with padding=10 on all sides; single flex-grow=1 child.
    // Child occupies inner box: x=10, y=10, w=80, h=80.
    [Fact]
    public void PaddingFlexChild()
    {
        var tree  = new TaffyTree();
        var child = tree.NewLeaf(new Style { flexGrow = 1, size = new Size<Dimension>(T.Px(10), Dimension.Auto()) });
        var root  = tree.NewWithChildren(new Style
        {
            padding = T.Padding(10),
            size    = new Size<Dimension>(T.Px(100), T.Px(100)),
        }, new[] { child });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root),  x:  0, y:  0, w: 100, h: 100, "root");
        T.AssertLayout(tree.Layout(child), x: 10, y: 10, w:  80, h:  80, "child");
    }

    // Reference: gap_column_gap_flexible__border_box_ltr.xml
    // 80×100 row; column-gap=10; 3 children flex-grow=1 flex-basis=0%.
    // Available = 80 - 2×10 = 60px for 3 items → each w=20; placed at x=0, 30, 60.
    [Fact]
    public void GapColumnGap()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = Dimension.Percent(0) });
        var c1   = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = Dimension.Percent(0) });
        var c2   = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = Dimension.Percent(0) });
        var root = tree.NewWithChildren(new Style
        {
            gap  = new Size<LengthPercentage>(LengthPercentage.Length(10), LengthPercentage.Length(20)),
            size = new Size<Dimension>(T.Px(80), T.Px(100)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y: 0, w: 80, h: 100, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y: 0, w: 20, h: 100, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 30, y: 0, w: 20, h: 100, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 60, y: 0, w: 20, h: 100, "c2");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// flex-grow edge cases
// ─────────────────────────────────────────────────────────────────────────────

public class FlexGrowEdgeCaseTests
{
    // Reference: flex_grow_less_than_factor_one__border_box_ltr.xml
    // When sum of flex-grow < 1.0, only (sum * free_space) is distributed.
    // Container 500×200; items: grow=0.2/0.2/0.4, shrink=0, basis=40/0/0.
    // Free=460, distributed=0.8×460=368; each gets basis + grow/0.8×368.
    [Fact]
    public void FlexGrowLessThanFactorOne()
    {
        var tree = new TaffyTree();
        var c0   = tree.NewLeaf(new Style { flexGrow = 0.2f, flexShrink = 0, flexBasis = T.Px(40) });
        var c1   = tree.NewLeaf(new Style { flexGrow = 0.2f, flexShrink = 0 });
        var c2   = tree.NewLeaf(new Style { flexGrow = 0.4f, flexShrink = 0 });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(500), T.Px(200)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:   0, y: 0, w: 500, h: 200, "root");
        T.AssertLayout(tree.Layout(c0),   x:   0, y: 0, w: 132, h: 200, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 132, y: 0, w:  92, h: 200, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 224, y: 0, w: 184, h: 200, "c2");
    }
}
