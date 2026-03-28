using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public partial class Window_Editor : Window
{
    #region Fields

    // Pawn-related fields
    // These are private, so they are only set through the TrySelect methods.
    // Static so their selection persists once a window is closed.
    private static Faction? _selectedFaction;
    private static Pawn? _selectedPawn;
    private static readonly List<Pawn> _selectedPawnGroup = []; // Pawns in the selected faction.

    // Tab related fields
    private static TabDef? _selectedTabDef;
    private static List<TabDef> _selectedTabDefsForPawn = [];
    private static List<TabRecord> _tabsList = [];

    private static Settings.WindowSize WindowSize => PawnEditorMod.Settings.size;

    private static readonly Dictionary<Settings.WindowSize, Vector2> WindowSizes = new()
    {
        { Settings.WindowSize.Small, Page.StandardSize },
        {
            Settings.WindowSize.Medium,
            new Vector2(Mathf.Min(1010f, UI.screenWidth), UI.screenHeight - (Playing ? MainButtonDef.ButtonHeight : 0f))
        },
        {
            Settings.WindowSize.Large,
            new Vector2(UI.screenWidth, UI.screenHeight - (Playing ? MainButtonDef.ButtonHeight : 0f))
        }
    };

    public override Vector2 InitialSize => WindowSizes[WindowSize];

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
        resizeable = true;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        if (WindowSize is Settings.WindowSize.Medium or Settings.WindowSize.Large)
        {
            windowRect.x = 0f;
            windowRect.y = 0f;
        }
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
        _selectedTabDef = null;
        _selectedTabDefsForPawn.Clear();
        _tabsList.Clear();
    }

    public override void DoWindowContents(Rect inRect)
    {
        DoLeftSection(inRect.TakeLeftPart(Widgets.CardSize.x + 24f));
        inRect.xMin += UIUtility.ScrollBarWidth;

        // Draw tabs
        inRect.yMin += TabDrawer.TabHeight;
        Verse.Widgets.DrawMenuSection(inRect);

        _tabsList = _selectedTabDefsForPawn.Select(tabDef => new TabRecord(tabDef.LabelCap, delegate
        {
            _selectedTabDef = tabDef;
            _selectedTabDef.Worker.Notify_ContentChanged();
        }, _selectedTabDef == tabDef)).ToList();
        TabDrawer.DrawTabs(inRect, _tabsList);

        var tabRect = inRect.TopPartPixels(TabDrawer.TabHeight) with
        {
            y = inRect.y - TabDrawer.TabHeight, width = _tabsList.Count * 200f
        };
        if (Mouse.IsOver(tabRect))
            TooltipHandler.TipRegion(tabRect,
                "Click to select tab");

        if (_selectedPawn != null && _selectedTabDef != null)
            _selectedTabDef.Worker.DoTabContents(ref inRect);
        else
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Verse.Widgets.Label(inRect, "No pawn selected.".Colorize(ColoredText.SubtleGrayColor));
            }
    }

    #endregion

    private void DoLeftSection(Rect inRect)
    {
        Taffy.Column(inRect, col =>
        {
            col.Item(height: Text.LineHeightOf(GameFont.Tiny), draw: rect =>
            {
                using (new TextBlock(GameFont.Tiny))
                    Verse.Widgets.Label(rect, "Selected faction");
            });
            col.Item(height: UIUtility.ButtonHeight, draw: rect =>
            {
                var (label, tex, c) = FactionUtility.GetFactionMeta(_selectedFaction);
                if (UIUtility.ButtonText_WithIcon(rect, label, tex, c))
                    Find.WindowStack.Add(FactionFloatMenu());
            });
            col.Item(grow: 1f, draw: rect =>
            {
                Widgets.DrawReorderablePawnList(rect, _selectedPawnGroup, _selectedPawn, out var newSelectedPawn);
                if (newSelectedPawn != _selectedPawn) TrySelect(newSelectedPawn);
            });
        });
    }
}