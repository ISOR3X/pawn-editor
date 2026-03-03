using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor
{
    [HotSwappable]
    public partial class Window_Editor : Window
    {
        #region Fields

        // Pawn related fields
        // These are private so they are only set through the TrySelect methods.
        [CanBeNull] private static Faction selectedFaction;
        [CanBeNull] private static Pawn selectedPawn;
        public static List<Pawn> selectedPawnGroup = new();

        private static Dictionary<Faction, List<Pawn>> Pawns_ByFaction = new();
        private static List<Pawn> Pawns_NoFaction = new();

        // Tab related fields
        private static TabDef selectedTabDef;
        private static TabDef secondarySelectedTabDef;
        private static List<TabDef> selectedTabDefsForPawn = new();
        private static List<TabRecord> tabsList = new();

        // UI related fields
        private readonly Listing_Advanced listing = new();
        private Settings.WindowSize windowSize => PawnEditorMod.Settings.Size;

        private static readonly Dictionary<Settings.WindowSize, Vector2> WindowSizes = new()
        {
            { Settings.WindowSize.Small, Page.StandardSize },
            { Settings.WindowSize.Medium, new(Mathf.Min(1010f, UI.screenWidth), UI.screenHeight - (Playing ? MainButtonDef.ButtonHeight : 0f)) },
            { Settings.WindowSize.Large, new(UI.screenWidth, UI.screenHeight - (Playing ? MainButtonDef.ButtonHeight : 0f)) }
        };

        public override Vector2 InitialSize => WindowSizes[windowSize];

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
            if (windowSize is Settings.WindowSize.Medium or Settings.WindowSize.Large)
            {
                windowRect.x = 0f;
                windowRect.y = 0f;
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            // Update pawn lists.
            (Pawns_ByFaction, Pawns_NoFaction) = PawnLister.Pawns_ByFaction;

            // Update selected pawn and faction if needed.
            if (selectedPawn == null && selectedFaction != null) TrySelect(selectedFaction);
            else if (selectedPawn == null && selectedFaction == null) TrySelect(Find.FactionManager.OfPlayer);

            // Update tabs
            RecacheTabs();
            TryRecachePawnGroup();
        }

        public override void DoWindowContents(Rect inRect)
        {
            DoLeftSection(inRect.TakeLeftPart(UIComponents.CardSize.x + 24f));
            inRect.xMin += UIUtility.scrollBarWidth;

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

            Rect tabRect = inRect.TopPartPixels(TabDrawer.TabHeight) with { y = inRect.y - TabDrawer.TabHeight, width = tabsList.Count * 200f };
            if (Mouse.IsOver(tabRect))
            {
                TooltipHandler.TipRegion(tabRect, "Click to select a tab. Shift click to select a secondary tab (large window size only).");
            }

            if (selectedPawn != null)
            {
                if (windowSize is Settings.WindowSize.Large)
                {
                    var leftRect = inRect.LeftHalf();
                    var rightRect = inRect.RightHalf();
                    selectedTabDef.Worker.DoTabContents(ref leftRect);
                    if (secondarySelectedTabDef != null) secondarySelectedTabDef.Worker.DoTabContents(ref rightRect);
                    else
                        using (new TextBlock(TextAnchor.MiddleCenter))
                            Widgets.Label(rightRect, "No secondary tab selected.".Colorize(ColoredText.SubtleGrayColor));
                }
                else
                {
                    selectedTabDef.Worker.DoTabContents(ref inRect);
                }
            }
            else
            {
                using (new TextBlock(TextAnchor.MiddleCenter))
                    Widgets.Label(inRect, "No pawn selected.".Colorize(ColoredText.SubtleGrayColor));
            }
        }

        #endregion


        private void DoLeftSection(Rect inRect)
        {
            listing.Begin(inRect);
            using (new TextBlock(GameFont.Tiny)) listing.Label("Selected faction");
            if (listing.ButtonText_TruncateWithTooltip(selectedFaction != null ? selectedFaction.Name : "Wildlife"))
            {
                Find.WindowStack.Add(FactionFloatMenu());
            }

            listing.Gap();
            var leftoverRect = listing.PushDownFromHere();

            listing.Gap();
            using (new TextBlock(GameFont.Tiny)) listing.Label("Overview");
            if (listing.ButtonText_TruncateWithTooltip("Colony"))
            {
            }

            if (listing.ButtonText_TruncateWithTooltip("Faction..."))
            {
            }

            listing.Gap();
            var rowRect = listing.GetRect(30f);
            if (UIComponents.ButtonText_TruncateWithTooltip(rowRect.LeftHalf(), "Save"))
            {
            }

            if (UIComponents.ButtonText_TruncateWithTooltip(rowRect.RightHalf(), "Load"))
            {
            }

            listing.End();

            UIComponents.DrawReorderablePawnList(leftoverRect, ref selectedPawnGroup, selectedPawn, out Pawn newSelectedPawn, out float _);
            TrySelect(newSelectedPawn);
        }
    }
}