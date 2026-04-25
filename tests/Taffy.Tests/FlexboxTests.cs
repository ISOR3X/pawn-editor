using Taffy;
using Xunit;

namespace TaffyTests;

public class FlexGrowTests
{
    [Fact]
    public void FlexBasisFlexGrowRow()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style
        {
            flexGrow = 1,
            flexBasis = T.Px(50)
        });
        var child1 = tree.NewLeaf(new Style
        {
            flexGrow = 1
        });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child0, child1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child0), 0, 0, 75, 100, "child0");
        T.AssertLayout(tree.Layout(child1), 75, 0, 25, 100, "child1");
    }

    [Fact]
    public void FlexBasisFlexGrowColumn()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = T.Px(50) });
        var child1 = tree.NewLeaf(new Style { flexGrow = 1 });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child0, child1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child0), 0, 0, 100, 75, "child0");
        T.AssertLayout(tree.Layout(child1), 0, 75, 100, 25, "child1");
    }
}

public class FlexShrinkTests
{
    [Fact]
    public void FlexBasisFlexShrinkRow()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style { flexBasis = T.Px(100) });
        var child1 = tree.NewLeaf(new Style { flexBasis = T.Px(50) });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child0, child1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child0), 0, 0, 67, 100, "child0");
        T.AssertLayout(tree.Layout(child1), 67, 0, 33, 100, "child1");
    }

    [Fact]
    public void FlexBasisFlexShrinkColumn()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style { flexBasis = T.Px(100) });
        var child1 = tree.NewLeaf(new Style { flexBasis = T.Px(50) });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child0, child1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child0), 0, 0, 100, 67, "child0");
        T.AssertLayout(tree.Layout(child1), 0, 67, 100, 33, "child1");
    }
}

public class JustifyContentTests
{
    [Fact]
    public void JustifyContentCenter()
    {
        var tree = new TaffyTree();
        var child = new Style { size = new Size<Dimension>(T.Px(10), Dimension.Auto()) };

        var c0 = tree.NewLeaf(child);
        var c1 = tree.NewLeaf(child);
        var c2 = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            justifyContent = AlignContent.Center,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 35, 0, 10, 100, "c0");
        T.AssertLayout(tree.Layout(c1), 45, 0, 10, 100, "c1");
        T.AssertLayout(tree.Layout(c2), 55, 0, 10, 100, "c2");
    }

    [Fact]
    public void JustifyContentSpaceBetween()
    {
        var tree = new TaffyTree();
        var child = new Style { size = new Size<Dimension>(T.Px(10), Dimension.Auto()) };

        var c0 = tree.NewLeaf(child);
        var c1 = tree.NewLeaf(child);
        var c2 = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            justifyContent = AlignContent.SpaceBetween,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 10, 100, "c0");
        T.AssertLayout(tree.Layout(c1), 45, 0, 10, 100, "c1");
        T.AssertLayout(tree.Layout(c2), 90, 0, 10, 100, "c2");
    }
}

public class BasicFlexTests
{
    [Fact]
    public void EmptyFlexContainer()
    {
        var tree = new TaffyTree();
        var root = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(200), T.Px(100))
        });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 200, 100, "root");
    }

    [Fact]
    public void SingleChildFillsContainer()
    {
        var tree = new TaffyTree();
        var child = tree.NewLeaf(new Style { flexGrow = 1 });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(50))
        }, [child]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 50, "root");
        T.AssertLayout(tree.Layout(child), 0, 0, 100, 50, "child");
    }

    [Fact]
    public void ThreeEqualChildrenRow()
    {
        var tree = new TaffyTree();
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(300), T.Px(100))
        },
        [
            tree.NewLeaf(new Style { flexGrow = 1 }),
            tree.NewLeaf(new Style { flexGrow = 1 }),
            tree.NewLeaf(new Style { flexGrow = 1 })
        ]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 300, 100, "root");
        T.AssertLayout(tree.Layout(tree.ChildAt(root, 0)), 0, 0, 100, 100, "c0");
        T.AssertLayout(tree.Layout(tree.ChildAt(root, 1)), 100, 0, 100, 100, "c1");
        T.AssertLayout(tree.Layout(tree.ChildAt(root, 2)), 200, 0, 100, 100, "c2");
    }
}

