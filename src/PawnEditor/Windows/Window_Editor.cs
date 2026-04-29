using RimWorld;
using RimWorld.Planet;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.Extensions;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

public partial class Window_Editor : Window
{
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

        if (_currentContext != null && _selectedTabDef != null)
            _selectedTabDef.Worker.DoTabContents(ref inRect, _currentContext);
        else
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Verse.Widgets.Label(inRect, "No pawn selected.".Colorize(ColoredText.SubtleGrayColor));
            }
    }

    private void DoLeftSection(Rect inRect)
    {
        var (label, tex, c) = FactionUtility.GetFactionMeta(_selectedFaction);
        Void.Taffy.Div(inRect, builder =>
        {
            builder.Text("Selected faction", style: new StyleOverride { fontSize = GameFont.Tiny });
            builder.Button(label, tex, c, block: true,
                onClick: _ => { Find.WindowStack.Add(FactionFloatMenu()); });
            builder.Item(rect =>
            {
                Widgets.DrawReorderablePawnList(rect, PawnLister.Pawns_ByFaction[_selectedFaction], SelectedPawn,
                    out var newSelectedPawn);
                if (newSelectedPawn != SelectedPawn) TrySelect(newSelectedPawn);
            }, new StyleOverride { flexGrow = 1f, margin = new Rect<LengthPercentageAuto>(0, 0, GenUI.GapSmall, 0) });
            /*
            builder.Button("Save Preset", block: true, disabled: SelectedPawn == null,
                onClick: _ => SaveSelectedPawn());
            builder.Button("Load Preset", block: true,
                onClick: _ => ShowLoadPawnMenu());
            */
        }, new StyleOverride { flexDirection = FlexDirection.Column });
    }

    private void SaveSelectedPawn()
    {
        if (SelectedPawn is not { } pawn) return;
        var name = pawn.LabelShort;
        foreach (var ch in Path.GetInvalidFileNameChars()) name = name.Replace(ch.ToString(), "");
        if (name.NullOrEmpty()) name = "pawn";
        PersistenceUtility.SavePawn(pawn, name);
        Messages.Message($"Saved preset: {name}", MessageTypeDefOf.SilentInput);
    }

    private void ShowLoadPawnMenu()
    {
        var files = PersistenceUtility.GetPresetFiles();
        if (files.Length == 0)
        {
            Messages.Message("No pawn presets found.", MessageTypeDefOf.RejectInput);
            return;
        }

        var options = files
            .Select(path => new FloatMenuOption(Path.GetFileNameWithoutExtension(path), () => LoadAndAddPawn(path)))
            .ToList();
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static void LoadAndAddPawn(string path)
    {
        var pawn = PersistenceUtility.LoadPawn(path);
        if (pawn == null)
        {
            Messages.Message("Failed to load pawn preset.", MessageTypeDefOf.RejectInput);
            return;
        }

        pawn.SetFactionDirect(Faction.OfPlayer);
        var map = Find.AnyPlayerHomeMap;
        if (map != null)
            GenPlace.TryPlaceThing(pawn, CellFinder.RandomEdgeCell(map), map, ThingPlaceMode.Near);
        else
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
    }

    #region Fields

    // Pawn-related fields
    // These are private, so they are only set through the TrySelect methods.
    // WeakRefs are static so the selection persists once a window is closed; strong refs are instance-scoped.
    private static readonly System.WeakReference<Faction?> SelectedFactionWeak = new(null);
    private static readonly System.WeakReference<IContext?> SelectedContextWeak = new(null);
    private Faction? _selectedFaction;
    private IContext? _currentContext;
    private Pawn? SelectedPawn => (_currentContext as IContext<Pawn>)?.Value;

    // Tab related fields
    private TabDef? _selectedTabDef;
    private List<TabDef> _selectedTabDefsFor = [];
    private List<TabRecord> _tabsList = [];

    public static Rect DefaultWindowRect = new(0, 0, UI.screenWidth / 2f, UI.screenHeight);
    public static Rect SavedWindowRect = DefaultWindowRect;

    // Options
    public static bool ShowHeadgear = true;
    public static bool ShowClothes = true;

    #endregion

    #region Constructors & base methods

    public Window_Editor()
    {
        layer = Current.ProgramState == ProgramState.Playing ? WindowLayer.Dialog : WindowLayer.Super;
        forcePause = true;
        closeOnClickedOutside = true;
        resizeable = PawnEditorMod.Settings.allowResize;
        draggable = PawnEditorMod.Settings.allowResize;
    }

    public override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        windowRect = SavedWindowRect;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        // When the reference is lost, of context is null, select the player faction as default. This also selects a player faction pawn.
        if (!SelectedContextWeak.TryGetTarget(out _currentContext)) TrySelect(Find.FactionManager.OfPlayer);
        // Set the _selectedFaction back to what is stored in the weak ref.
        SelectedFactionWeak.TryGetTarget(out _selectedFaction);

        // Update tabs
        RecacheTabs();
    }

    public override void PostClose()
    {
        base.PostClose();
        SavedWindowRect = windowRect;

        // Clear the direct refs.
        _selectedFaction = null;
        _currentContext = null;

        _selectedTabDef = null;
        _selectedTabDefsFor.Clear();
        _tabsList.Clear();
    }

    #endregion
}