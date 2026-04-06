// CSS Grid layout tests ported from taffy/tests/xml/grid/*.xml
// Each test references its source XML file.

using Taffy;
using Xunit;

namespace TaffyTests;

// Helper: three 40px columns
file static class G
{
    public static List<TrackSizingFunction> Tracks40x3() =>
        new() { TrackSizingFunction.Px(40), TrackSizingFunction.Px(40), TrackSizingFunction.Px(40) };

    public static List<TrackSizingFunction> Tracks(params TrackSizingFunction[] tracks) =>
        new(tracks);
}

// ─────────────────────────────────────────────────────────────────────────────
// Basic grid
// ─────────────────────────────────────────────────────────────────────────────

public class GridBasicTests
{
    // Reference: grid_basic__border_box_ltr.xml
    // 3×3 grid, 120×120, each cell 40×40.
    [Fact]
    public void Grid3x3()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (int i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display              = Display.Grid,
            size                 = new Size<Dimension>(T.Px(120), T.Px(120)),
            gridTemplateColumns  = G.Tracks40x3(),
            gridTemplateRows     = G.Tracks40x3(),
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 120, h: 120, "root");

        // Row 0
        T.AssertLayout(tree.Layout(children[0]), x:  0, y:  0, w: 40, h: 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), x: 40, y:  0, w: 40, h: 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), x: 80, y:  0, w: 40, h: 40, "c2");
        // Row 1
        T.AssertLayout(tree.Layout(children[3]), x:  0, y: 40, w: 40, h: 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), x: 40, y: 40, w: 40, h: 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), x: 80, y: 40, w: 40, h: 40, "c5");
        // Row 2
        T.AssertLayout(tree.Layout(children[6]), x:  0, y: 80, w: 40, h: 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), x: 40, y: 80, w: 40, h: 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), x: 80, y: 80, w: 40, h: 40, "c8");
    }

    // Reference: grid_basic_implicit_tracks__border_box_ltr.xml
    // 1 explicit 40px column/row; second item is 35×35 → implicit row of 35px.
    // Root should size to 40×75.
    [Fact]
    public void ImplicitRowGrowsToFitContent()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style());
        var c1 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(35), T.Px(35)),
        });
        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Px(40)),
            gridTemplateRows    = G.Tracks(TrackSizingFunction.Px(40)),
        }, new[] { c0, c1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 40, h: 75, "root");
        T.AssertLayout(tree.Layout(c0),   x: 0, y:  0, w: 40, h: 40, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 0, y: 40, w: 35, h: 35, "c1");
    }

    // Reference: grid_basic_with_padding__border_box_ltr.xml
    // 3×3 grid with asymmetric padding (left=40, right=20, top=10, bottom=30).
    // Root grows to fit: w = 40+120+20=180, h = 10+120+30=160.
    [Fact]
    public void GridWithPadding()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (int i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            padding             = new Rect<LengthPercentage>(
                T.LPx(40), T.LPx(20), T.LPx(10), T.LPx(30)), // left, right, top, bottom
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows    = G.Tracks40x3(),
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 180, h: 160, "root");
        // Row 0 starts at x=40, y=10
        T.AssertLayout(tree.Layout(children[0]), x:  40, y: 10, w: 40, h: 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), x:  80, y: 10, w: 40, h: 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), x: 120, y: 10, w: 40, h: 40, "c2");
        // Row 1
        T.AssertLayout(tree.Layout(children[3]), x:  40, y: 50, w: 40, h: 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), x:  80, y: 50, w: 40, h: 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), x: 120, y: 50, w: 40, h: 40, "c5");
        // Row 2
        T.AssertLayout(tree.Layout(children[6]), x:  40, y: 90, w: 40, h: 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), x:  80, y: 90, w: 40, h: 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), x: 120, y: 90, w: 40, h: 40, "c8");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Gap
// ─────────────────────────────────────────────────────────────────────────────

public class GridGapTests
{
    // Reference: grid_gap__border_box_ltr.xml
    // 3×3 in a 200×200 container with 40px row and column gaps.
    // Columns at x=0,80,160; rows at y=0,80,160.
    [Fact]
    public void GridWithGap()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (int i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(200), T.Px(200)),
            gap                 = new Size<LengthPercentage>(T.LPx(40), T.LPx(40)),
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows    = G.Tracks40x3(),
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 200, h: 200, "root");
        // Row 0
        T.AssertLayout(tree.Layout(children[0]), x:   0, y:   0, w: 40, h: 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), x:  80, y:   0, w: 40, h: 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), x: 160, y:   0, w: 40, h: 40, "c2");
        // Row 1
        T.AssertLayout(tree.Layout(children[3]), x:   0, y:  80, w: 40, h: 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), x:  80, y:  80, w: 40, h: 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), x: 160, y:  80, w: 40, h: 40, "c5");
        // Row 2
        T.AssertLayout(tree.Layout(children[6]), x:   0, y: 160, w: 40, h: 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), x:  80, y: 160, w: 40, h: 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), x: 160, y: 160, w: 40, h: 40, "c8");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Fr units
