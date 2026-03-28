using System.Collections.Generic;
using HotSwap;
using PawnEditor.TaffySharp;
using UnityEngine;
using Verse;
using Display = PawnEditor.TaffySharp.Display;

namespace PawnEditor;

[HotSwappable]
public class Window_Dev : Window
{
    public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight) - new Vector2(128f, 128f);

    public Window_Dev()
    {
        resizeable = true;
        draggable = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Taffy.Div(inRect, new Style { display = Display.Block }, div =>
        {
            div.Div(new Style { display = Display.Flex, flexWrap = FlexWrap.Wrap }, div1 =>
            {
                for (var i = 0; i < 10; i++)
                {
                    div1.Div(new Style(), div2 =>
                    {
                        div2.Text("Hello");
                        div2.Item(100f, draw: rect => { Verse.Widgets.DrawRectFast(rect, Color.red); });
                    });
                }
            });
        });
    }


    private static readonly Color[] ItemColors =
    [
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.cyan, Color.magenta, Color.white, Color.grey
    ];

    private int _itemCount = 3;

    private void DrawFlexLayout(Rect inRect)
    {
        Taffy.Column(inRect, col =>
        {
            col.Row(gap: 8f, grow: 0f, row =>
            {
                row.Item(width: 100f, height: 40f, draw: rect => Verse.Widgets.DrawRectFast(rect, Color.red));
                row.Item(width: 80f, height: 40f, draw: rect =>
                {
                    if (Verse.Widgets.ButtonText(rect, "+ Add")) _itemCount = Mathf.Min(_itemCount + 1, 50);
                });
                row.Item(width: 90f, height: 40f, draw: rect =>
                {
                    if (Verse.Widgets.ButtonText(rect, "- Remove")) _itemCount = Mathf.Max(_itemCount - 1, 0);
                });
                row.Item(grow: 1f, height: 40f, draw: rect => Verse.Widgets.Label(rect, $"Items: {_itemCount}"));
                row.Item(width: 100f, height: 40f, draw: rect => Verse.Widgets.DrawRectFast(rect, Color.gray));
            });
            col.Row(new Style { flexGrow = 1f, flexWrap = FlexWrap.Wrap }, row =>
            {
                for (var i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    row.Item(width: 100f, height: 100f, draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
            col.Row(new Style { flexWrap = FlexWrap.Wrap }, row =>
            {
                for (var i = 0; i < _itemCount; i++)
                {
                    var color = ItemColors[i % ItemColors.Length];
                    row.Item(width: 50f, height: 50f, draw: rect => Verse.Widgets.DrawRectFast(rect, color));
                }
            });
        });
    }

    private void DrawGridLayout(Rect inRect)
    {
        // 3-column grid: [1fr, 2fr, 1fr], 8px gap
        Taffy.Grid(inRect, [Taffy.Fr(), Taffy.Fr(2), Taffy.Fr()], gap: 8f, autoRowHeight: 40f, grid =>
        {
            // Row 1: three independent cells
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.red);
                Verse.Widgets.Label(r, "A (1fr)");
            });
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.green);
                Verse.Widgets.Label(r, "B (2fr)");
            });
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.blue);
                Verse.Widgets.Label(r, "C (1fr)");
            });

            // Row 2: one cell spanning all 3 columns
            grid.GridItem(colSpan: 3,
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.yellow);
                    Verse.Widgets.Label(r, "D — colSpan 3");
                });

            // Row 3: a 2-wide cell then a 1-wide cell
            grid.GridItem(colSpan: 2,
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.cyan);
                    Verse.Widgets.Label(r, "E — colSpan 2");
                });
            grid.GridItem(draw: r =>
            {
                Verse.Widgets.DrawRectFast(r, Color.magenta);
                Verse.Widgets.Label(r, "F");
            });

            // Row 4: G locked to col 2 (colStart=2); cursor was past col 2 so row advances.
            //         H auto-placed at col 3 (cursor continues forward). Col 1 left empty (sparse).
            grid.GridItem(colStart: 2,
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.grey);
                    Verse.Widgets.Label(r, "G (start=2)");
                });
            grid.GridItem(
                draw: r =>
                {
                    Verse.Widgets.DrawRectFast(r, Color.white);
                    Verse.Widgets.Label(r, "H");
                });
        });
    }

    // Demonstrates TextItem auto-sizing: each row's height is driven by text content, not a fixed value.
    // The short label rows should be single-line; the long paragraph row should wrap and grow taller.
    private void DrawTextItemLayout(Rect inRect)
    {
        var bg = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        var hl = new Color(0.25f, 0.45f, 0.8f, 0.4f);
        var sep = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        const string paragraph =
            "This is a long paragraph that should wrap across multiple lines when the available " +
            "width is less than the natural text width. The row height must grow to fit all the " +
            "wrapped lines — if TextItem is working correctly you will see the full text here " +
            "without clipping, and rows above and below will remain single-line.";

        Taffy.Column(inRect.LeftPart(0.5f), gap: 8f, col =>
        {
            // // Row 1: short single-line label — height should be one line tall
            // col.Row(grow: 0f, row =>
            // {
            //     row.Item(width: 140f, draw: r =>
            //     {
            //         Verse.Widgets.DrawRectFast(r, hl);
            //         Verse.Widgets.Label(r, "Short label:");
            //     });
            //     row.TextItem("Hello, TextItem!", grow: 1f, draw: r =>
            //     {
            //         Verse.Widgets.DrawRectFast(r, bg);
            //         Verse.Widgets.Label(r, "Hello, TextItem!");
            //     });
            // });
            //
            // // Separator
            // col.Item(height: 1f, grow: 0f, draw: r => Verse.Widgets.DrawRectFast(r, sep));

            // Row 2: the wrapping paragraph — height driven entirely by text content
            col.Row(grow: 0f, row =>
            {
                row.Text("Paragraph:");
                row.Text(paragraph);
                // row.TextItem(paragraph, grow: 1f, draw: r =>
                // {
                //     Verse.Widgets.DrawRectFast(r, bg);
                //     Verse.Widgets.DrawBox(r);
                //     Text.WordWrap = true;
                //     Verse.Widgets.Label(r, paragraph);
                // });
            });

            // // Separator
            // col.Item(height: 1f, grow: 0f, draw: r => Verse.Widgets.DrawRectFast(r, sep));
            //
            // // Row 3: two TextItems side by side, each in half the available width
            // col.Row(grow: 0f, row =>
            // {
            //     row.TextItem("Left column — auto height", grow: 1f, draw: r =>
            //     {
            //         Verse.Widgets.DrawRectFast(r, bg);
            //         Verse.Widgets.Label(r, "Left column — auto height");
            //     });
            //     row.Item(width: 8f); // gap
            //     row.TextItem("Right column — also auto height", grow: 1f, draw: r =>
            //     {
            //         Verse.Widgets.DrawRectFast(r, bg);
            //         Verse.Widgets.Label(r, "Right column — also auto height");
            //     });
            // });
            //
            // // Row 4: large font TextItem — height should be taller than the small-font rows
            // col.Item(height: 1f, grow: 0f, draw: r => Verse.Widgets.DrawRectFast(r, sep));
            // col.Row(grow: 0f, row =>
            // {
            //     row.Item(width: 140f, draw: r =>
            //     {
            //         Verse.Widgets.DrawRectFast(r, hl);
            //         Verse.Widgets.Label(r, "Large font:");
            //     });
            //     row.TextItem("Large font label", grow: 1f, font: GameFont.Medium);
            // });
        });
    }

    private string _buttonLog = "—";

    private void DrawButtonItemLayout(Rect inRect)
    {
        Taffy.Column(inRect.LeftPart(0.5f), gap: 8f, col =>
        {
            // Row 1: label-only button
            col.Row(grow: 0f, row =>
            {
                row.Text("Label only:");
                row.Button(label: "Generate name",
                    onClick: () => _buttonLog = "Generate name clicked");
            });

            // Row 2: label + icon button
            col.Row(grow: 0f, row =>
            {
                row.Text("Label + icon:");
                row.Button(label: "Add trait", icon: TexButton.Add, iconColor: Color.green,
                    onClick: () => _buttonLog = "Add trait clicked");
            });

            // Row 3: icon-only button
            col.Row(grow: 0f, row =>
            {
                row.Text("Icon only:");
                row.Button(icon: TexButton.Delete, iconColor: Color.red,
                    onClick: () => _buttonLog = "Delete clicked");
            });

            // Row 4: last clicked feedback
            col.Item(height: 1f, grow: 0f,
                draw: r => Verse.Widgets.DrawRectFast(r, new Color(0.4f, 0.4f, 0.4f, 0.5f)));
            col.Row(grow: 0f, row =>
            {
                row.Text("Last clicked:");
                row.Text(_buttonLog);
            });
        });
    }

    // Demonstrates the low-level TaffyTree API directly — equivalent to taffy's new_leaf /
    // new_with_children / compute_layout pattern shown in examples/grid_holy_grail.rs.
    private void DrawHolyGrailLayout(Rect inRect)
    {
        const float sidebarW = 200f;
        const float headerH = 60f;
        const float footerH = 40f;

        var tree = new TaffyTree();

        // ── Leaf nodes — styles carry only placement ────────────────────────────
        var header = tree.NewLeaf(new Style
        {
            gridRow = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Auto, GridPlacement.Span(3)),
        });
        var leftSidebar = tree.NewLeaf(new Style
        {
            gridRow = new Line<GridPlacement>(GridPlacement.Line(2), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Auto),
        });
        var content = tree.NewLeaf(new Style
        {
            gridRow = new Line<GridPlacement>(GridPlacement.Line(2), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(2), GridPlacement.Auto),
        });
        var rightSidebar = tree.NewLeaf(new Style
        {
            gridRow = new Line<GridPlacement>(GridPlacement.Line(2), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Auto),
        });
        var footer = tree.NewLeaf(new Style
        {
            gridRow = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Auto),
            gridColumn = new Line<GridPlacement>(GridPlacement.Auto, GridPlacement.Span(3)),
        });

        // ── Container — carries the grid template ───────────────────────────────
        var root = tree.NewWithChildren(new Style
        {
            display = TaffySharp.Display.Grid,
            size = new Size<Dimension>(Dimension.Length(inRect.width), Dimension.Length(inRect.height)),
            gridTemplateColumns =
            [
                TrackSizingFunction.Px(sidebarW),
                TrackSizingFunction.Fr(1),
                TrackSizingFunction.Px(sidebarW)
            ],
            gridTemplateRows =
            [
                TrackSizingFunction.Px(headerH),
                TrackSizingFunction.Fr(1),
                TrackSizingFunction.Px(footerH)
            ],
        }, [header, leftSidebar, content, rightSidebar, footer]);

        // ── Compute layout ──────────────────────────────────────────────────────
        tree.ComputeLayout(root, new Size<AvailableSpace>(
            AvailableSpace.Definite(inRect.width),
            AvailableSpace.Definite(inRect.height)));

        // ── Draw — read Location/Size from each node's Layout ──────────────────
        ref var rootLayout = ref tree.Layout(root);
        var ox = inRect.x + rootLayout.Location.X;
        var oy = inRect.y + rootLayout.Location.Y;

        var blue = new Color(0.25f, 0.45f, 0.8f, 0.7f);
        var red = new Color(0.75f, 0.25f, 0.2f, 0.7f);
        var green = new Color(0.2f, 0.65f, 0.3f, 0.7f);

        Draw(header, "header (row 1, span 3)", blue);
        Draw(leftSidebar, "left sidebar (row 2, col 1)", red);
        Draw(content, "content (row 2, col 2)", green);
        Draw(rightSidebar, "right sidebar (row 2, col 3)", red);
        Draw(footer, "footer (row 3, span 3)", blue);

        return;

        void Draw(NodeId id, string label, Color color)
        {
            ref var l = ref tree.Layout(id);
            var r = new Rect(ox + l.Location.X, oy + l.Location.Y, l.Size.Width, l.Size.Height);
            Verse.Widgets.DrawRectFast(r, color);
            Verse.Widgets.Label(r, label);
        }
    }
}