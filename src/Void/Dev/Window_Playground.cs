#if DEBUG
using LudeonTK;
using Taffy;
using UnityEngine;
using Verse;
using Void.Taffy;
using static VoidComponents;

namespace Void.Dev;

/// <summary>
/// Dev-only sandbox for exercising components on the new <see cref="UITree" /> without needing
/// PawnEditor to compile. Opened from the dev-mode debug actions menu under the "Void" category,
/// available on the main menu as well as in play. Edit the tab bodies and EditCompileReload picks
/// the change up live.
/// </summary>
public class Window_Playground : Window
{
    private enum Tab
    {
        Buttons,
        Text,
        Icons,
        Collapsible,
        List,
        Layout
    }

    private static readonly Tab[] Tabs = (Tab[])Enum.GetValues(typeof(Tab));

    private static readonly (string name, Texture2D tex)[] Icons =
    [
        ("ArrowUp", TexUI.ArrowUp),
        ("ArrowDown", TexUI.ArrowDown),
        ("ArrowLeft", TexUI.ArrowLeft),
        ("ArrowRight", TexUI.ArrowRight),
        ("ArrowLeftDouble", TexUI.ArrowLeftDouble),
        ("ArrowRightDouble", TexUI.ArrowRightDouble)
    ];

    private const string Lorem =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut " +
        "labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco.";

    private readonly UITree _tree = new();
    private Tab _tab = Tab.Buttons;
    private int _clicks;
    private bool _showExtra;

    // List tab state. Items live on the window so add/remove buttons can mutate them between frames.
    private readonly List<string> _listItems = Enumerable.Range(1, 200).Select(i => $"Item {i}").ToList();
    private readonly HashSet<string> _listSelected = [];
    private bool _listGap;

    public override Vector2 InitialSize => new(1280f, 860f);

    public Window_Playground()
    {
        resizeable = true;
        draggable = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
    }