// ─────────────────────────────────────────────────────────────────────────────

public class GridFrTests
{
    // Reference: grid_fr_fixed_size_no_content_proportions__border_box_ltr.xml
    // 200px wide, columns = 1fr 2fr 3fr (6fr total).
    // Expected widths: 33, 67, 100.
    [Fact]
    public void FrProportions()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style());
        var c1 = tree.NewLeaf(new Style());
        var c2 = tree.NewLeaf(new Style());
        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(200), Dimension.Auto()),
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(2), TrackSizingFunction.Fr(3)),
            gridTemplateRows    = G.Tracks(TrackSizingFunction.Px(40)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 200, h: 40, "root");
        T.AssertLayout(tree.Layout(c0),   x:   0, y: 0, w:  33, h: 40, "c0");
        T.AssertLayout(tree.Layout(c1),   x:  33, y: 0, w:  67, h: 40, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 100, y: 0, w: 100, h: 40, "c2");
    }

    // Reference: grid_fr_fixed_size_single_item__border_box_ltr.xml
    // 200×200; columns = 40px 1fr 1fr; rows = 40px 1fr 1fr.
    // One item in cell (2,2) has explicit width=100px, which sizes that column to 100px.
    // Remaining fr column = 200-40-100 = 60px. Remaining fr rows share 200-40=160px → 80px each.
    [Fact]
    public void FrWithFixedSizeItem()
    {
        var tree = new TaffyTree();
        // 9 items; item at position [1,1] (0-indexed row 1, col 1) has width=100
        var children = new NodeId[9];
        for (int i = 0; i < 9; i++)
        {
            var style = new Style();
            if (i == 4) // row 1, col 1 (middle cell)
                style.size = new Size<Dimension>(T.Px(100), Dimension.Auto());
            children[i] = tree.NewLeaf(style);
        }

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(200), T.Px(200)),
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Px(40), TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(1)),
            gridTemplateRows    = G.Tracks(TrackSizingFunction.Px(40), TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(1)),
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 200, h: 200, "root");
        // Row 0: h=40
        T.AssertLayout(tree.Layout(children[0]), x:   0, y:  0, w:  40, h:  40, "c0");
        T.AssertLayout(tree.Layout(children[1]), x:  40, y:  0, w: 100, h:  40, "c1");
        T.AssertLayout(tree.Layout(children[2]), x: 140, y:  0, w:  60, h:  40, "c2");
        // Row 1: h=80
        T.AssertLayout(tree.Layout(children[3]), x:   0, y: 40, w:  40, h:  80, "c3");
        T.AssertLayout(tree.Layout(children[4]), x:  40, y: 40, w: 100, h:  80, "c4");
        T.AssertLayout(tree.Layout(children[5]), x: 140, y: 40, w:  60, h:  80, "c5");
        // Row 2: h=80
        T.AssertLayout(tree.Layout(children[6]), x:   0, y: 120, w:  40, h:  80, "c6");
        T.AssertLayout(tree.Layout(children[7]), x:  40, y: 120, w: 100, h:  80, "c7");
        T.AssertLayout(tree.Layout(children[8]), x: 140, y: 120, w:  60, h:  80, "c8");
    }

    // Reference: grid_fr_span_2_proportion__border_box_ltr.xml
    // Columns = 1fr 2fr; rows = 40px 40px.
    // First item has width=60px and spans 2 columns → sets total width to 60px.
    // 1fr = 20px, 2fr = 40px.
    [Fact]
    public void FrSpan2Proportion()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style
        {
            size      = new Size<Dimension>(T.Px(60), Dimension.Auto()),
            gridColumn = new Line<GridPlacement>(GridPlacement.Auto, GridPlacement.Span(2)),
        });
        var c1 = tree.NewLeaf(new Style());
        var c2 = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Fr(1), TrackSizingFunction.Fr(2)),
            gridTemplateRows    = G.Tracks(TrackSizingFunction.Px(40), TrackSizingFunction.Px(40)),
        }, new[] { c0, c1, c2 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 60, h: 80, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y:  0, w: 60, h: 40, "c0");
        T.AssertLayout(tree.Layout(c1),   x:  0, y: 40, w: 20, h: 40, "c1");
        T.AssertLayout(tree.Layout(c2),   x: 20, y: 40, w: 40, h: 40, "c2");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// align-content
// ─────────────────────────────────────────────────────────────────────────────