public class FlexDirectionColumnTests
{
    [Fact]
    public void FlexDirectionColumn()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) });
        var c1 = tree.NewLeaf(new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) });
        var c2 = tree.NewLeaf(new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 100, 10, "c0");
        T.AssertLayout(tree.Layout(c1), 0, 10, 100, 10, "c1");
        T.AssertLayout(tree.Layout(c2), 0, 20, 100, 10, "c2");
    }
}

public class FlexWrapTests
{
    [Fact]
    public void WrapRowBasic()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(31), T.Px(30)) });
        var c1 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(32), T.Px(30)) });
        var c2 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(33), T.Px(30)) });
        var c3 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(34), T.Px(30)) });
        var root = tree.NewWithChildren(new Style
        {
            flexWrap = FlexWrap.Wrap,
            size = new Size<Dimension>(T.Px(100), Dimension.Auto())
        }, [c0, c1, c2, c3]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 60, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 31, 30, "c0");
        T.AssertLayout(tree.Layout(c1), 31, 0, 32, 30, "c1");
        T.AssertLayout(tree.Layout(c2), 63, 0, 33, 30, "c2");
        T.AssertLayout(tree.Layout(c3), 0, 30, 34, 30, "c3");
    }

    [Fact]
    public void WrapRowAlignItemsCenter()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(10)) });
        var c1 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(20)) });
        var c2 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(30)) });
        var c3 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(30)) });
        var root = tree.NewWithChildren(new Style
        {
            flexWrap = FlexWrap.Wrap,
            alignItems = AlignItems.Center,
            size = new Size<Dimension>(T.Px(100), Dimension.Auto())
        }, [c0, c1, c2, c3]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 60, "root");
        T.AssertLayout(tree.Layout(c0), 0, 10, 30, 10, "c0");
        T.AssertLayout(tree.Layout(c1), 30, 5, 30, 20, "c1");
        T.AssertLayout(tree.Layout(c2), 60, 0, 30, 30, "c2");
        T.AssertLayout(tree.Layout(c3), 0, 30, 30, 30, "c3");
    }

    [Fact]
    public void WrapReverseColumn()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(31)) });
        var c1 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(32)) });
        var c2 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(33)) });
        var c3 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(30), T.Px(34)) });
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            flexWrap = FlexWrap.WrapReverse,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2, c3]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 70, 0, 30, 31, "c0");
        T.AssertLayout(tree.Layout(c1), 70, 31, 30, 32, "c1");
        T.AssertLayout(tree.Layout(c2), 70, 63, 30, 33, "c2");
        T.AssertLayout(tree.Layout(c3), 20, 0, 30, 34, "c3");
    }
}

public class AlignItemsTests
{
    [Fact]
    public void AlignItemsStretch()
    {
        var tree = new TaffyTree();
        var child = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(10), Dimension.Auto()) });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child), 0, 0, 10, 100, "child");
    }

    [Fact]
    public void AlignSelfCenter()
    {
        var tree = new TaffyTree();
        var child = tree.NewLeaf(new Style
        {
            alignSelf = AlignItems.Center,
            size = new Size<Dimension>(T.Px(10), T.Px(10))
        });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child), 0, 45, 10, 10, "child");
    }

    [Fact]
    public void AlignBaseline()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(50), T.Px(50)) });
        var c1 = tree.NewLeaf(new Style { size = new Size<Dimension>(T.Px(50), T.Px(20)) });
        var root = tree.NewWithChildren(new Style
        {
            alignItems = AlignItems.Baseline,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 50, 50, "c0");
        T.AssertLayout(tree.Layout(c1), 50, 30, 50, 20, "c1");
    }
}

