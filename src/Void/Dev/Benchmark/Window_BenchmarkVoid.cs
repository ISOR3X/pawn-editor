using Taffy;
using UnityEngine;
using Verse;
using Void.Taffy;
using static Void.Dev.Benchmark.BenchmarkContent;

namespace Void.Dev.Benchmark;

/// <summary>
///     The benchmark content on the Void API over the Taffy bindings (<see cref="Void.Taffy.UITree" />, diffing in C#
///     over ctaffy). On this branch the tree has the C#-only optimisations of the void_native one: it builds once per
///     frame, skips the Layout event, keeps layouts between frames and culls what is scrolled out of sight.
///     <see cref="Window_BenchmarkNative" /> is the same code on <c>void_native</c>.
/// </summary>
public class Window_BenchmarkVoid(bool dynamic = false) : Window_Benchmark("Void (ctaffy, optimised)", 1, dynamic)
{
    private static readonly Style RootStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        gap = new TaffyAxes(Dimension.Px(Gap)),
        width = Dimension.Percent(1f)
    };

    private static readonly Style HeadingStyle = new() { fontSize = GameFont.Medium };

    private static readonly Style ButtonRowStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        flexWrap = TaffyFlexWrap.Wrap,
        gap = new TaffyAxes(Dimension.Px(Gap))
    };

    private static readonly Style GridStyle = new()
    {
        display = TaffyDisplay.Grid,
        gridTemplateColumns = Enumerable.Repeat(TrackSizingFunction.Fr(1f), GridColumns).ToArray(),
        gap = new TaffyAxes(Dimension.Px(GridGap)),
        width = Dimension.Percent(1f)
    };

    private static readonly Style CellStyle = new()
    {
        display = TaffyDisplay.Flex,
        alignItems = TaffyAlignItems.Center,
        height = Dimension.Px(CellHeight),
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(CellPadding))
    };

    private static readonly Style NoWrapStyle = new() { wordWrap = false, minWidth = Dimension.Px(0f) };

    private static readonly Style ListStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        overflowY = TaffyOverflow.Scroll,
        height = Dimension.Px(ListHeight),
        width = Dimension.Percent(1f)
    };

    private static readonly Style RowStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        alignItems = TaffyAlignItems.Center,
        gap = new TaffyAxes(Dimension.Px(RowGap)),
        height = Dimension.Px(RowHeight),
        width = Dimension.Percent(1f),
        flexShrink = 0f,
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(RowPadding))
    };

    private static readonly Style LabelStyle = new() { wordWrap = false, flexGrow = 1f, minWidth = Dimension.Px(0f) };

    private static readonly Style ValueStyle = new()
    {
        wordWrap = false,
        textAnchor = TextAnchor.UpperRight,
        width = Dimension.Px(ValueWidth),
        flexShrink = 0f
    };

    private readonly UITree _tree = new();
    private TaffyNode? _list;
    private int _clicks;

    public override void PostClose()
    {
        base.PostClose();
        _tree.Dispose();
    }

    protected override void DrawContents(Rect inRect)
    {
        // Before the build, which picks the rows to build from the scroll position.
        if (Dynamic && _list is { } list && _tree.GetState<ScrollState>(list, "scroll") is { } scroll)
            scroll.pos.y = ListScroll(Time.frameCount);

        _tree.Build(root =>
        {
            root.Text(Heading, HeadingStyle);

            for (var p = 0; p < Paragraphs.Length; p++) root.Text(Paragraphs[p], id: $"paragraph-{p}");

            for (var r = 0; r < ButtonRows.Length; r++)
            {
                var labels = ButtonRows[r];
                root.Div(row =>
                {
                    for (var b = 0; b < labels.Length; b++)
                        row.Button(labels[b], onClick: _ => _clicks++, id: $"button-{b}");
                }, style: ButtonRowStyle, id: $"buttons-{r}");
            }

            if (Dynamic) DynamicSection(root);

            root.Div(grid =>
            {
                for (var i = 0; i < Cells.Length; i++)
                {
                    var index = i;
                    grid.Div(cell => cell.Text(Cells[index], NoWrapStyle),
                        draw: rect => Verse.Widgets.DrawBoxSolid(rect, CellColor(index)),
                        style: CellStyle, id: $"cell-{i}");
                }
            }, style: GridStyle);

            // Only builds the rows in view, like the vanilla window only draws them.
            _list = root.List(IndexedRows, (row, item) =>
                {
                    row.Div(content =>
                    {
                        content.Icon(TexUI.ArrowRight);
                        content.Text(item.label, LabelStyle);
                        content.Text(item.value, ValueStyle);
                    }, draw: rect => DrawRowBackground(rect, item.index), style: RowStyle);
                }, itemHeight: RowHeight, maxItemsVisibleAtOnce: Mathf.CeilToInt(ListHeight / RowHeight),
                style: ListStyle);
        }, RootStyle);

        _tree.Draw(inRect);
    }

    /// <summary>
    ///     Buttons that come and go, and a text that changes, for the nodes this adds, removes and measures.
    /// </summary>
    private void DynamicSection(UIBranch root)
    {
        var frame = Time.frameCount;
        root.Text(Ticker(frame), id: "ticker");
        root.Div(row =>
        {
            var count = DynamicCount(frame);
            for (var b = 0; b < count; b++)
                row.Button(DynamicLabels[b], onClick: _ => _clicks++, id: $"dynamic-{b}");
        }, style: ButtonRowStyle, id: "dynamic");
    }
}
