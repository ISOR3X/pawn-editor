using LudeonTK;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void.Taffy;

namespace PawnEditor.v2;

class Window_Editor : Window
{

    /// <summary>
    /// Void tree for rendering.
    /// </summary>
    private readonly UITree _tree = new();

    /// <summary>
    /// Save the position and size of the window across instances
    /// </summary>
    private static Rect savedWindowRect = new(0, 0, UI.screenWidth / 2f, UI.screenHeight);

    /// <summary>
    /// All tab workers valid for the selected context
    /// </summary>
    private List<TabWorker> _allTabWorkers = [];

    /// <summary>
    /// All tab records (for the tab widget) for the selected context
    /// </summary>
    private List<TabRecord> _allTabRecords = [];

    /// <summary>
    /// Current selected (on screen) tab worker.
    /// </summary>
    private TabWorker? _activeTabWorker;

    /// <summary>
    /// Current selected context. Can be any type. WeakRef ensures no errors in the situation that it is dropped by the GC.
    /// </summary>
    private readonly System.WeakReference<object?> _ctx = new(null);

    public Window_Editor()
    {
        forcePause = true;
        closeOnClickedOutside = true;
        resizeable = true;
        draggable = true;
    }

    public override void SetInitialSizeAndPosition() => windowRect = savedWindowRect;

    public Window_Editor(object p) : this()
    {
        Select(p);
    }

    [DebugAction("Void", "Open editor", allowedGameStates = AllowedGameStates.Invalid)]
    public static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_Editor>()) Find.WindowStack.TryRemove(typeof(Window_Editor));
        else Find.WindowStack.Add(new Window_Editor());
    }

    public override void DoWindowContents(Rect inRect)
    {
        _tree.Build(b =>
        {
            // Left panel
            /*
            b.Div(b2 =>
            {
                b2.Text("Selected faction", style: new Style { fontSize = GameFont.Tiny });
                b2.Button("label", Void.TexUI.ArrowRight, Color.red, block: true,
                    onClick: _ => { });
                b2.Div(rect =>
                    {

                    },
                    style: new Style
                    {
                        flexGrow = 1f,
                        margin = new TaffyEdges(Dimension.Px(GenUI.GapSmall), Dimension.Px(0), Dimension.Px(0),
                            Dimension.Px(0))
                    });
                b2.Button("Save Preset", block: true,
                    onClick: _ => { });
                b2.Button("Load Preset", block: true,
                    onClick: _ => { });
            }, style: new Style { flexDirection = TaffyFlexDirection.Column, width = Dimension.Px(Widgets.CardSize.x + GenUI.GapLabel) });
            */

            // Tabs
            b.Div(b2 =>
            {
                // DrawTabs draws above
                b2.Div(draw: r => TabDrawer.DrawTabs(r with { y = r.yMax }, _allTabRecords), style: new Style { height = Dimension.Px(TabDrawer.TabHeight), width = Dimension.Percent(1f), minHeight = Dimension.Px(TabDrawer.TabHeight) });

                // Provide context
                _ctx.TryGetTarget(out var innerCtx);
                b2.Provide("ctx", innerCtx, b3 =>
                {
                    b3.Div(b4 => _activeTabWorker?.DoTabContents(b4), draw: Verse.Widgets.DrawMenuSection, style: new Style { flexGrow = 1f, flexDirection = TaffyFlexDirection.Column, padding = new TaffyEdges(Dimension.Px(GenUI.Gap)) });
                });

            }, style: new Style { width = Dimension.Percent(1f), display = TaffyDisplay.Flex, flexDirection = TaffyFlexDirection.Column });

        }, style: new Style { width = Dimension.Percent(1f), height = Dimension.Percent(1f), display = TaffyDisplay.Flex, gap = new TaffyAxes(Dimension.Px(GenUI.Gap)) });
        _tree.Draw(inRect);
    }

    public bool Select(object item)
    {
        if (item is Pawn p)
        {
            // Get all tab workers for pawn
            _allTabWorkers = DefDatabase<TabDef>.AllDefsListForReading.Where(d => d.tabCategory.HasFlag(PawnUtility.GetPawnCategory(p))).Select(d => d.Worker).ToList();

            // Update selected tab
            _activeTabWorker = _allTabWorkers.FirstOrDefault();

            // Create new records for the tab widget
            _allTabRecords = _allTabWorkers.Select(tabWorker => new TabRecord(tabWorker.Def.label, () =>
            {
                _activeTabWorker = tabWorker;
            }, () => _activeTabWorker == tabWorker)).ToList();

            // Hold the subject weakly so the editor never pins it.
            _ctx.SetTarget(p);

            return true;
        }
        Messages.Message($"{item.GetType()} not supported yet", MessageTypeDefOf.RejectInput);
        return false;
    }

    public override void PostClose()
    {
        base.PostClose();

        _tree.Dispose();
        savedWindowRect = windowRect;
    }

}