public class JustifyContentColumnTests
{
    [Fact]
    public void JustifyContentColumnCenter()
    {
        var tree = new TaffyTree();
        var child = new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) };
        var c0 = tree.NewLeaf(child);
        var c1 = tree.NewLeaf(child);
        var c2 = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            justifyContent = AlignContent.Center,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 35, 100, 10, "c0");
        T.AssertLayout(tree.Layout(c1), 0, 45, 100, 10, "c1");
        T.AssertLayout(tree.Layout(c2), 0, 55, 100, 10, "c2");
    }

    [Fact]
    public void JustifyContentColumnSpaceBetween()
    {
        var tree = new TaffyTree();
        var child = new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) };
        var c0 = tree.NewLeaf(child);
        var c1 = tree.NewLeaf(child);
        var c2 = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            justifyContent = AlignContent.SpaceBetween,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 100, 10, "c0");
        T.AssertLayout(tree.Layout(c1), 0, 45, 100, 10, "c1");
        T.AssertLayout(tree.Layout(c2), 0, 90, 100, 10, "c2");
    }

    [Fact]
    public void JustifyContentColumnSpaceEvenly()
    {
        var tree = new TaffyTree();
        var child = new Style { size = new Size<Dimension>(Dimension.Auto(), T.Px(10)) };
        var c0 = tree.NewLeaf(child);
        var c1 = tree.NewLeaf(child);
        var c2 = tree.NewLeaf(child);
        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            justifyContent = AlignContent.SpaceEvenly,
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 18, 100, 10, "c0");
        T.AssertLayout(tree.Layout(c1), 0, 45, 100, 10, "c1");
        T.AssertLayout(tree.Layout(c2), 0, 73, 100, 10, "c2");
    }
}

public class PaddingAndGapTests
{
    [Fact]
    public void PaddingFlexChild()
    {
        var tree = new TaffyTree();
        var child = tree.NewLeaf(new Style
        {
            flexGrow = 1,
            size = new Size<Dimension>(T.Px(10), Dimension.Auto())
        });
        var root = tree.NewWithChildren(new Style
        {
            padding = T.Padding(10),
            size = new Size<Dimension>(T.Px(100), T.Px(100))
        }, [child]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 100, 100, "root");
        T.AssertLayout(tree.Layout(child), 10, 10, 80, 80, "child");
    }

    [Fact]
    public void GapColumnGap()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = Dimension.Percent(0) });
        var c1 = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = Dimension.Percent(0) });
        var c2 = tree.NewLeaf(new Style { flexGrow = 1, flexBasis = Dimension.Percent(0) });
        var root = tree.NewWithChildren(new Style
        {
            gap = new Size<LengthPercentage>(LengthPercentage.Length(10), LengthPercentage.Length(20)),
            size = new Size<Dimension>(T.Px(80), T.Px(100))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 80, 100, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 20, 100, "c0");
        T.AssertLayout(tree.Layout(c1), 30, 0, 20, 100, "c1");
        T.AssertLayout(tree.Layout(c2), 60, 0, 20, 100, "c2");
    }
}

public class FlexGrowEdgeCaseTests
{
    [Fact]
    public void FlexGrowLessThanFactorOne()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style { flexGrow = 0.2f, flexShrink = 0, flexBasis = T.Px(40) });
        var c1 = tree.NewLeaf(new Style { flexGrow = 0.2f, flexShrink = 0 });
        var c2 = tree.NewLeaf(new Style { flexGrow = 0.4f, flexShrink = 0 });
        var root = tree.NewWithChildren(new Style
        {
            size = new Size<Dimension>(T.Px(500), T.Px(200))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 500, 200, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 132, 200, "c0");
        T.AssertLayout(tree.Layout(c1), 132, 0, 92, 200, "c1");
        T.AssertLayout(tree.Layout(c2), 224, 0, 184, 200, "c2");
    }
}
