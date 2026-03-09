using System.Collections.Generic;
using System.Linq;
using PawnEditor.Extensions;
using PawnEditor.Layout;
using L = PawnEditor.Layout.FlexLayoutHelper;
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
    private Faction? _selectedFaction;
    private Pawn? _selectedPawn;
    private List<Pawn> selectedPawnGroup = [];


    // Tab related fields
    private static TabDef? selectedTabDef;
    private static List<TabDef> selectedTabDefsForPawn = [];
    private static List<TabRecord> tabsList = [];

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
        selectedTabDef = null;
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
            selectedTabDef = tabDef;
            selectedTabDef.Worker.Notify_ContentChanged();
        }, selectedTabDef == tabDef)).ToList();
        TabDrawer.DrawTabs(inRect, tabsList);

        var tabRect = inRect.TopPartPixels(TabDrawer.TabHeight) with
        {
            y = inRect.y - TabDrawer.TabHeight, width = tabsList.Count * 200f
        };
        if (Mouse.IsOver(tabRect))
            TooltipHandler.TipRegion(tabRect,
                "Click to select");

        if (_selectedPawn != null && selectedTabDef != null)
        {
            selectedTabDef.Worker.DoTabContents(ref inRect);
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
        var layout = L.Col([
            L.Cell(rect =>
            {
                using (new TextBlock(GameFont.Tiny))
                    Widgets.Label(rect, "Selected faction");
            }, flexBasis: 18f),

            L.Cell(rect =>
            {
                if (UIComponents.ButtonText_TruncateWithTooltip(rect,
                        _selectedFaction != null ? _selectedFaction.Name : "Wildlife"))
                    Find.WindowStack.Add(FactionFloatMenu());
            }, flexBasis: 30f),


            L.Cell(rect =>
            {
                UIComponents.DrawReorderablePawnList(rect, ref selectedPawnGroup, _selectedPawn,
                    out var newSelectedPawn, out _);
                if (_selectedPawn != newSelectedPawn) TrySelect(newSelectedPawn);
            }, flexGrow: 1f),

            L.Cell(rect =>
            {
                using (new TextBlock(GameFont.Tiny))
                    Widgets.Label(rect, "Overview");
            }, flexBasis: 18f),

            L.Cell(rect =>
            {
                if (UIComponents.ButtonText_TruncateWithTooltip(rect, "Colony"))
                    Messages.Message("Not yet implemented.", MessageTypeDefOf.RejectInput);
            }, flexBasis: 30f),

            L.Cell(rect =>
            {
                if (UIComponents.ButtonText_TruncateWithTooltip(rect, "Faction..."))
                    Messages.Message("Not yet implemented.", MessageTypeDefOf.RejectInput);
            }, flexBasis: 30f),


            L.Row([
                L.Cell(rect =>
                {
                    if (UIComponents.ButtonText_TruncateWithTooltip(rect, "Save"))
                    {
                    }
                }),
                L.Cell(rect =>
                {
                    if (UIComponents.ButtonText_TruncateWithTooltip(rect, "Load"))
                    {
                    }
                })
            ], flexBasis: 30f)
        ]);

        FlexLayoutEngine.Draw(layout, inRect, (action, rect) =>
        {
            action(rect);
            return rect.height > 1000f ? 0f : rect.height;
        });
    }
}