    [DebugAction("Void", "Open playground", allowedGameStates = AllowedGameStates.Invalid)]
    private static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_Playground>()) Find.WindowStack.TryRemove(typeof(Window_Playground));
        else Find.WindowStack.Add(new Window_Playground());
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.Build(root =>
        {
            TabBar(root);

            switch (_tab)
            {
                case Tab.Buttons: ButtonPlayground(root); break;
                case Tab.Text: TextPlayground(root); break;
                case Tab.Icons: IconPlayground(root); break;
                case Tab.Collapsible: CollapsiblePlayground(root); break;
                case Tab.List: ListPlayground(root); break;
                case Tab.Layout: LayoutPlayground(root); break;
            }
        }, new Style
        {
            display = TaffyDisplay.Flex,
            flexDirection = TaffyFlexDirection.Column,
            gap = Axes(10f),
            width = Dimension.Percent(1f),
            height = Dimension.Auto()
        });

        _tree.Draw(inRect);
    }

    public override void PostClose()
    {
        base.PostClose();
        _tree.Dispose();
    }

    /// <summary>
    /// Components are keyed by caller line, so everything emitted inside a loop needs an explicit id
    /// or every iteration collapses onto the same node.
    /// </summary>
    private void TabBar(UIBranch root)
    {
        root.Div(b =>
        {
            foreach (var tab in Tabs)
                b.Button(tab.ToString(),
                    variant: tab == _tab ? ButtonVariant.Solid : ButtonVariant.Ghost,
                    onClick: _ => _tab = tab,
                    id: $"tab-{tab}");

            b.Div(style: new Style { flexGrow = 1f });
            b.Button("Overlay", size: ComponentSize.Small, onClick: _ => VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug);
        }, style: Row());
    }

    private void ButtonPlayground(UIBranch builder)
    {
        builder.Text($"Button playground (clicks: {_clicks})", new Style { fontSize = GameFont.Medium });

        // One row per size, so measured widths can be compared side by side.
        builder.Div(b =>
        {
            b.Button("Small", size: ComponentSize.Small, onClick: _ => _clicks++);
            b.Button("Small + icon", TexUI.ArrowRight, size: ComponentSize.Small, onClick: _ => _clicks++);
            b.Button(icon: TexUI.ArrowRight, size: ComponentSize.Small, onClick: _ => _clicks++);
        }, style: Row());

        builder.Div(b =>
        {
            b.Button("Default", onClick: _ => _clicks++);
            b.Button("Default + icon", TexUI.ArrowRight, onClick: _ => _clicks++);
            b.Button(icon: TexUI.ArrowRight, onClick: _ => _clicks++);
            b.Button("Ghost", variant: ButtonVariant.Ghost, onClick: _ => _clicks++);
            b.Button("Disabled", disabled: true);
        }, style: Row());

        builder.Div(b =>
        {
            b.Button("Large", size: ComponentSize.Large, onClick: _ => _clicks++);
            b.Button("Large + icon", TexUI.ArrowRight, size: ComponentSize.Large, onClick: _ => _clicks++);
        }, style: Row());

        // Block button stretches to the parent width; resize the window to see it follow.
        builder.Button(_showExtra ? "Hide extra" : "Show extra", block: true, onClick: _ => _showExtra = !_showExtra);

        // Toggled subtree exercises keyed add/remove of children.
        if (_showExtra)
            builder.Div(b =>
            {
                b.Text("This block is added and removed by the button above. A very long label follows to check truncation:");
                b.Button("This label is far too long for the space it has been given and should truncate",
                    TexUI.ArrowLeft, style: new Style { maxWidth = Dimension.Px(220f) });
            }, style: Column(6f, 8f));
    }

    private void TextPlayground(UIBranch builder)
    {
        builder.Text("Text playground", new Style { fontSize = GameFont.Medium });

        // Fonts: each leaf measures with its own font, so the three lines should have different heights.
        builder.Div(b =>
        {
            b.Text("Tiny font", new Style { fontSize = GameFont.Tiny });
            b.Text("Small font", new Style { fontSize = GameFont.Small });
            b.Text("Medium font", new Style { fontSize = GameFont.Medium });
            b.Text("Colored text", new Style { color = Color.cyan });
        }, style: Row());

        // Anchors only show when the leaf is larger than its text, so each gets a fixed box.
        builder.Div(b =>
        {
            foreach (var anchor in new[] { TextAnchor.UpperLeft, TextAnchor.MiddleCenter, TextAnchor.LowerRight })
                b.Text(anchor.ToString(),
                    new Style { textAnchor = anchor, width = Dimension.Px(150f), height = Dimension.Px(60f) },
                    id: $"anchor-{anchor}");
        }, style: Row());

        // Wrapping paragraph at full width; resize the window and the height should follow.
        builder.Text(Lorem, new Style { width = Dimension.Percent(1f) });

        // Same paragraph beside a fixed box in a row. With shrinking allowed it should wrap down
        // to its widest word before the box gives way.
        builder.Div(b =>
        {
            b.Div(draw: r => Verse.Widgets.DrawRectFast(r, Color.gray with { a = 0.4f }),
                style: new Style { width = Dimension.Px(200f), height = Dimension.Px(40f), flexShrink = 0f });
            b.Text(Lorem, new Style { flexShrink = 1f });
        }, style: Row());

        // No wrap: measured as a single line, then truncated to whatever width it ends up with.
        builder.Div(b =>
        {
            b.Text("No wrap, this single line is longer than its 200px box and must truncate rather than grow",
                new Style { wordWrap = false, flexShrink = 1f, minWidth = Dimension.Px(0) });
        }, style: new Style { display = TaffyDisplay.Flex, width = Dimension.Px(200f) });

        // Dynamic text: the context changes every frame the value changes, which must re-measure.
        builder.Text($"Ticks: {Time.frameCount}   Clicks: {_clicks}", new Style { wordWrap = false });
    }

    private void IconPlayground(UIBranch builder)
    {
        builder.Text("Icon playground", new Style { fontSize = GameFont.Medium });

        foreach (var size in new[] { ComponentSize.Small, ComponentSize.Default, ComponentSize.Large })
            builder.Div(b =>
            {
                b.Text(size.ToString(), new Style { width = Dimension.Px(60f) }, id: $"label-{size}");
                foreach (var (name, tex) in Icons)
                    b.Icon(tex, size: size, id: $"icon-{size}-{name}");
            }, style: Row(), id: $"row-{size}");

        // Tinted icons.
        builder.Div(b =>
        {
            b.Text("Tinted", new Style { width = Dimension.Px(60f) });
            b.Icon(TexUI.ArrowUp, Color.red);
            b.Icon(TexUI.ArrowDown, Color.green);
            b.Icon(TexUI.ArrowLeft, Color.cyan);
            b.Icon(TexUI.ArrowRight, Color.yellow);
        }, style: Row());

        // Shrink test: the icons sit beside a non-wrapping label that is allowed to shrink. When the
        // window narrows the label should truncate while every icon keeps its fixed size.
        builder.Div(b =>
        {
            foreach (var (name, tex) in Icons)
                b.Icon(tex, id: $"shrink-{name}");
            b.Text("Narrow the window: this label should give way before any icon shrinks",
                new Style { wordWrap = false, flexShrink = 1f, minWidth = Dimension.Px(0) });
        }, style: Row());

        // Icon inside a fixed-size box, centered both ways via the parent.
        builder.Div(b => b.Icon(TexUI.ArrowRightDouble, size: ComponentSize.Large),
            draw: r => Verse.Widgets.DrawBox(r),
            style: new Style
            {
                display = TaffyDisplay.Flex,
                alignItems = TaffyAlignItems.Center,
                justifyContent = TaffyAlignContent.Center,
                width = Dimension.Px(80f),
                height = Dimension.Px(80f)
            });
    }

    private void CollapsiblePlayground(UIBranch builder)
    {
        builder.Collapsible("I am collapsed", b => { b.Button(Lorem); });
        builder.Collapsible("I am collapsed2", b => { b.Text(Lorem); });
        builder.Collapsible("I am collapsed3", b => { b.Button(Lorem); });
        builder.Collapsible("I am collapsed4", b => { b.Text(Lorem); });
    }

    private void ListPlayground(UIBranch builder)
    {
        builder.Text($"List playground ({_listItems.Count} items, {_listSelected.Count} selected)",
            new Style { fontSize = GameFont.Medium });

        // Mutating the item count changes the list's own height whenever it is below the visible cap,
        // and the scroll range once it is above it.
        builder.Div(b =>
        {
            b.Button("Add 10", size: ComponentSize.Small, onClick: _ =>
            {
                for (var i = 0; i < 10; i++) _listItems.Add($"Item {_listItems.Count + 1}");
            });
            b.Button("Remove 10", size: ComponentSize.Small, onClick: _ =>
            {
                var n = Math.Min(10, _listItems.Count);
                _listItems.RemoveRange(_listItems.Count - n, n);
            });
            b.Button("Clear", size: ComponentSize.Small, onClick: _ => { _listItems.Clear(); _listSelected.Clear(); });
            b.Button("Reset", size: ComponentSize.Small, onClick: _ =>
            {
                _listItems.Clear();
                _listItems.AddRange(Enumerable.Range(1, 200).Select(i => $"Item {i}"));
                _listSelected.Clear();
            });
            b.Button(_listGap ? "Gap: on" : "Gap: off", size: ComponentSize.Small,
                variant: _listGap ? ButtonVariant.Solid : ButtonVariant.Ghost, onClick: _ => _listGap = !_listGap);
        }, style: Row());

        // Main list: virtualized, capped at eight visible rows, checkbox rows that toggle selection.
        // Scroll far down and back to confirm only visible rows are drawn and the scroll position sticks.
        builder.List(_listItems, (r, item) =>
        {
            var selected = _listSelected.Contains(item);
            var was = selected;
            Verse.Widgets.CheckboxLabeled(r, item, ref selected);
            if (selected != was)
            {
                if (selected) _listSelected.Add(item);
                else _listSelected.Remove(item);
            }
        }, maxItemsVisibleAtOnce: 8, style: _listGap ? new Style { gap = Axes(4f) } : null);

        // Two independent lists side by side. Same call site in a loop, so each needs an id, and each
        // must keep its own scroll position through the per-parent state slot.
        builder.Text("Independent scroll state:", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            foreach (var side in new[] { "left", "right" })
                b.Div(inner => inner.List(_listItems, (r, item) =>
                    {
                        Verse.Widgets.DrawHighlightIfMouseover(r);
                        Verse.Widgets.Label(r, $"{side}: {item}");
                    }, itemHeight: 22f, maxItemsVisibleAtOnce: 5, id: $"list-{side}"),
                    style: new Style { flexGrow = 1f, flexBasis = Dimension.Px(0) },
                    id: $"col-{side}");
        }, style: Row());

        // Short and empty lists: height follows the item count below the cap, and the empty
        // list falls back to a single row with the placeholder label.
        builder.Text("Short (3 items) and empty:", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Div(inner => inner.List(_listItems.Take(3).ToList(), (r, item) => Verse.Widgets.Label(r, item)),
                style: new Style { flexGrow = 1f, flexBasis = Dimension.Px(0) });
            b.Div(inner => inner.List(Array.Empty<string>(), (r, item) => Verse.Widgets.Label(r, item)),
                style: new Style { flexGrow = 1f, flexBasis = Dimension.Px(0) });
        }, style: Row());
    }

    /// <summary>
    /// Renders the <c>Void_Playground</c> <see cref="LayoutDef" /> parsed from XML, so the XML element
    /// registry can be exercised next to the hand-written tabs.
    /// </summary>
    private static void LayoutPlayground(UIBranch builder)
    {
        builder.Text("Layout playground (Void_Playground def)", new Style { fontSize = GameFont.Medium });

        var def = DefDatabase<LayoutDef>.GetNamedSilentFail("Void_Playground");
        if (def?.layout?._builder == null)
        {
            builder.Text("LayoutDef 'Void_Playground' not found or has no layout.", new Style { color = Color.red });
            return;
        }

        builder.Div(def.layout._builder, style: def.layout._style);
    }

    private static Style Row() => new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Row,
        alignItems = TaffyAlignItems.Center,
        gap = Axes(8f),
        width = Dimension.Percent(1f)
    };

    private static Style Column(float gap, float padding) => new()
    {
        display = TaffyDisplay.Flex,
        flexDirection = TaffyFlexDirection.Column,
        gap = Axes(gap),
        padding = Edges(padding),
        width = Dimension.Percent(1f)
    };

    private static TaffyAxes Axes(float v) => new(Dimension.Px(v));

    private static TaffyEdges Edges(float v) => new(Dimension.Px(v));
}
#endif
