using Taffy;
using Xunit;

namespace TaffyTests;

public class NestedFlexRegressionTests
{
    [Fact]
    public void RoundingTotalFractialNested_BorderBoxLtr()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style
        {
            flexGrow = 1,
            flexBasis = T.Px(0.3f),
            size = new Size<Dimension>(Dimension.AUTO, T.Px(9.9f)),
            inset = new Rect<LengthPercentageAuto>(
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.Length(13.3f))
        });

        var child1 = tree.NewLeaf(new Style
        {
            flexGrow = 4,
            flexBasis = T.Px(0.3f),
            size = new Size<Dimension>(Dimension.AUTO, T.Px(1.1f)),
            inset = new Rect<LengthPercentageAuto>(
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.Length(13.3f),
                LengthPercentageAuto.AUTO)
        });

        var nested = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            flexGrow = 0.7f,
            flexBasis = T.Px(50.3f),
            size = new Size<Dimension>(Dimension.AUTO, T.Px(20.3f))
        }, [child0, child1]);

        var sibling0 = tree.NewLeaf(new Style
        {
            flexGrow = 1.6f,
            size = new Size<Dimension>(Dimension.AUTO, T.Px(10))
        });

        var sibling1 = tree.NewLeaf(new Style
        {
            flexGrow = 1.1f,
            size = new Size<Dimension>(Dimension.AUTO, T.Px(10.7f))
        });

        var root = tree.NewWithChildren(new Style
        {
            flexDirection = FlexDirection.Column,
            size = new Size<Dimension>(T.Px(87.4f), T.Px(113.4f))
        }, [nested, sibling0, sibling1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 87, 113, "root");
        T.AssertLayout(tree.Layout(nested), 0, 0, 87, 59, "nested");
        T.AssertLayout(tree.Layout(child0), 0, -13, 87, 12, "child0");
        T.AssertLayout(tree.Layout(child1), 0, 25, 87, 47, "child1");
        T.AssertLayout(tree.Layout(sibling0), 0, 59, 87, 30, "sibling0");
        T.AssertLayout(tree.Layout(sibling1), 0, 89, 87, 24, "sibling1");
    }

    [Fact]
    public void RoundingTotalFractialNested_ContentBoxLtr()
    {
        var tree = new TaffyTree();

        var child0 = tree.NewLeaf(new Style
        {
            boxSizing = BoxSizing.ContentBox,
            flexGrow = 1,
            flexBasis = T.Px(0.3f),
            size = new Size<Dimension>(Dimension.AUTO, T.Px(9.9f)),
            inset = new Rect<LengthPercentageAuto>(
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.Length(13.3f))
        });

        var child1 = tree.NewLeaf(new Style
        {
            boxSizing = BoxSizing.ContentBox,
            flexGrow = 4,
            flexBasis = T.Px(0.3f),
            size = new Size<Dimension>(Dimension.AUTO, T.Px(1.1f)),
            inset = new Rect<LengthPercentageAuto>(
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.AUTO,
                LengthPercentageAuto.Length(13.3f),
                LengthPercentageAuto.AUTO)
        });

        var nested = tree.NewWithChildren(new Style
        {
            boxSizing = BoxSizing.ContentBox,
            flexDirection = FlexDirection.Column,
            flexGrow = 0.7f,
            flexBasis = T.Px(50.3f),
            size = new Size<Dimension>(Dimension.AUTO, T.Px(20.3f))
        }, [child0, child1]);

        var sibling0 = tree.NewLeaf(new Style
        {
            boxSizing = BoxSizing.ContentBox,
            flexGrow = 1.6f,
            size = new Size<Dimension>(Dimension.AUTO, T.Px(10))
        });

        var sibling1 = tree.NewLeaf(new Style
        {
            boxSizing = BoxSizing.ContentBox,
            flexGrow = 1.1f,
            size = new Size<Dimension>(Dimension.AUTO, T.Px(10.7f))
        });

        var root = tree.NewWithChildren(new Style
        {
            boxSizing = BoxSizing.ContentBox,
            flexDirection = FlexDirection.Column,
            size = new Size<Dimension>(T.Px(87.4f), T.Px(113.4f))
        }, [nested, sibling0, sibling1]);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), 0, 0, 87, 113, "root");
        T.AssertLayout(tree.Layout(nested), 0, 0, 87, 59, "nested");
        T.AssertLayout(tree.Layout(child0), 0, -13, 87, 12, "child0");
        T.AssertLayout(tree.Layout(child1), 0, 25, 87, 47, "child1");
        T.AssertLayout(tree.Layout(sibling0), 0, 59, 87, 30, "sibling0");
        T.AssertLayout(tree.Layout(sibling1), 0, 89, 87, 24, "sibling1");
    }
}
