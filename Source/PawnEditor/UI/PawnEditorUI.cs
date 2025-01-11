using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LudeonTK;
using PawnEditor.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class PawnEditor
{
    public static bool RenderClothes = true;
    public static bool RenderHeadgear = true;
    private static bool usePointLimit;
    private static float remainingPoints;
    public static Faction selectedFaction;
    private static Pawn selectedPawn;
    private static bool showFactionInfo;
    public static PawnCategory selectedCategory;
    private static float cachedValue;
    private static FloatMenuOption lastRandomization;
    private static TabGroupDef tabGroup;
    private static List<TabRecord> tabs;
    private static TabDef curTab;
    private static List<WidgetDef> widgets;
    private static int startingSilver;

    private static readonly TabDef widgetTab = new()
    {
        defName = "Widgets",
        label = "MiscRecordsCategory".Translate()
    };

    public static PawnLister PawnList = new();
    public static PawnListerBase AllPawns = new();

    private static Rot4 curRot = Rot4.South;

    public static bool Pregame;

    private static TabRecord cachedWidgetTab;

    public static void DoUI(Rect inRect, Action onClose, Action onNext)
    {
        var headerRect = inRect.TakeTopPart(50f);
        headerRect.xMax -= 10f;
        headerRect.yMax -= 20f;
        using (new TextBlock(GameFont.Medium))
            Widgets.Label(headerRect, $"{(Pregame ? "Create" : "PawnEditor.Edit")}Characters".Translate());

        if (ModsConfig.IdeologyActive)
        {
            Text.Font = GameFont.Small;
            string text = "ShowHeadgear".Translate();
            string text2 = "ShowApparel".Translate();
            var width = Mathf.Max(Text.CalcSize(text).x, Text.CalcSize(text2).x) + 4f + 24f;
            var rect2 = headerRect.TakeRightPart(width).TopPartPixels(Text.LineHeight * 2f);
            Widgets.CheckboxLabeled(rect2.TopHalf(), text, ref RenderHeadgear);
            Widgets.CheckboxLabeled(rect2.BottomHalf(), text2, ref RenderClothes);
            headerRect.xMax -= 4f;
        }

        string text3 = "PawnEditor.UsePointLimit".Translate();
        string text4 = "PawnEditor.PointsRemaining".Translate();
        var text5 = remainingPoints.ToStringMoney();
        var num = Text.CalcSize(text4).x;
        var width2 = Mathf.Max(Text.CalcSize(text3).x, num) + 4f + Mathf.Max(Text.CalcSize(text3).x, 24f);
        var rect3 = headerRect.TakeRightPart(width2).TopPartPixels(Text.LineHeight * 2f);
        UIUtility.CheckboxLabeledCentered(rect3.TopHalf(), text3, ref usePointLimit);
        rect3 = rect3.BottomHalf();
        Widgets.Label(rect3.TakeLeftPart(num), text4);
        var pointColor = usePointLimit ? ColoredText.CurrencyColor : ColoredText.SubtleGrayColor;
        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(rect3, text5.Colorize(pointColor));

        var bottomButtonsRect = inRect.TakeBottomPart(Page.BottomButHeight);

        inRect.yMin -= 10f;
        DoLeftPanel(inRect.TakeLeftPart(134), Pregame);
        inRect.xMin += 12f;
        inRect = inRect.ContractedBy(6);
        inRect.TakeTopPart(40);
        Widgets.DrawMenuSection(inRect);
        if (!tabs.NullOrEmpty() && (showFactionInfo || selectedPawn != null))
            TabDrawer.DrawTabs(inRect, tabs);
        inRect = inRect.ContractedBy(6);
        if (curTab != null)
        {
            if (curTab == widgetTab)
                DoWidgets(inRect);
            else if (showFactionInfo && selectedFaction != null)
                curTab.DrawTabContents(inRect, selectedFaction);
            else if (selectedPawn != null)
                curTab.DrawTabContents(inRect, selectedPawn);
        }

        // Since the Close button clears caches, it must be after all the stuff that uses the caches
        DoBottomButtons(bottomButtonsRect, onClose, Pregame
            ? onNext
            : () =>
            {
                if (!showFactionInfo && selectedPawn != null)
                    Find.WindowStack.Add(new FloatMenu(Find.Maps.Select(map => PawnList.GetTeleportOption(map, selectedPawn))
                        .Concat(Find.WorldObjects.Caravans.Select(caravan => PawnList.GetTeleportOption(caravan, selectedPawn)))
                        .Append(PawnList.GetTeleportOption(Find.World, selectedPawn))
                        .Append(new("PawnEditor.Teleport.Specific".Translate(), delegate
                        {
                            onClose();
                            DebugTools.curTool = new("PawnEditor.Teleport".Translate(), () =>
                            {
                                var cell = UI.MouseCell();
                                var map = Find.CurrentMap;
                                if (!cell.Standable(map) || cell.Fogged(map)) return;
                                PawnList.TeleportFromTo(selectedPawn, PawnList.GetLocation(selectedPawn), map);
                                selectedPawn.Position = cell;
                                selectedPawn.Notify_Teleported();
                                DebugTools.curTool = null;
                            });
                        }))
                        .ToList()));
            });
    }

    public static void DoBottomButtons(Rect inRect, Action onLeftButton, Action onRightButton)
    {
        Text.Font = GameFont.Small;
        if (Widgets.ButtonText(inRect.TakeLeftPart(Page.BottomButSize.x), Pregame ? "Back".Translate() : "Close".Translate()) && CanExit()) onLeftButton();

        if (Widgets.ButtonText(inRect.TakeRightPart(Page.BottomButSize.x), Pregame ? "Start".Translate() : "PawnEditor.Teleport".Translate())
            && CanExit()) onRightButton();

        var randomRect = new Rect(Vector2.zero, Page.BottomButSize).CenteredOnXIn(inRect).CenteredOnYIn(inRect);

        var buttonRect = new Rect(randomRect);
        var options = GetRandomizationOptions().ToList();

        //Add randomize options for factions
        if (!showFactionInfo && selectedPawn!=null && curTab == TabGroupDefOf.Humanlike.tabs[0])
        {
            //Add randomize all - including faction option
            var randomizeAllWithFactionOption = curTab.GetRandomizationOptions(selectedPawn).Select(option => new FloatMenuOption("PawnEditor.Randomize".Translate() + " " + "PawnEditor.AllIncludingFaction".Translate(), () =>
            {
                foreach (var option in options)
                {
                    if (option.Label.Contains("all"))
                        continue;

                    option.action();

                }
                lastRandomization = option;
                Notify_PointsUsed();
                List<Faction> factions = Find.FactionManager.AllFactionsVisibleInViewOrder.ToList();
                var chosenFaction = factions[Rand.Range(0, factions.Count - 1)];
                selectedFaction = chosenFaction;
                if (chosenFaction != selectedPawn.Faction) selectedPawn.SetFaction(chosenFaction);
                DoRecache();
            })).ToList();

            if (randomizeAllWithFactionOption.Any())
            {
                options.Add(randomizeAllWithFactionOption[0]);

                //Add randomize faction option
                options.Add(new FloatMenuOption("PawnEditor.SelectRandomFaction".Translate(), () =>
                {
                    List<Faction> factions = Find.FactionManager.AllFactionsVisibleInViewOrder.ToList();
                    var chosenFaction = factions[Rand.Range(0, factions.Count - 1)];
                    selectedFaction = chosenFaction;
                    if (chosenFaction != selectedPawn.Faction) selectedPawn.SetFaction(chosenFaction);
                    DoRecache();
                }));
            }
        }

        if (lastRandomization != null && Widgets.ButtonImageWithBG(randomRect.TakeRightPart(20), TexUI.RotRightTex, new Vector2(12, 12)))
        {
            var label = lastRandomization.Label.ToLower();
            lastRandomization = options.First(op => op.Label.Contains(label));
            lastRandomization.action();
            randomRect.TakeRightPart(1);
        }

        //Don't show the randomize button if no randomization options exist or you can't edit the faction overview
        if (options.Count > 0 && (selectedPawn != null && selectedFaction!=null))
            if (Widgets.ButtonText(randomRect, "Randomize".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
  
        buttonRect.x -= 5 + buttonRect.width;

        if (Widgets.ButtonText(buttonRect, "Save".Translate()))
            Find.WindowStack.Add(new FloatMenu(GetSaveLoadItems()
                .Select(static item => item.MakeSaveOption())
                .Where(static option => option != null)
                .ToList()));

        buttonRect.x += buttonRect.width * 2 + 10;

        if (Widgets.ButtonText(buttonRect, "Load".Translate()))
            Find.WindowStack.Add(new FloatMenu(GetSaveLoadItems()
                .Select(static item => item.MakeLoadOption())
                .Where(static option => option != null)
                .ToList()));
    }

    public static bool CanExit()
    {
        if (Pregame && usePointLimit && remainingPoints < 0)
        {
            Messages.Message("PawnEditor.NegativePoints".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }
        return true;
    }

    private static IEnumerable<SaveLoadItem> GetSaveLoadItems()
    {
        if (showFactionInfo)
            yield return new SaveLoadItem<Faction>("PawnEditor.Selected".Translate(), selectedFaction, new()
            {
                LoadLabel = "PawnEditor.LoadFaction".Translate()
            });
        else
        {
            IntVec3 pos = default;
            Map map = null;
            Rot4 rot = default;
            ThingOwner parent = null;
            yield return new SaveLoadItem<Pawn>("PawnEditor.Selected".Translate() + " " + "PawnEditor.Pawn".Translate().ToLower(), selectedPawn, new()
            {
                LoadLabel = "PawnEditor.LoadPawn".Translate(),
                TypePostfix = selectedCategory.ToString(),
                PrepareLoad = pawn =>
                {
                    if (pawn.Spawned)
                    {
                        pos = pawn.Position;
                        rot = pawn.Rotation;
                        map = pawn.Map;
                        pawn.DeSpawn();
                    }
                    else if (pawn.SpawnedOrAnyParentSpawned)
                    {
                        parent = pawn.holdingOwner;
                        pawn.holdingOwner.Remove(pawn);
                    }

                    if (Pregame) Find.GameInitData.startingPossessions.Remove(pawn);
                },
                OnLoad = pawn =>
                {
                    if (map != null)
                        GenSpawn.Spawn(pawn, pos, map, rot, WipeMode.VanishOrMoveAside, true);
                    else if (parent != null && !parent.TryAdd(pawn, false))
                    {
                        Log.Warning($"[Pawn Editor] Failed to add {pawn} to old parent {parent}.");
                        var thing = parent.owner switch
                        {
                            Thing { SpawnedOrAnyParentSpawned: true } thing1 => thing1,
                            ThingComp { parent: Thing { SpawnedOrAnyParentSpawned: true } thing2 } => thing2,
                            _ => null
                        };

                        if (thing != null && GenPlace.TryFindPlaceSpotNear(thing.Position, Rot4.South, thing.MapHeld, pawn, false, out var spot))
                            GenSpawn.Spawn(pawn, spot, thing.MapHeld, Rot4.South, WipeMode.VanishOrMoveAside, true);
                    }
                    if (Pregame) Find.GameInitData.startingPossessions.Add(pawn, new());
                    // TabWorker<Pawn>.Notify_OpenedDialog(); should recache tables?
                }
            });
        }

        if (Pregame)
            yield return new SaveLoadItem<StartingThingsManager.StartingPreset>("PawnEditor.StartingPreset".Translate().CapitalizeFirst(), new());
        else
            yield return new SaveLoadItem<Map>("PawnEditor.Colony".Translate(), Find.CurrentMap, new()
            {
                PrepareLoad = map =>
                {
                    MapDeiniter.DoQueuedPowerTasks(map);
                    map.weatherManager.EndAllSustainers();
                    Find.SoundRoot.sustainerManager.EndAllInMap(map);
                    Find.TickManager.RemoveAllFromMap(map);
                },
                OnLoad = map => map.FinalizeLoading()
            });

        if (curTab != null)
            if (showFactionInfo)
                foreach (var item in curTab.GetSaveLoadItems(selectedFaction))
                    yield return item;
            else
                foreach (var item in curTab.GetSaveLoadItems(selectedPawn))
                    yield return item;
    }

    private static IEnumerable<FloatMenuOption> GetRandomizationOptions()
    {
        if (curTab == null) return Enumerable.Empty<FloatMenuOption>();
        if (showFactionInfo)
        {
            return curTab.GetRandomizationOptions(selectedFaction);
        }
        else
        {
            List<FloatMenuOption> options = curTab.GetRandomizationOptions(selectedPawn).Select(option => new FloatMenuOption("PawnEditor.Randomize".Translate() + " " + option.Label.ToLower(), () =>
            {
                lastRandomization = option;
                option.action();
                Notify_PointsUsed();
                
            })).ToList();
            return options as IEnumerable<FloatMenuOption>;
        }
    }

    //Used for viewing null-faction pawns
    public static void RecachePawnListWithNoFactionPawns()
    {
        PawnEditor.needToRecacheNullFactionPawns = true;
        List<Pawn> noFPawns = PawnEditor_PawnsFinder.GetHumanPawnsWithoutFaction();
        CheckChangeTabGroup();
        TabWorker_FactionOverview.RecachePawnsWithPawnList(noFPawns);
        TabWorker_AnimalMech.Notify_PawnAdded(selectedCategory);
        List<Pawn> pawns;
        PawnList.UpdateCacheWithNullFaction();
        pawns = noFPawns;

        if (selectedPawn == null || !pawns.Contains(selectedPawn))
        {
            selectedPawn = pawns.FirstOrDefault();
            CheckChangeTabGroup();
        }
        PortraitsCache.Clear();
    }

    public static void RecachePawnList()
    {
        if (selectedFaction == null || !Find.FactionManager.allFactions.Contains(selectedFaction))
        {
            selectedFaction = Faction.OfPlayer;
            CheckChangeTabGroup();
        }

        if (selectedPawn is { Faction: { } pawnFaction } && pawnFaction != selectedFaction && Find.FactionManager.allFactions.Contains(pawnFaction))
        {
            selectedFaction = pawnFaction;
            CheckChangeTabGroup();
        }

        if (Pregame && selectedFaction != Faction.OfPlayer)
        {
            selectedFaction = Faction.OfPlayer;
            CheckChangeTabGroup();
        }

        TabWorker_FactionOverview.RecachePawns(selectedFaction);
        TabWorker_AnimalMech.Notify_PawnAdded(selectedCategory);

        List<Pawn> pawns;
        if (Pregame)
            pawns = selectedCategory == PawnCategory.Humans ? Find.GameInitData.startingAndOptionalPawns : StartingThingsManager.GetPawns(selectedCategory);
        else
        {
            PawnList.UpdateCache(selectedFaction, selectedCategory);
            (pawns, _, _) = PawnList.GetLists();
        }

        if (selectedPawn == null || !pawns.Contains(selectedPawn))
        {
            selectedPawn = pawns.FirstOrDefault();
            CheckChangeTabGroup();
        }

        PortraitsCache.Clear();
    }

    private static void SetTabGroup(TabGroupDef def)
    {
        tabGroup = def;
        curTab = def?.tabs?.FirstOrDefault();
        tabs = def?.tabs?.Select(static tab => new TabRecord(tab.LabelCap, () => curTab = tab, () => curTab == tab)).ToList() ?? new List<TabRecord>();
    }

    public static void CheckChangeTabGroup()
    {
        TabGroupDef desiredTabGroup;

        if (showFactionInfo && selectedFaction != null)
            desiredTabGroup = selectedFaction.IsPlayer ? TabGroupDefOf.PlayerFaction : TabGroupDefOf.NPCFaction;
        else if (showFactionInfo && selectedFaction == null)
            desiredTabGroup = TabGroupDefOf.NPCFaction;
        else if (selectedPawn != null)
            desiredTabGroup = selectedCategory == PawnCategory.Humans ? TabGroupDefOf.Humanlike : TabGroupDefOf.AnimalMech;
        else desiredTabGroup = null;

        if (desiredTabGroup != tabGroup)
            SetTabGroup(desiredTabGroup);

        RecacheWidgets();
    }

    private static void RecacheWidgets()
    {
        if (cachedWidgetTab != null) tabs.Remove(cachedWidgetTab);

        Func<WidgetDef, bool> predicate;
        if (showFactionInfo && selectedFaction != null) predicate = def => def.type == TabDef.TabType.Faction && def.ShowOn(selectedFaction);
        else if (selectedPawn != null) predicate = def => def.type == TabDef.TabType.Pawn && def.ShowOn(selectedPawn);
        else predicate = _ => false;

        widgets = DefDatabase<WidgetDef>.AllDefs.Where(predicate).ToList();

        if (widgets.NullOrEmpty())
            cachedWidgetTab = null;
        else
        {
            cachedWidgetTab = new(widgetTab.LabelCap, static () => curTab = widgetTab, static () => curTab == widgetTab);
            tabs.Add(cachedWidgetTab);
        }
    }

    public static void Select(Pawn pawn)
    {
        selectedPawn = pawn;
        var recache = false;
        if (pawn.Faction != selectedFaction)
        {
            selectedFaction = pawn.Faction;
            recache = true;
        }

        showFactionInfo = false;
        if (!selectedCategory.Includes(pawn))
        {
            selectedCategory = pawn.RaceProps.Humanlike ? PawnCategory.Humans : pawn.RaceProps.IsMechanoid ? PawnCategory.Mechs : PawnCategory.Animals;
            recache = true;
        }

        if (recache || tabGroup == TabGroupDefOf.PlayerFaction || tabGroup == TabGroupDefOf.NPCFaction )
        {
            CheckChangeTabGroup();
            DoRecache();
        }
    }

    public static void Select(Faction faction)
    {
        selectedFaction = faction;
        selectedPawn = null;
        showFactionInfo = true;
        CheckChangeTabGroup();
    }

    public static void GotoTab(TabDef tab)
    {
        curTab = tab;
    }

    public static RenderTexture GetPawnTex(Pawn pawn, Vector2 portraitSize, Rot4 dir, Vector3 cameraOffset = default, float cameraZoom = 1f) =>
        PortraitsCache.Get(pawn, portraitSize, dir, cameraOffset, cameraZoom,
            renderHeadgear: RenderHeadgear, renderClothes: RenderClothes, stylingStation: true);

    public static void SavePawnTex(Pawn pawn, string path, Rot4 dir)
    {
        var tex = GetPawnTex(pawn, new(128, 128), dir);
        RenderTexture.active = tex;
        var tex2D = new Texture2D(tex.width, tex.width);
        tex2D.ReadPixels(new(0, 0, tex.width, tex.height), 0, 0);
        RenderTexture.active = null;
        tex2D.Apply(true, false);
        var bytes = tex2D.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    public static void DrawPawnPortrait(Rect rect)
    {
        var image = GetPawnTex(selectedPawn, rect.size, curRot);
        GUI.color = Command.LowLightBgColor;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
        GUI.DrawTexture(rect, Command.BGTex);
        GUI.DrawTexture(rect, image);
        if (Widgets.ButtonImage(rect.ContractedBy(8).RightPartPixels(16).TopPartPixels(16), TexUI.RotRightTex))
            curRot.Rotate(RotationDirection.Counterclockwise);

        if (Widgets.InfoCardButtonWorker(rect.ContractedBy(8).LeftPartPixels(16).TopPartPixels(16))) Find.WindowStack.Add(new Dialog_InfoCard(selectedPawn));
    }
}