public class GridAlignContentTests
{
    // Reference: grid_align_content_center__border_box_ltr.xml
    // 3×3 grid (120px tall) inside a 200×200 container with align-content=center.
    // Free space = 80, offset = 40.
    [Fact]
    public void AlignContentCenter()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (int i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(200), T.Px(200)),
            alignContent        = AlignContent.Center,
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows    = G.Tracks40x3(),
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 200, h: 200, "root");
        // Rows offset by 40 (centred)
        T.AssertLayout(tree.Layout(children[0]), x:  0, y:  40, w: 40, h: 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), x: 40, y:  40, w: 40, h: 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), x: 80, y:  40, w: 40, h: 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), x:  0, y:  80, w: 40, h: 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), x: 40, y:  80, w: 40, h: 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), x: 80, y:  80, w: 40, h: 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), x:  0, y: 120, w: 40, h: 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), x: 40, y: 120, w: 40, h: 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), x: 80, y: 120, w: 40, h: 40, "c8");
    }

    // Reference: grid_align_content_space_between__border_box_ltr.xml
    // 3×3 in 200×200 with align-content=space-between.
    // Free space = 80; 2 gaps → 40 each. Rows at y=0, 80, 160.
    [Fact]
    public void AlignContentSpaceBetween()
    {
        var tree = new TaffyTree();
        var children = new NodeId[9];
        for (int i = 0; i < 9; i++)
            children[i] = tree.NewLeaf(new Style());

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(200), T.Px(200)),
            alignContent        = AlignContent.SpaceBetween,
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows    = G.Tracks40x3(),
        }, children);

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x: 0, y: 0, w: 200, h: 200, "root");
        T.AssertLayout(tree.Layout(children[0]), x:  0, y:   0, w: 40, h: 40, "c0");
        T.AssertLayout(tree.Layout(children[1]), x: 40, y:   0, w: 40, h: 40, "c1");
        T.AssertLayout(tree.Layout(children[2]), x: 80, y:   0, w: 40, h: 40, "c2");
        T.AssertLayout(tree.Layout(children[3]), x:  0, y:  80, w: 40, h: 40, "c3");
        T.AssertLayout(tree.Layout(children[4]), x: 40, y:  80, w: 40, h: 40, "c4");
        T.AssertLayout(tree.Layout(children[5]), x: 80, y:  80, w: 40, h: 40, "c5");
        T.AssertLayout(tree.Layout(children[6]), x:  0, y: 160, w: 40, h: 40, "c6");
        T.AssertLayout(tree.Layout(children[7]), x: 40, y: 160, w: 40, h: 40, "c7");
        T.AssertLayout(tree.Layout(children[8]), x: 80, y: 160, w: 40, h: 40, "c8");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// align-items / align-self
// ─────────────────────────────────────────────────────────────────────────────

public class GridAlignItemsTests
{
    // Reference: grid_align_items_sized_center__border_box_ltr.xml
    // 3×3 grid 120×120; align-items=center.
    // Item 0 (20×20) at grid-row=1, grid-column=1 → centered in 40×40 cell → y=10, x=0.
    // Item 1 (60×60) at grid-row=3, grid-column=3 → centered in 40×40 cell, overflows → y=70, x=80.
    [Fact]
    public void AlignItemsCenter()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style
        {
            size      = new Size<Dimension>(T.Px(20), T.Px(20)),
            gridRow    = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Auto),
        });
        var c1 = tree.NewLeaf(new Style
        {
            size      = new Size<Dimension>(T.Px(60), T.Px(60)),
            gridRow    = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Auto),
        });

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(120), T.Px(120)),
            alignItems          = AlignItems.Center,
            gridTemplateColumns = G.Tracks40x3(),
            gridTemplateRows    = G.Tracks40x3(),
        }, new[] { c0, c1 });

        tree.ComputeLayout(root, T.MaxContent());

        T.AssertLayout(tree.Layout(root), x:  0, y:  0, w: 120, h: 120, "root");
        T.AssertLayout(tree.Layout(c0),   x:  0, y: 10, w:  20, h:  20, "c0");
        T.AssertLayout(tree.Layout(c1),   x: 80, y: 70, w:  60, h:  60, "c1");
    }

    // Explicit placement with auto-placement default (stretch): items fill their cell height.
    [Fact]
    public void AlignItemsStretchDefault()
    {
        var tree = new TaffyTree();
        var c0 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(20), Dimension.Auto()),
        });
        var c1 = tree.NewLeaf(new Style
        {
            size = new Size<Dimension>(T.Px(20), Dimension.Auto()),
        });

        var root = tree.NewWithChildren(new Style
        {
            display             = Display.Grid,
            size                = new Size<Dimension>(T.Px(100), T.Px(50)),
            gridTemplateColumns = G.Tracks(TrackSizingFunction.Px(50), TrackSizingFunction.Px(50)),
            gridTemplateRows    = G.Tracks(TrackSizingFunction.Px(50)),
        }, new[] { c0, c1 });

        tree.ComputeLayout(root, T.MaxContent());

        // Both items should stretch to fill the 50px row height
        T.AssertLayout(tree.Layout(c0), x:  0, y: 0, w: 20, h: 50, "c0");
        T.AssertLayout(tree.Layout(c1), x: 50, y: 0, w: 20, h: 50, "c1");
    }
}
