#if DEBUG
using LudeonTK;
using UnityEngine;
using Verse;

namespace Void.Dev;

using Void.Native;
using static Void.Native.Components;

/// <summary>
///     Dev-only sandbox for the <c>void_native</c> backed <see cref="Native.UITree" />, the counterpart of
///     <see cref="Window_Playground" /> (old tree) and <see cref="Window_PlaygroundVanilla" /> (plain Widgets).
///     Kept to the basics: text measurement, adding and removing nodes, scrolling and grid layout.
/// </summary>
public class Window_PlaygroundVoid : Window
{
    private const string Lorem =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut " +
        "labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco.";

    private static readonly Tab[] Tabs = (Tab[])Enum.GetValues(typeof(Tab));

    private static readonly Style RootStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        gap = Axes(10f),
        width = Dimension.Percent(1f),
        height = Dimension.Auto()
    };

    private static readonly Style RowStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        alignItems = TaffyAlignItems.Center,
        gap = Axes(8f),
        width = Dimension.Percent(1f)
    };

    private static readonly Style ColumnStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        gap = Axes(4f),
        width = Dimension.Percent(1f)
    };

    private static readonly Style ReadoutStyle = new() { width = Dimension.Percent(1f), height = Dimension.Px(18f), flexShrink = 0f };
    private static readonly Style HeadingStyle = new() { fontSize = GameFont.Medium };
    private static readonly Style SpacerStyle = new() { flexGrow = 1f };

    private static readonly Style BoxStyle = new()
    {
        width = Dimension.Px(120f),
        height = Dimension.Px(40f),
        flexShrink = 0f
    };

    private static readonly Style ScrollStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        overflowY = TaffyOverflow.Scroll,
        height = Dimension.Px(300f),
        width = Dimension.Percent(1f)
    };

    private static readonly Style ScrollRowStyle = new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        alignItems = TaffyAlignItems.Center,
        gap = Axes(6f),
        height = Dimension.Px(28f),
        flexShrink = 0f,
        padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(6f))
    };

    private static readonly Style GridStyle = new()
    {
        display = TaffyDisplay.Grid,
        gridTemplateColumns =
        [
            TrackSizingFunction.Px(80f),
            TrackSizingFunction.AutoTrack(),
            TrackSizingFunction.Fr(1f),
            TrackSizingFunction.Fr(2f)
        ],
        gap = Axes(4f),
        width = Dimension.Percent(1f)
    };

    private static readonly Style GridSpanStyle = new() { gridColumn = new TaffyGridPlacement(span: 2) };

    private readonly UITree _tree = new();

    private int _boxes = 3;
    private int _clicks;
    private int _rows = 200;
    private bool _showLorem = true;
    private Tab _tab = Tab.Basics;
    private bool _wide;

    public Window_PlaygroundVoid()
    {
        resizeable = true;
        draggable = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
    }

    public override Vector2 InitialSize => new(UI.screenWidth / 2f, UI.screenHeight);

    [DebugAction("Void", "Open void_native playground", allowedGameStates = AllowedGameStates.Invalid)]
    private static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_PlaygroundVoid>()) Find.WindowStack.TryRemove(typeof(Window_PlaygroundVoid));
        else Find.WindowStack.Add(new Window_PlaygroundVoid());
    }

    public override void PostClose()
    {
        base.PostClose();
        _tree.Dispose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.Build(root =>
        {
            Stats(root);
            TabBar(root);

            switch (_tab)
            {
                case Tab.Basics: Basics(root); break;
                case Tab.Scroll: Scroll(root); break;
                case Tab.Grid: Grid(root); break;
                case Tab.Table: Table(root); break;
                case Tab.Layout: LayoutPlayground(root); break;
            }
        }, RootStyle);

        _tree.Draw(inRect);
    }

    /// <summary>
    ///     Shows what the native side did, so it is visible that unchanged frames skip layout. The values are from
    ///     the previous frame, since this frame has not been sent yet while it is being built.
    /// </summary>
    private void Stats(UIBranch root)
    {
        Readout(root, () =>
            $"nodes {_tree.NodeCount} · layout last frame: {(_tree.LastFrameLaidOut ? "yes" : "no")} · " +
            $"styles {StyleRegistry.Count} · texts {MeasureRegistry.Count} · " +
            $"measure callbacks {NativeMethods.void_measure_callback_count()}", "stats");
    }

    /// <summary>
    ///     A line of live diagnostics. It has a fixed size and paints its text at draw time instead of being a
    ///     measured <see cref="Components.Text" />: otherwise every new value would be new content to register and
    ///     lay out, which changes the very numbers being shown, so the window would never go idle.
    /// </summary>
    private static void Readout(UIBranch b, Func<string> text, string id)
    {
        b.Div(draw: r =>
        {
            using (new GUIColor(Color.gray))
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft, false))
            {
                Verse.Widgets.Label(r, text());
            }
        }, style: ReadoutStyle, id: id);
    }

    private void TabBar(UIBranch root)
    {
        root.Div(b =>
        {
            foreach (var tab in Tabs)
                b.Button(tab.ToString(),
                    variant: tab == _tab ? ButtonVariant.Solid : ButtonVariant.Ghost,
                    onClick: _ => _tab = tab,
                    id: $"tab-{tab}");

            b.Div(style: SpacerStyle);
            b.Button("Overlay", size: ComponentSize.Small,
                onClick: _ => VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug);
        }, style: RowStyle);
    }

    private void Basics(UIBranch root)
    {
        root.Text($"Basics (clicks: {_clicks})", HeadingStyle);

        root.Div(b =>
        {
            b.Button("Click", onClick: _ => _clicks++);
            b.Button("Small", size: ComponentSize.Small, onClick: _ => _clicks++);
            b.Button("Large", size: ComponentSize.Large, onClick: _ => _clicks++);
            b.Button(icon: TexUI.ArrowRight, onClick: _ => _clicks++);
            b.Button("Disabled", disabled: true);
        }, style: RowStyle);

        // Adding and removing nodes: removed boxes are reported back by the native side.
        root.Div(b =>
        {
            b.Button("Add box", onClick: _ => _boxes++);
            b.Button("Remove box", onClick: _ => _boxes = Math.Max(0, _boxes - 1));
            b.Text($"{_boxes} boxes");
        }, style: RowStyle);

        root.Div(b =>
        {
            for (var i = 0; i < _boxes; i++)
            {
                var index = i;
                b.Div(draw: r =>
                {
                    Verse.Widgets.DrawBoxSolid(r, Color.HSVToRGB(index * 0.13f % 1f, 0.5f, 0.6f));
                    using (new TextBlock(TextAnchor.MiddleCenter)) Verse.Widgets.Label(r, $"box {index}");
                }, style: BoxStyle, id: $"box-{i}");
            }
        }, style: RowStyle);

        // Wrapping text: the height depends on the width, which is what the native height cache is for.
        root.Div(b =>
        {
            b.Button(_showLorem ? "Hide text" : "Show text", onClick: _ => _showLorem = !_showLorem);
            b.Button(_wide ? "Narrow" : "Wide", onClick: _ => _wide = !_wide);
        }, style: RowStyle);

        if (_showLorem)
            root.Div(b =>
            {
                b.Text(Lorem);
                b.Text(Lorem, new Style { fontSize = GameFont.Tiny });
                b.Text("No wrap: " + Lorem, new Style { wordWrap = false });
            }, style: _wide ? ColumnStyle : new Style { width = Dimension.Px(300f), display = TaffyDisplay.Flex, flexDirection = TaffyFlexDirection.Column, gap = Axes(4f) });
    }

    private void Scroll(UIBranch root)
    {
        root.Text($"Scroll ({_rows} rows)", HeadingStyle);

        root.Div(b =>
        {
            b.Button("+100 rows", onClick: _ => _rows += 100);
            b.Button("-100 rows", onClick: _ => _rows = Math.Max(0, _rows - 100));
        }, style: RowStyle);

        root.Div(viewport =>
        {
            Readout(viewport, () => $"scroll offset {viewport.GetState<ScrollState>("scroll")?.pos.y ?? 0f:0}",
                "offset");

            for (var i = 0; i < _rows; i++)
            {
                var index = i;
                viewport.Div(row =>
                    {
                        row.Icon(TexUI.ArrowRight, size: ComponentSize.Small);
                        row.Text($"Row {index}");
                    },
                    draw: r =>
                    {
                        if (index % 2 == 0) Verse.Widgets.DrawLightHighlight(r);
                        Verse.Widgets.DrawHighlightIfMouseover(r);
                    },
                    style: ScrollRowStyle, id: $"row-{i}");
            }
        }, style: ScrollStyle);
    }

    private void Grid(UIBranch root)
    {
        root.Text("Grid: 80px | auto | 1fr | 2fr", HeadingStyle);

        root.Div(b =>
        {
            for (var i = 0; i < 12; i++)
            {
                var index = i;
                b.Div(c => c.Text($"cell {index}", new Style { textAnchor = TextAnchor.MiddleCenter }),
                    draw: r => Verse.Widgets.DrawBoxSolidWithOutline(r, new Color(0.2f, 0.2f, 0.25f), Color.gray),
                    id: $"cell-{i}");
            }

            b.Div(c => c.Text("spans two columns", new Style { textAnchor = TextAnchor.MiddleCenter }),
                draw: r => Verse.Widgets.DrawBoxSolidWithOutline(r, new Color(0.25f, 0.2f, 0.2f), Color.gray),
                style: GridSpanStyle);
        }, style: GridStyle);
    }

    private static readonly string[] TableKinds = ["Tool", "Food", "Weapon", "Apparel"];

    private sealed record TableThing(string Name, int Mass, string Kind);

    private readonly List<TableThing> _tableThings = Enumerable.Range(1, 60)
        .Select(i => new TableThing($"Thing {i}", i * 37 % 97, TableKinds[i % TableKinds.Length]))
        .ToList();

    // Columns are built once: their styles are made on first use and reused every frame.
    private IReadOnlyList<TableColumn<TableThing>>? _tableColumns;
    private string? _tableKind;
    private string? _tableSelected;
    private TableSorting? _tableSorting;
    private List<TableThing> _tableView = [];
    private bool _tableViewDirty = true;

    private void Table(UIBranch root)
    {
        root.Text($"Table ({_tableView.Count} / {_tableThings.Count} rows, selected: {_tableSelected ?? "none"})",
            HeadingStyle);
        root.Text("Click a header to sort (right click for descending) and a row to select. Only the rows in view " +
                  "are built; the header stays put and leaves room for the scrollbar.",
            new Style { fontSize = GameFont.Tiny, width = Dimension.Percent(1f) });

        _tableColumns ??=
        [
            new TableColumn<TableThing>("Name",
                (c, t) => c.Text(t.Name, new Style { wordWrap = false, minWidth = Dimension.Px(0f) }))
            {
                Sort = TableColumn<TableThing>.ByText(t => t.Name)
            },
            new TableColumn<TableThing>("Mass", (c, t) => c.Text(t.Mass.ToString()))
            {
                Width = TableColumnWidth.Fixed(80f),
                Sort = TableColumn<TableThing>.By(t => t.Mass)
            },
            new TableColumn<TableThing>("Kind", (c, t) => c.Text(t.Kind))
            {
                Width = TableColumnWidth.Fixed(90f),
                Sort = TableColumn<TableThing>.ByText(t => t.Kind)
            },
            // No sort, so its header is a plain label.
            new TableColumn<TableThing>("", (c, t) => c.Button("Select", size: ComponentSize.Small,
                onClick: _ => _tableSelected = t.Name)) { Width = TableColumnWidth.Fixed(70f) }
        ];

        // The view is built out here, filtered and then sorted, and only rebuilt when the filter or the sorting
        // changes. The table just draws it and reports header clicks.
        if (_tableViewDirty)
        {
            _tableViewDirty = false;
            var filtered = _tableKind == null ? _tableThings : _tableThings.Where(t => t.Kind == _tableKind);
            _tableView = _tableSorting?.Apply(filtered, _tableColumns) ?? [.. filtered];
        }

        root.Div(b =>
        {
            foreach (var kind in TableKinds)
                b.Button(kind, size: ComponentSize.Small,
                    variant: kind == _tableKind ? ButtonVariant.Solid : ButtonVariant.Ghost,
                    onClick: _ =>
                    {
                        _tableKind = _tableKind == kind ? null : kind;
                        _tableViewDirty = true;
                    }, id: $"kind-{kind}");
        }, style: RowStyle);

        root.Table(_tableView, _tableColumns, maxRowsVisibleAtOnce: 8, rowKey: t => t.Name,
            highlightRow: t => t.Name == _tableSelected, onRowClick: t => _tableSelected = t.Name,
            sorting: _tableSorting, onSortingChange: s =>
            {
                _tableSorting = s;
                _tableViewDirty = true;
            }, id: "table-things");

        root.Text("Short (3 rows, no scrollbar) and empty", new Style { fontSize = GameFont.Tiny });
        root.Table(_tableView.Take(3).ToList(), _tableColumns, rowKey: t => t.Name, id: "table-short", sorting: _tableSorting, onSortingChange: s =>
        {
            _tableSorting = s;
            _tableViewDirty = true;
        });
        root.Table(Array.Empty<TableThing>(), _tableColumns, id: "table-empty");
    }

    /// <summary>
    ///     Renders the <c>Void_Playground</c> <see cref="LayoutDef" /> parsed from XML, so the XML element
    ///     registry can be exercised next to the hand-written tabs.
    /// </summary>
    private static void LayoutPlayground(UIBranch root)
    {
        root.Text("Layout playground (Void_Playground def)", HeadingStyle);

        var def = DefDatabase<LayoutDef>.GetNamedSilentFail("Void_Playground");
        if (def?.layout?._builder == null)
        {
            root.Text("LayoutDef 'Void_Playground' not found or has no layout.", new Style { color = Color.red });
            return;
        }

        root.Div(def.layout._builder, style: def.layout._style);
    }

    private static TaffyAxes Axes(float v)
    {
        return new TaffyAxes(Dimension.Px(v));
    }

    private enum Tab
    {
        Basics,
        Scroll,
        Grid,
        Table,
        Layout
    }
}
#endif
