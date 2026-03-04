using System.Collections.Generic;
using System.Linq;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public partial class Window_Editor : Window
{
    #region Fields
    // TODO: Should these fields be static?
    
    // Pawn-related fields
    // These are private, so they are only set through the TrySelect methods.
    private static Faction? selectedFaction;
    private static Pawn? selectedPawn;
    public static List<Pawn> selectedPawnGroup = [];

    private static Dictionary<Faction, List<Pawn>> Pawns_ByFaction = new();
    private static List<Pawn> Pawns_NoFaction = [];

    // Tab related fields
    private static TabDef? selectedTabDef;
    private static TabDef? secondarySelectedTabDef;
    private static List<TabDef> selectedTabDefsForPawn = [];
    private static List<TabRecord> tabsList = [];

    // UI-related fields
    private readonly Listing_Advanced _listing = new();
    private static Settings.WindowSize WindowSize => PawnEditorMod.Settings.Size;

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
    public static bool showHeadgear = true;
    public static bool showClothes = true;

    public static bool Playing => Current.ProgramState == ProgramState.Playing;

    #endregion

    #region Constructors & base methods

    public Window_Editor()
    {
        layer = Playing ? WindowLayer.Dialog : WindowLayer.Super;
        forcePause = true;
        closeOnClickedOutside = true;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        if (WindowSize is not (Settings.WindowSize.Medium or Settings.WindowSize.Large)) return;
        windowRect.x = 0f;
        windowRect.y = 0f;
    }

    public override void PreOpen()
    {
        base.PreOpen();

        // Update pawn lists.
        (Pawns_ByFaction, Pawns_NoFaction) = PawnLister.Pawns_ByFaction;

        switch (selectedPawn)
        {
            // Update selected pawn and faction if needed.
            case null when selectedFaction != null:
                TrySelect(selectedFaction);
                break;
            case null when selectedFaction == null:
                TrySelect(Find.FactionManager.OfPlayer);
                break;
        }

        // Update tabs
        RecacheTabs();
        TryRecachePawnGroup();
    }

    public override void PostClose()
    {
        base.PostClose();
        selectedTabDef = null;
        secondarySelectedTabDef = null;
        selectedTabDefsForPawn.Clear();
        tabsList.Clear();
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        DoLeftSection(inRect.TakeLeftPart(UIComponents.CardSize.x + 24f));
        inRect.xMin += UIUtility.ScrollBarWidth;

        // Draw tabs
        inRect.yMin += TabDrawer.TabHeight;
        Widgets.DrawMenuSection(inRect);
        tabsList = selectedTabDefsForPawn.Select(tabDef => new TabRecord(tabDef.LabelCap, delegate
        {
            if (Event.current.shift)
            {
                if (tabDef == selectedTabDef) selectedTabDef = secondarySelectedTabDef;
                secondarySelectedTabDef = tabDef;
                secondarySelectedTabDef.Worker.Notify_ContentChanged();
            }
            else
            {
                if (tabDef == secondarySelectedTabDef) secondarySelectedTabDef = selectedTabDef;
                selectedTabDef = tabDef;
                selectedTabDef.Worker.Notify_ContentChanged();
            }
        }, selectedTabDef == tabDef)).ToList();
        TabDrawer.DrawTabs(inRect, tabsList);

        var tabRect = inRect.TopPartPixels(TabDrawer.TabHeight) with
        {
            y = inRect.y - TabDrawer.TabHeight, width = tabsList.Count * 200f
        };
        if (Mouse.IsOver(tabRect))
            TooltipHandler.TipRegion(tabRect,
                "Click to select a tab. Shift click to select a secondary tab (large window size only).");

        if (selectedPawn != null && selectedTabDef != null)
        {
            if (WindowSize is Settings.WindowSize.Large)
            {
                var leftRect = inRect.LeftHalf();
                var rightRect = inRect.RightHalf();
                selectedTabDef.Worker.DoTabContents(ref leftRect);
                if (secondarySelectedTabDef != null) secondarySelectedTabDef.Worker.DoTabContents(ref rightRect);
                else
                    using (new TextBlock(TextAnchor.MiddleCenter))
                    {
                        Widgets.Label(rightRect, "No secondary tab selected.".Colorize(ColoredText.SubtleGrayColor));
                    }
            }
            else
            {
                selectedTabDef.Worker.DoTabContents(ref inRect);
            }
        }
        else
        {
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(inRect, "No pawn selected.".Colorize(ColoredText.SubtleGrayColor));
            }
        }
    }

    #endregion

    private void DoLeftSection(Rect inRect)
    {
        _listing.Begin(inRect);
        using (new TextBlock(GameFont.Tiny))
        {
            _listing.Label("Selected faction");
        }

        if (_listing.ButtonText_TruncateWithTooltip(selectedFaction != null ? selectedFaction.Name : "Wildlife"))
            Find.WindowStack.Add(FactionFloatMenu());

        _listing.Gap();
        var leftoverRect = _listing.PushDownFromHere();

        _listing.Gap();
        using (new TextBlock(GameFont.Tiny))
        {
            _listing.Label("Overview");
        }

        if (_listing.ButtonText_TruncateWithTooltip("Colony"))
        {
            Messages.Message("Not yet implemented.", MessageTypeDefOf.RejectInput);
        }

        if (_listing.ButtonText_TruncateWithTooltip("Faction..."))
        {
            Messages.Message("Not yet implemented.", MessageTypeDefOf.RejectInput);
        }

        _listing.Gap();
        var rowRect = _listing.GetRect(30f);
        if (UIComponents.ButtonText_TruncateWithTooltip(rowRect.LeftHalf(), "Save"))
        {
        }

        if (UIComponents.ButtonText_TruncateWithTooltip(rowRect.RightHalf(), "Load"))
        {
        }

        _listing.End();

        UIComponents.DrawReorderablePawnList(leftoverRect, ref selectedPawnGroup, selectedPawn, out var newSelectedPawn,
            out _);
        TrySelect(newSelectedPawn);
    }
}