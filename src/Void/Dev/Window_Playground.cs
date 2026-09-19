#if DEBUG
using System.Text.RegularExpressions;
using LudeonTK;
using Taffy;
using UnityEngine;
using Verse;
using Void.Taffy;
using static VoidComponents;

namespace Void.Dev;

/// <summary>
///     Dev-only sandbox for exercising components on the new <see cref="UITree" /> without needing
///     PawnEditor to compile. Opened from the dev-mode debug actions menu under the "Void" category,
///     available on the main menu as well as in play. Edit the tab bodies and EditCompileReload picks
///     the change up live.
/// </summary>
public class Window_Playground : Window
{
    private const string Lorem =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut " +
        "labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco.";

    private static readonly Tab[] Tabs = (Tab[])Enum.GetValues(typeof(Tab));

    // Whole-value regex: the edit is rejected unless the resulting string matches.
    private static readonly Regex LettersOnly = new("^[a-zA-Z]*$");

    private static readonly (string name, Texture2D tex)[] Icons =
    [
        ("ArrowUp", TexUI.ArrowUp),
        ("ArrowDown", TexUI.ArrowDown),
        ("ArrowLeft", TexUI.ArrowLeft),
        ("ArrowRight", TexUI.ArrowRight),
        ("ArrowLeftDouble", TexUI.ArrowLeftDouble),
        ("ArrowRightDouble", TexUI.ArrowRightDouble)
    ];

    private static readonly StyleCache<(int variant, float gap, float padding)> Styles = new(k =>
    {
        if (k.variant == 0)
            return new Style
            {
                display = TaffyDisplay.Flex,
                flexDirection = TaffyFlexDirection.Row,
                alignItems = TaffyAlignItems.Center,
                gap = Axes(8f),
                width = Dimension.Percent(1f)
            };

        return new Style
        {
            display = TaffyDisplay.Flex,
            flexDirection = TaffyFlexDirection.Column,
            gap = Axes(k.gap),
            padding = Edges(k.padding),
            width = Dimension.Percent(1f)
        };
    });

    // Input tab state. Each field is the single source of truth for its widget; the component
    // renders whatever it is handed and reports edits back through onChange.
    private readonly string[] _inputRow = ["one", "two", "three"];

    // List tab state. Items live on the window so add/remove buttons can mutate them between frames.
    private readonly List<string> _listItems = Enumerable.Range(1, 200).Select(i => $"Item {i}").ToList();
    private readonly HashSet<string> _listSelected = [];

    // Context tab state. Subjects carry a stable Id separate from the editable Name: keys must not
    // change while typing, or every keystroke frees and recreates the node and focus is lost.
    private readonly List<Subject> _subjects =
    [
        new("a", "Alpha"), new("b", "Bravo"), new("c", "Charlie")
    ];

    private readonly UITree _tree = new();
    private int _clicks;
    private string _inputCapped = "max 8";
    private string _inputDigits = "2077";
    private string _inputExternal = "100";
    private string _inputFilter = "";
    private string _inputFull = "Full width";
    private string _inputGhost = "Ghost variant";
    private string _inputGrow = "grows with the row";
    private string _inputLarge = "Large";
    private string _inputPattern = "letters";
    private string _inputSelfKeyed = "retype me";
    private string _inputSmall = "Small";
    private string _inputStd = "Default";
    private bool _listGap;
    private int _numBasic = 42;
    private int _numClamped = 5;
    private int _numLarge = 3;
    private int _numSigned;
    private int _numSmall = 1;
    private int _numStd = 2;
    private bool _showExtra;
    private int _subjectSeq;
    private Tab _tab = Tab.Scroll;
    private bool _warmTheme;

    public Window_Playground()
    {
        resizeable = true;
        draggable = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
    }

    public override Vector2 InitialSize => new(UI.screenWidth / 2f, UI.screenHeight);

    /// <summary>
    ///     Left half of the screen, so this can sit beside <see cref="Window_PlaygroundVanilla" /> for a
    ///     side-by-side comparison. Also re-applied on a resolution change by the base class.
    /// </summary>
    public override void SetInitialSizeAndPosition()
    {
        windowRect = new Rect(0f, 0f, UI.screenWidth / 2f, UI.screenHeight).Rounded();
    }

