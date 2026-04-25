using Taffy;
using Xunit;

namespace TaffyTests;

file static class G
{
    public static List<GridTemplateComponent> Tracks40x3()
    {
        return [TrackSizingFunction.Px(40), TrackSizingFunction.Px(40), TrackSizingFunction.Px(40)];
    }

    public static List<GridTemplateComponent> Tracks(params TrackSizingFunction[] tracks)
    {
        return [.. tracks.Select(static track => (GridTemplateComponent)track)];
    }
}

public class GridBasicTests
{
    [Fact]
    public void Grid3x3()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (var i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(120), T.Px(120)),
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows = G.Tracks40x3()
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 120, 120, "root");
        T.AssertLayout(tree.Layout(children[0]), 0, 0, 40, 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), 40, 0, 40, 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), 80, 0, 40, 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), 0, 40, 40, 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), 40, 40, 40, 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), 80, 40, 40, 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), 0, 80, 40, 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), 40, 80, 40, 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), 80, 80, 40, 40, "c8");
    }

    [Fact]
    public void ImplicitRowGrowsToFitContent()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style());
        var c1 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(35), T.Px(35))
        });
        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Px(40)),
            gridTemplateRows = G.Tracks(TrackSizingFunction.Px(40))
        }, [c0, c1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 40, 75, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 40, 40, "c0");
        T.AssertLayout(tree.Layout(c1), 0, 40, 35, 35, "c1");
    }

    [Fact]
    public void GridWithPadding()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (var i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            padding = new Rect<LengthPercentage>(T.LPx(40), T.LPx(20), T.LPx(10), T.LPx(30)),
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows = G.Tracks40x3()
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 180, 160, "root");
        T.AssertLayout(tree.Layout(children[0]), 40, 10, 40, 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), 80, 10, 40, 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), 120, 10, 40, 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), 40, 50, 40, 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), 80, 50, 40, 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), 120, 50, 40, 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), 40, 90, 40, 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), 80, 90, 40, 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), 120, 90, 40, 40, "c8");
    }
}

public class GridGapTests
{
    [Fact]
    public void GridWithGap()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (var i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(200), T.Px(200)),
            gap = new Size<LengthPercentage>(T.LPx(40), T.LPx(40)),
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows = G.Tracks40x3()
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 200, 200, "root");
        T.AssertLayout(tree.Layout(children[0]), 0, 0, 40, 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), 80, 0, 40, 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), 160, 0, 40, 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), 0, 80, 40, 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), 80, 80, 40, 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), 160, 80, 40, 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), 0, 160, 40, 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), 80, 160, 40, 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), 160, 160, 40, 40, "c8");
    }
}

public class GridFrTests
{
    [Fact]
    public void FrProportions()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style());
        var c1 = tree.NewLeaf(new Style());
        var c2 = tree.NewLeaf(new Style());
        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(200), Dimension.Auto()),
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(2), TrackSizingFunction.Fr(3)),
            gridTemplateRows = G.Tracks(TrackSizingFunction.Px(40))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 200, 40, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 33, 40, "c0");
        T.AssertLayout(tree.Layout(c1), 33, 0, 67, 40, "c1");
        T.AssertLayout(tree.Layout(c2), 100, 0, 100, 40, "c2");
    }

    [Fact]
    public void FrWithFixedSizeItem()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (var i = 0; i < 9; i++)
        {
            var style = new Style();
            if (i == 4)
                style.size = new Size<Dimension>(T.Px(100), Dimension.Auto());
            children[i] = tree.NewLeaf(style);
        }

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(200), T.Px(200)),
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Px(40), TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(1)),
            gridTemplateRows = G.Tracks(TrackSizingFunction.Px(40), TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(1))
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 200, 200, "root");
        T.AssertLayout(tree.Layout(children[0]), 0, 0, 40, 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), 40, 0, 100, 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), 140, 0, 60, 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), 0, 40, 40, 80, "c3");
        T.AssertLayout(tree.Layout(children[4]), 40, 40, 100, 80, "c4");
        T.AssertLayout(tree.Layout(children[5]), 140, 40, 60, 80, "c5");
        T.AssertLayout(tree.Layout(children[6]), 0, 120, 40, 80, "c6");
        T.AssertLayout(tree.Layout(children[7]), 40, 120, 100, 80, "c7");
        T.AssertLayout(tree.Layout(children[8]), 140, 120, 60, 80, "c8");
    }

    [Fact]
    public void FrSpan2Proportion()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(60), Dimension.Auto()),
            gridColumn = new Line<GridPlacement>(GridPlacement.Auto, GridPlacement.Span(2))
        });
        var c1 = tree.NewLeaf(new Style());
        var c2 = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(2)),
            gridTemplateRows = G.Tracks(TrackSizingFunction.Px(40), TrackSizingFunction.Px(40))
        }, [c0, c1, c2]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 60, 80, "root");
        T.AssertLayout(tree.Layout(c0), 0, 0, 60, 40, "c0");
        T.AssertLayout(tree.Layout(c1), 0, 40, 20, 40, "c1");
        T.AssertLayout(tree.Layout(c2), 20, 40, 40, 40, "c2");
    }
}

public class GridAlignContentTests
{
    [Fact]
    public void AlignContentCenter()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (var i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(200), T.Px(200)),
            alignContent = AlignContent.Center,
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows = G.Tracks40x3()
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 200, 200, "root");
        T.AssertLayout(tree.Layout(children[0]), 0, 40, 40, 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), 40, 40, 40, 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), 80, 40, 40, 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), 0, 80, 40, 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), 40, 80, 40, 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), 80, 80, 40, 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), 0, 120, 40, 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), 40, 120, 40, 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), 80, 120, 40, 40, "c8");
    }

    [Fact]
    public void AlignContentSpaceBetween()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (var i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(200), T.Px(200)),
            alignContent = AlignContent.SpaceBetween,
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows = G.Tracks40x3()
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 200, 200, "root");
        T.AssertLayout(tree.Layout(children[0]), 0, 0, 40, 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), 40, 0, 40, 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), 80, 0, 40, 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), 0, 80, 40, 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), 40, 80, 40, 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), 80, 80, 40, 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), 0, 160, 40, 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), 40, 160, 40, 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), 80, 160, 40, 40, "c8");
    }
}

public class GridAlignItemsTests
{
    [Fact]
    public void AlignItemsCenter()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(20), T.Px(20)),
            gridRow = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Auto)
        });
        var c1 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(60), T.Px(60)),
            gridRow = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Auto)
        });

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(120), T.Px(120)),
            alignItems = AlignItems.Center,
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows = G.Tracks40x3()
        }, [c0, c1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 120, 120, "root");
        T.AssertLayout(tree.Layout(c0), 0, 10, 20, 20, "c0");
        T.AssertLayout(tree.Layout(c1), 80, 70, 60, 60, "c1");
    }

    [Fact]
    public void AlignItemsStretchDefault()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(20), Dimension.Auto())
        });
        var c1 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(20), Dimension.Auto())
        });

        var root = tree.NewWithChildren(new Style
        {
            display = Display.Grid,
            size = new Size<Dimension>(T.Px(100), T.Px(50)),
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Px(50), TrackSizingFunction.Px(50)),
            gridTemplateRows = G.Tracks(TrackSizingFunction.Px(50))
        }, [c0, c1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(c0), 0, 0, 20, 50, "c0");
        T.AssertLayout(tree.Layout(c1), 50, 0, 20, 50, "c1");
    }
}
