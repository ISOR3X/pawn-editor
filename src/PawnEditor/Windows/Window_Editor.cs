using HotSwap;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.Extensions;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

[HotSwappable]
public partial class Window_Editor : Window
{
    private void DoLeftSection(Rect inRect)
    {
        var (label, tex, c) = FactionUtility.GetFactionMeta(_selectedFaction);
        Void.Taffy.Div(inRect, builder =>
        {
            builder.Text("Selected faction", GameFont.Tiny);
            builder.Button(label, tex, c, block: true,
                onClick: _ => { Find.WindowStack.Add(FactionFloatMenu()); });
            builder.Item(rect =>
            {
                Widgets.DrawReorderablePawnList(rect, _selectedPawnGroup, _selectedPawn, out var newSelectedPawn);
                if (newSelectedPawn != _selectedPawn) TrySelect(newSelectedPawn);
            }, new StyleOverride { flexGrow = 1f, margin = new Rect<LengthPercentageAuto>(0, 0, GenUI.GapSmall, 0) });
        }, new StyleOverride { flexDirection = FlexDirection.Column });
    }

    #region Fields

    // Pawn-related fields
    // These are private, so they are only set through the TrySelect methods.
    // Static so their selection persists once a window is closed.
    private static Faction? _selectedFaction;
    private static Pawn? _selectedPawn;
    private static readonly List<Pawn> _selectedPawnGroup = []; // Pawns in the selected faction.

    // Tab related fields
    private static IContext? _currentContext;
    private static TabDef? _selectedTabDef;
    private static List<TabDef> _selectedTabDefsFor = [];
    private static List<TabRecord> _tabsList = [];

    public static Rect DefaultWindowRect = new(
        new Vector2((UI.screenWidth - Page.StandardSize.x) / 2, (UI.screenHeight - Page.StandardSize.y) / 2),
        Page.StandardSize);

    public static Rect SavedWindowRect = DefaultWindowRect;

    // Options
    public static bool ShowHeadgear = true;
    public static bool ShowClothes = true;

    public static bool Playing => Current.ProgramState == ProgramState.Playing;

    #endregion

    #region Constructors & base methods

    public Window_Editor()
    {
        layer = Playing ? WindowLayer.Dialog : WindowLayer.Super;
        forcePause = true;
        closeOnClickedOutside = true;
        resizeable = PawnEditorMod.PawnEditorSettings.allowResize;
        draggable = PawnEditorMod.PawnEditorSettings.allowResize;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        windowRect = SavedWindowRect;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        TryRecachePawnGroup();

        switch (_selectedPawn)
        {
            // Update selected pawn and faction if needed.
            case null when _selectedFaction != null:
                TrySelect(_selectedFaction);
                break;
            case null when _selectedFaction == null:
                TrySelect(Find.FactionManager.OfPlayer);
                break;
        }

        // Update tabs
        RecacheTabs();
    }

    public override void PostClose()
    {
        base.PostClose();
        SavedWindowRect = windowRect;

        _selectedTabDef = null;
        _selectedTabDefsFor.Clear();
        _tabsList.Clear();
    }

    public override void DoWindowContents(Rect inRect)
    {
        DoLeftSection(inRect.TakeLeftPart(Widgets.CardSize.x + 24f));
        inRect.xMin += UIUtility.ScrollBarWidth;

        // Draw tabs
        inRect.yMin += TabDrawer.TabHeight;
        Verse.Widgets.DrawMenuSection(inRect);

        _tabsList = _selectedTabDefsFor.Select(tabDef => new TabRecord(tabDef.LabelCap, delegate
        {
            _selectedTabDef = tabDef;
            _selectedTabDef.Worker.Notify_ContentChanged();
        }, _selectedTabDef == tabDef)).ToList();
        TabDrawer.DrawTabs(inRect, _tabsList);

        var tabRect = inRect.TopPartPixels(TabDrawer.TabHeight) with
        {
            y = inRect.y - TabDrawer.TabHeight, width = _tabsList.Count * 200f
        };
        if (Mouse.IsOver(tabRect)) TooltipHandler.TipRegion(tabRect, "Click to select tab");

        if (_currentContext != null && _selectedTabDef != null)
            _selectedTabDef.Worker.DoTabContents(ref inRect, _currentContext);
        else
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Verse.Widgets.Label(inRect, "No pawn selected.".Colorize(ColoredText.SubtleGrayColor));
            }
    }

    #endregion
}