    [DebugAction("Void", "Open playground", allowedGameStates = AllowedGameStates.Invalid)]
    public static void Open()
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
                case Tab.Scroll: ScrollPlayground(root); break;
                case Tab.Buttons: ButtonPlayground(root); break;
                case Tab.Text: TextPlayground(root); break;
                case Tab.Icons: IconPlayground(root); break;
                case Tab.Input: InputPlayground(root); break;
                case Tab.Collapsible: CollapsiblePlayground(root); break;
                case Tab.List: ListPlayground(root); break;
                case Tab.Context: ContextPlayground(root); break;
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
    ///     Components are keyed by caller line, so everything emitted inside a loop needs an explicit id
    ///     or every iteration collapses onto the same node.
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
            b.Button("Overlay", size: ComponentSize.Small,
                onClick: _ => VoidMod.Settings.drawDebug = !VoidMod.Settings.drawDebug);
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
                b.Text(
                    "This block is added and removed by the button above. A very long label follows to check truncation:");
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
                    $"anchor-{anchor}");
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
                    b.Text(size.ToString(), new Style { width = Dimension.Px(60f) }, $"label-{size}");
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
            r => Verse.Widgets.DrawBox(r),
            style: new Style
            {
                display = TaffyDisplay.Flex,
                alignItems = TaffyAlignItems.Center,
                justifyContent = TaffyAlignContent.Center,
                width = Dimension.Px(80f),
                height = Dimension.Px(80f)
            });
    }

    /// <summary>
    ///     Exercises <see cref="VoidComponents.Input" />. The component is write-through: the caller's
    ///     field is the source of truth every frame and <c>onChange</c> fires from the draw callback,
    ///     so there is no internal buffer that can drift from the value you passed in.
    /// </summary>
    private void InputPlayground(UIBranch builder)
    {
        builder.Text("Input playground", new Style { fontSize = GameFont.Medium });

        // Sizes. Each size picks its own font and height, so the boxes should differ in both.
        builder.Text("Sizes", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Input(_inputSmall, v => _inputSmall = v, size: ComponentSize.Small);
            b.Input(_inputStd, v => _inputStd = v);
            b.Input(_inputLarge, v => _inputLarge = v, size: ComponentSize.Large);
            b.Input("disabled", _ => { }, disabled: true);
        }, style: Row());

        // Sizing. The base style is a fixed 160px with flexShrink 0, and the caller's style wins on
        // merge, so both of these have to override width (and shrink) to escape it.
        builder.Text("Sizing: fixed default, full width, and flex-grown", new Style { fontSize = GameFont.Tiny });
        builder.Input(_inputFull, v => _inputFull = v, style: new Style { width = Dimension.Percent(1f) },
            id: "input-fullwidth");
        builder.Div(b =>
        {
            b.Text("Label", new Style { width = Dimension.Px(60f) });
            b.Input(_inputGrow, v => _inputGrow = v,
                style: new Style { flexGrow = 1f, flexShrink = 1f, width = Dimension.Auto() });
            b.Button("Clear", size: ComponentSize.Small, onClick: _ => _inputGrow = "");
        }, style: Row());

        // Three ways to refuse an edit, in the order the component applies them: maxLength, then a
        // whole-value regex, then whatever the callback decides. All three reject rather than
        // truncate, so pasting an over-long or non-matching string is refused outright.
        builder.Text("Rejecting edits: maxLength, pattern, and caller-side validation",
            new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Input(_inputCapped, v => _inputCapped = v, 8);
            b.Text($"{_inputCapped.Length}/8", new Style { fontSize = GameFont.Tiny, width = Dimension.Px(40f) });

            b.Input(_inputPattern, v => _inputPattern = v, pattern: LettersOnly, id: "input-pattern");
            b.Text("pattern: letters", new Style { fontSize = GameFont.Tiny });

            // Write-through means a filter is also just an if in the callback: reject the edit and
            // the field keeps its previous value, which is what gets rendered next frame.
            b.Input(_inputDigits, v =>
            {
                if (v.All(char.IsDigit)) _inputDigits = v;
            }, 6, id: "input-digits");
            b.Text("callback: digits", new Style { fontSize = GameFont.Tiny });
        }, style: Row());

        // Variants and focus behaviour. Ghost drops the background and border in every state, for
        // fields sitting inside an already-framed row. Click any field, then click anywhere else:
        // focus releases on that same frame, and an unfocused field renders with the locked style
        // so a click that lands inside its rect on the way past produces no flash.
        builder.Text("Variants, and click-outside to release focus", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Input(_inputStd, v => _inputStd = v, id: "variant-solid");
            b.Input(_inputGhost, v => _inputGhost = v, variant: InputVariant.Ghost, id: "variant-ghost");
            b.Text("ghost", new Style { fontSize = GameFont.Tiny });
            b.Button("Click me (focus should release)", size: ComponentSize.Small, onClick: _ => _clicks++);
        }, style: Row());

        // External writes. The legacy input needed Source/Typed bookkeeping so a button press could
        // not be overwritten by a stale buffer; with no buffer to go stale these just take effect.
        builder.Text("External writes land immediately", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Input(_inputExternal, v => _inputExternal = v, id: "input-external");
            b.Button("Set 1234", size: ComponentSize.Small, onClick: _ => _inputExternal = "1234");
            b.Button("+1", size: ComponentSize.Small,
                onClick: _ => _inputExternal = (int.TryParse(_inputExternal, out var n) ? n + 1 : 0).ToString());
            b.Text($"backing value: {_inputExternal}", new Style { fontSize = GameFont.Tiny });
        }, style: Row());

        // Same call site in a loop, so each needs an explicit id. Without one all three would key to
        // the same node and the same GUI control name, and focus would jump between them.
        builder.Text("Independent focus: tab between these, edits must not bleed",
            new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            for (var i = 0; i < _inputRow.Length; i++)
            {
                var index = i;
                b.Input(_inputRow[index], v => _inputRow[index] = v, id: $"row-input-{index}");
            }

            b.Text(string.Join(" | ", _inputRow), new Style { fontSize = GameFont.Tiny });
        }, style: Row());

        // InputNumber. Unlike Input this one does keep a buffer in branch.State, because "" and a
        // lone "-" have to be typeable without committing. Clamping is deferred to blur for the same
        // reason: typing 10 on the way to 100 must not snap to the max.
        builder.Text("InputNumber: spinner buttons hold-repeat, scroll over a field to increment (shift = 5)",
            new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.InputNumber(_numBasic, v => _numBasic = v);
            b.Text($"value: {_numBasic}", new Style { fontSize = GameFont.Tiny, width = Dimension.Px(80f) });

            b.InputNumber(_numClamped, v => _numClamped = v, 1, 10, id: "num-clamped");
            b.Text("1-10, clamped on blur", new Style { fontSize = GameFont.Tiny });

            b.InputNumber(_numSigned, v => _numSigned = v, -50, 50, id: "num-signed");
            b.Text("-50..50, leading minus allowed", new Style { fontSize = GameFont.Tiny });

            b.InputNumber(7, _ => { }, disabled: true, id: "num-disabled");
        }, style: Row());

        // Sizes, and the ghost variant. The two ghost fields below share one backing value, which
        // exercises the external-write path: committing in one is an outside write to the other,
        // so the other drops its buffer and re-reads rather than fighting over it.
        builder.Div(b =>
        {
            b.InputNumber(_numSmall, v => _numSmall = v, size: ComponentSize.Small, id: "num-small");
            b.InputNumber(_numStd, v => _numStd = v, id: "num-std");
            b.InputNumber(_numLarge, v => _numLarge = v, size: ComponentSize.Large, id: "num-large");

            b.InputNumber(_numBasic, v => _numBasic = v, variant: InputVariant.Ghost, id: "num-ghost-a");
            b.InputNumber(_numBasic, v => _numBasic = v, variant: InputVariant.Ghost, id: "num-ghost-b");
            b.Text("two ghosts, one value", new Style { fontSize = GameFont.Tiny });
        }, style: Row());

        // Anti-pattern, kept because it is the sharpest way to lose an afternoon: the id is derived
        // from the value being edited, so every keystroke changes the key, frees the node, and drops
        // keyboard focus. Type here and it accepts exactly one character at a time.
        builder.Text("Anti-pattern: id derived from the edited value (focus dies every keystroke)",
            new Style { fontSize = GameFont.Tiny, color = ColoredText.SubtleGrayColor });
        builder.Input(_inputSelfKeyed, v => _inputSelfKeyed = v, id: $"self-keyed-{_inputSelfKeyed}");

        // Realistic use: an input driving another component. The list's item count changes as you
        // type, which re-measures and relayouts the subtree under it.
        builder.Text("Filter a list", new Style { fontSize = GameFont.Tiny });
        var filtered = string.IsNullOrEmpty(_inputFilter)
            ? _listItems
            : _listItems.Where(i => i.IndexOf(_inputFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        builder.Div(b =>
        {
            b.Input(_inputFilter, v => _inputFilter = v, id: "input-filter");
            b.Text($"{filtered.Count} / {_listItems.Count} items", new Style { fontSize = GameFont.Tiny });
            b.Button("Reset", size: ComponentSize.Small, onClick: _ => _inputFilter = "");
        }, style: Row());
        builder.List(filtered, (r, item) => Verse.Widgets.Label(r, item), maxItemsVisibleAtOnce: 6);
    }

    private void CollapsiblePlayground(UIBranch builder)
    {
        builder.Collapsible("I am collapsed", b => { b.Button(Lorem); });
        builder.Collapsible("I am collapsed2", b => { b.Text(Lorem); });
        builder.Collapsible("I am collapsed3", b => { b.Button(Lorem); });
        builder.Collapsible("I am collapsed4", b => { b.Text(Lorem); });
    }

    // Scroll tab state. Counting the rows actually built each frame is the cheapest proof that
    // virtualization is doing anything: it should track the visible count, not the item count.
    private readonly Dictionary<string, int> _rowValues = [];
    private int _rowsBuilt;

    /// <summary>
    ///     Exercises the <see cref="UIBranch" /> overload of <c>List</c>, where rows are real nodes and
    ///     their contents are built with components instead of drawn from a <see cref="Rect" />.
    /// </summary>
    private void ScrollPlayground(UIBranch builder)
    {
        builder.Text($"List playground - node rows ({_listItems.Count} items)",
            new Style { fontSize = GameFont.Medium });
        _rowsBuilt = 0;

        // 1. A row is a Div, so its contents are components. The row node stretches its children on
        // the cross axis, which is why Text needs an anchor while Button and Icon (fixed heights) do not.
        builder.Text("1. Components inside rows", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Button(_listGap ? "Gap: 4px" : "Gap: none", size: ComponentSize.Small,
                onClick: _ => _listGap = !_listGap);
            b.Text($"{_listSelected.Count} selected", new Style { fontSize = GameFont.Tiny });
        }, style: Row());

        builder.List(_listItems, (row, item) =>
        {
            _rowsBuilt++;
            row.Icon(_listSelected.Contains(item) ? TexUI.ArrowRightDouble : TexUI.ArrowRight,
                size: ComponentSize.Small);
            row.Text(item, new Style
            {
                flexGrow = 1f, textAnchor = TextAnchor.MiddleLeft, wordWrap = false
            });
            row.Button(_listSelected.Contains(item) ? "Deselect" : "Select", size: ComponentSize.Small,
                onClick: _ =>
                {
                    if (!_listSelected.Add(item)) _listSelected.Remove(item);
                });
        }, maxItemsVisibleAtOnce: 8, itemKey: item => item,
            style: _listGap ? new Style { gap = Axes(4f) } : null);

        // 2. Rows are destroyed when they leave the window, so anything stateful inside one has to be
        // fed from outside. InputNumber reconciles its buffer against the value every frame, so it
        // survives either way - but its Unity control name comes from the node key, so only the keyed
        // list keeps focus on the row you are typing in while the list scrolls.
        builder.Text("2. Stateful rows: itemKey decides what focus follows",
            new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            foreach (var keyed in new[] { true, false })
            {
                var isKeyed = keyed;
                b.Div(col =>
                    {
                        col.Text(isKeyed ? "itemKey: item" : "itemKey: null (index)",
                            new Style { fontSize = GameFont.Tiny });
                        col.List(_listItems, (row, item) =>
                            {
                                _rowsBuilt++;
                                row.Text(item, new Style
                                {
                                    flexGrow = 1f, textAnchor = TextAnchor.MiddleLeft, wordWrap = false
                                });
                                row.InputNumber(_rowValues.TryGetValue(item, out var v) ? v : 0,
                                    n => _rowValues[item] = n, size: ComponentSize.Small,
                                    style: new Style { width = Dimension.Px(64f) });
                            }, 24f, 6,
                            isKeyed ? item => item : null,
                            id: $"list-keyed-{isKeyed}");
                    },
                    style: new Style
                    {
                        flexGrow = 1f, flexBasis = Dimension.Px(0),
                        flexDirection = TaffyFlexDirection.Column
                    },
                    id: $"col-keyed-{isKeyed}");
            }
        }, style: Row());

        // 3. Height follows the item count below the cap, and an empty list falls back to one row
        // holding the placeholder.
        builder.Text("3. Short (3 items) and empty", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Div(inner => inner.List(_listItems.Take(3).ToList(),
                    (row, item) => { _rowsBuilt++; row.Text(item, MiddleLeft); }),
                style: new Style { flexGrow = 1f, flexBasis = Dimension.Px(0) });
            b.Div(inner => inner.List(Array.Empty<string>(),
                    (row, item) => row.Text(item, MiddleLeft)),
                style: new Style { flexGrow = 1f, flexBasis = Dimension.Px(0) });
        }, style: Row());

        // Built last so it sees this pass's total. Three lists over 200 items each: if virtualization
        // works this stays in the low tens, and drawDebug will outline only the rows that exist.
        builder.Text($"rows built this pass: {_rowsBuilt} (of {_listItems.Count * 3 + 3} possible)",
            new Style { fontSize = GameFont.Tiny, color = ColoredText.SubtleGrayColor });
    }

    private static readonly Style MiddleLeft = new()
    {
        textAnchor = TextAnchor.MiddleLeft, wordWrap = false, flexGrow = 1f
    };

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
            b.Button("Clear", size: ComponentSize.Small, onClick: _ =>
            {
                _listItems.Clear();
                _listSelected.Clear();
            });
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
                        }, 22f, 5, id: $"list-{side}"),
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
    ///     Exercises <see cref="UIBranch.Provide{T}" /> / <see cref="UIBranch.Inject{T}" />. Each block below
    ///     is a claim that should hold on screen; if one breaks it prints LEAK or throws rather than
    ///     silently degrading.
    /// </summary>
    private void ContextPlayground(UIBranch builder)
    {
        builder.Text("Context playground", new Style { fontSize = GameFont.Medium });

        // 1. Depth. Provide is not a node, so the value has to survive being carried through the
        // UIBranch that every nested Div creates. Reading it three Divs down is the regression test.
        builder.Text("1. Reaches through nested Divs", new Style { fontSize = GameFont.Tiny });
        // Section 3 can empty the list, so this block has to tolerate that.
        if (_subjects.Count > 0)
            builder.Provide("subject", _subjects[0], scope =>
                scope.Div(l1 => l1.Div(l2 => l2.Div(l3 =>
                    l3.Text($"depth 3 sees: {l3.Inject<Subject>("subject").Name}", new Style { color = Color.green })),
                    style: Column(0f, 4f)), style: Column(0f, 4f)));
        else
            builder.Text("(no subjects)", new Style { color = ColoredText.SubtleGrayColor });

        // 2. Shadowing and isolation. The inner Provide must win inside its own lambda only: the
        // sibling after it sees the outer value again, and the sibling outside sees nothing at all.
        builder.Text("2. Shadowing stays inside the lambda", new Style { fontSize = GameFont.Tiny });
        builder.Div(col =>
        {
            col.Provide("theme", new Theme(Color.cyan, "outer"), outer =>
            {
                outer.Text($"outer scope: {outer.Inject<Theme>("theme").Label}");
                outer.Provide("theme", new Theme(Color.yellow, "inner"),
                    inner => inner.Text($"  nested scope: {inner.Inject<Theme>("theme").Label}"));
                // Under a mutate-in-place Provide this line would print "inner".
                var after = outer.Inject<Theme>("theme").Label;
                outer.Text($"outer scope after nesting: {after}",
                    new Style { color = after == "outer" ? Color.green : Color.red });
            });

            // Provide ended, so nothing should be resolvable here.
            var leaks = col.TryInject<Theme>("theme", out var leaked);
            col.Text(leaks ? $"LEAK: sibling sees {leaked.Label}" : "sibling sees no Theme",
                new Style { color = leaks ? Color.red : Color.green });
        }, style: Column(4f, 8f));

        // 3. Per-item scopes with real interaction. Two context types are in scope at once, and each
        // card owns an input, a button and a collapsible. Components are keyed by call site, and
        // Provide adds no node, so everything inside the loop needs an explicit id or all three
        // cards collapse onto one set of nodes.
        builder.Text("3. One scope per item, with inputs and per-branch state",
            new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            b.Button("Add subject", size: ComponentSize.Small,
                onClick: _ => _subjects.Add(new Subject($"s{++_subjectSeq}", $"Subject {_subjects.Count + 1}")));
            b.Button("Remove last", size: ComponentSize.Small, disabled: _subjects.Count == 0,
                onClick: _ =>
                {
                    if (_subjects.Count > 0) _subjects.RemoveAt(_subjects.Count - 1);
                });
            b.Button(_warmTheme ? "Theme: warm" : "Theme: cool", size: ComponentSize.Small,
                onClick: _ => _warmTheme = !_warmTheme);
        }, style: Row());

        var theme = _warmTheme ? new Theme(Color.yellow, "warm") : new Theme(Color.cyan, "cool");
        builder.Provide("theme", theme, themed =>
        {
            foreach (var subject in _subjects)
                themed.Provide("subject", subject, scope => SubjectCard(scope, subject.Id));
        });

        // 4. The same component under different scopes. Type into one input and only that subject's
        // label changes, and focus must survive the keystroke rather than jumping between cards.
        builder.Text("4. Live values (edit an input above)", new Style { fontSize = GameFont.Tiny });
        builder.Div(b =>
        {
            foreach (var subject in _subjects)
                b.Text($"{subject.Id}: {subject.Name} ({subject.Clicks})", id: $"echo-{subject.Id}");
        }, style: Row());
    }

    /// <summary>
    ///     A component that takes nothing but a key: everything it renders comes from the context it
    ///     is invoked under. <paramref name="key" /> is the subject's stable id, never its editable
    ///     name, or the keys would change while typing.
    /// </summary>
    private static void SubjectCard(UIBranch branch, string key)
    {
        var subject = branch.Inject<Subject>("subject");
        var theme = branch.Inject<Theme>("theme");

        branch.Div(card =>
            {
                card.Div(row =>
                    {
                        row.Text(subject.Name,
                            new Style { color = theme.Accent, width = Dimension.Px(110f), wordWrap = false },
                            $"{key}-name");
                        row.Input(subject.Name, v => subject.Name = v, 24, id: $"{key}-input");
                        row.Button($"Clicks: {subject.Clicks}", size: ComponentSize.Small,
                            onClick: _ => subject.Clicks++, id: $"{key}-btn");
                    }, style: Row(), id: $"{key}-row");

                // Collapsible keeps its open flag in branch.State on the parent record, so each card must
                // hold its own. It also passes context across a component boundary into its content lambda.
                card.Collapsible($"Notes for {subject.Name}", inner =>
                        inner.Text($"still in scope: {inner.Inject<Subject>("subject").Name} / {inner.Inject<Theme>("theme").Label}",
                            id: $"{key}-note"),
                    id: $"{key}-collapsible");
            }, style: Column(4f, 6f), id: $"{key}-card");
    }

    /// <summary>
    ///     Renders the <c>Void_Playground</c> <see cref="LayoutDef" /> parsed from XML, so the XML element
    ///     registry can be exercised next to the hand-written tabs.
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

    private static Style Row()
    {
        return Styles.Get((0, 0, 0));
    }

    private static Style Column(float gap, float padding)
    {
        return Styles.Get((1, gap, padding));
    }

    private static TaffyAxes Axes(float v)
    {
        return new TaffyAxes(Dimension.Px(v));
    }

    private static TaffyEdges Edges(float v)
    {
        return new TaffyEdges(Dimension.Px(v));
    }

    /// <summary>
    ///     Stand-in for the real context payload (a Pawn). Deliberately not game state, so the tab
    ///     works from the main menu like the rest of the playground.
    /// </summary>
    private sealed class Subject(string id, string name)
    {
        public readonly string Id = id;
        public int Clicks;
        public string Name = name;
    }

    /// <summary>A second, unrelated context type, to check both resolve in one scope.</summary>
    private sealed record Theme(Color Accent, string Label);

    private enum Tab
    {
        Scroll,
        Buttons,
        Text,
        Icons,
        Input,
        Collapsible,
        List,
        Context,
        Layout
    }
}
#endif
