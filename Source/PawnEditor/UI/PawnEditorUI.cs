using System;
using System.Collections.Generic;
using System.Linq;
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
    private static float remainingPoints = 100000;
    private static Faction selectedFaction;
    private static Pawn selectedPawn;
    private static bool showFactionInfo;
    private static PawnCategory selectedCategory;
    private static float cachedPawnValue;
    private static List<Pawn> cachedPawnList;
    private static FloatMenuOption lastRandomization;
    private static TabGroupDef tabGroup;
    private static List<TabRecord> tabs;
    private static TabDef curTab;
    public static PawnLister PawnList = new();

    private static Rot4 curRot = Rot4.South;

    public static bool Pregame;

    public static void DoUI(Rect inRect, Action onClose, Action onNext, bool pregame)
    {
        Pregame = pregame;
        var headerRect = inRect.TakeTopPart(50f);
        headerRect.xMax -= 10f;
        headerRect.yMax -= 20f;
        using (new TextBlock(GameFont.Medium))
            Widgets.Label(headerRect, $"{(pregame ? "Create" : "PawnEditor.Edit")}Characters".Translate());

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
        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(rect3, text5.Colorize(ColoredText.CurrencyColor));

        DoBottomButtons(inRect.TakeBottomPart(Page.BottomButHeight), onClose, pregame
            ? onNext
            : () =>
            {
                if (!showFactionInfo && selectedPawn != null)
                    Find.WindowStack.Add(new FloatMenu(Find.Maps.Select(map => PawnList.GetTeleportOption(map, selectedPawn))
                       .Concat(Find.WorldObjects.Caravans.Select(caravan => PawnList.GetTeleportOption(caravan, selectedPawn)))
                       .Append(PawnList.GetTeleportOption(Find.World, selectedPawn))
                       .ToList()));
            }, pregame);

        inRect.yMin -= 10f;
        DoLeftPanel(inRect.TakeLeftPart(134), pregame);
        inRect.xMin += 12f;
        inRect = inRect.ContractedBy(6);
        inRect.TakeTopPart(40);
        Widgets.DrawMenuSection(inRect);
        if (!tabs.NullOrEmpty()) TabDrawer.DrawTabs(inRect, tabs, 1);
        inRect = inRect.ContractedBy(6);
        if (curTab != null)
            if (showFactionInfo)
                curTab.DrawTabContents(inRect, selectedFaction);
            else
                curTab.DrawTabContents(inRect, selectedPawn);
    }

    public static void DoBottomButtons(Rect inRect, Action onLeftButton, Action onRightButton, bool pregame)
    {
        Text.Font = GameFont.Small;
        if (Widgets.ButtonText(inRect.TakeLeftPart(Page.BottomButSize.x), pregame ? "Back".Translate() : "Close".Translate())) onLeftButton();

        if (Widgets.ButtonText(inRect.TakeRightPart(Page.BottomButSize.x), pregame ? "Start".Translate() : "PawnEditor.Teleport".Translate())) onRightButton();

        var randomRect = new Rect(Vector2.zero, Page.BottomButSize).CenteredOnXIn(inRect).CenteredOnYIn(inRect);

        var buttonRect = new Rect(randomRect);

        if (lastRandomization != null && Widgets.ButtonImageWithBG(randomRect.TakeRightPart(20), TexUI.RotRightTex, new Vector2(12, 12)))
            lastRandomization.action();

        randomRect.TakeRightPart(1);

        if (Widgets.ButtonText(randomRect, "Randomize".Translate())) Find.WindowStack.Add(new FloatMenu(GetRandomizationOptions().ToList()));

        buttonRect.x -= 5 + buttonRect.width;

        if (Widgets.ButtonText(buttonRect, "Save".Translate()))
            Find.WindowStack.Add(new FloatMenu(GetSaveLoadItems(pregame).Select(item => item.MakeSaveOption()).ToList()));

        buttonRect.x += buttonRect.width * 2 + 10;

        if (Widgets.ButtonText(buttonRect, "Load".Translate()))
            Find.WindowStack.Add(new FloatMenu(GetSaveLoadItems(pregame).Select(item => item.MakeLoadOption()).ToList()));
    }

    private static IEnumerable<SaveLoadItem> GetSaveLoadItems(bool pregame)
    {
        if (showFactionInfo)
            yield return new SaveLoadItem<Faction>("PawnEditor.Selected".Translate(), selectedFaction, new()
            {
                LoadLabel = "PawnEditor.LoadFaction".Translate()
            });
        else
            yield return new SaveLoadItem<Pawn>("PawnEditor.Selected".Translate(), selectedPawn, new()
            {
                LoadLabel = "PawnEditor.LoadPawn".Translate()
            });

        if (pregame)
            yield return new SaveLoadItem<StartingThingsManager.StartingPreset>("PawnEditor.Selection".Translate(), new());
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
        return (showFactionInfo ? curTab.GetRandomizationOptions(selectedFaction) : curTab.GetRandomizationOptions(selectedPawn))
           .Select(option => new FloatMenuOption("PawnEditor.Randomize".Translate() + " " + option.Label.ToLower(), () =>
            {
                lastRandomization = option;
                option.action();
            }));
    }

    public static void RecachePawnList()
    {
        if (selectedFaction == null || !Find.FactionManager.allFactions.Contains(selectedFaction)) selectedFaction = Faction.OfPlayer;
        if (selectedPawn is { Faction: { } pawnFaction } && pawnFaction != selectedFaction) selectedFaction = pawnFaction;
        PawnList.UpdateCache(selectedFaction, selectedCategory);
        TabWorker_FactionOverview.RecachePawns(selectedFaction);
        PortraitsCache.Clear();
        ResetPoints();
    }

    public static void ResetPoints()
    {
        remainingPoints = 100000;
        cachedPawnValue = 0;
        cachedPawnList = null;
        Notify_PointsUsed();
    }

    public static void Notify_PointsUsed(float? amount = null)
    {
        if (amount.HasValue)
            remainingPoints -= amount.Value;
        else if (cachedPawnList?.Count > 0)
        {
            var pawnValue = cachedPawnList.Sum(p => p.GetStatValue(StatDefOf.MarketValue));
            remainingPoints -= pawnValue - cachedPawnValue;
            cachedPawnValue = pawnValue;
        }
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

        if (showFactionInfo && selectedFaction != null) desiredTabGroup = selectedFaction.IsPlayer ? TabGroupDefOf.PlayerFaction : TabGroupDefOf.NPCFaction;
        else if (selectedPawn != null) desiredTabGroup = selectedCategory == PawnCategory.Humans ? TabGroupDefOf.Humanlike : TabGroupDefOf.AnimalMech;
        else desiredTabGroup = null;

        if (desiredTabGroup != tabGroup) SetTabGroup(desiredTabGroup);
    }

    public static void Select(Pawn pawn)
    {
        selectedPawn = pawn;
        selectedFaction = pawn.Faction;
        showFactionInfo = false;
        if (!selectedCategory.Includes(pawn))
        {
            selectedCategory = pawn.RaceProps.Humanlike ? PawnCategory.Humans : pawn.RaceProps.IsMechanoid ? PawnCategory.Mechs : PawnCategory.Animals;
            RecachePawnList();
        }

        CheckChangeTabGroup();
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
        PortraitsCache.Get(pawn, portraitSize, dir, cameraOffset, cameraZoom, renderHeadgear: RenderHeadgear, renderClothes: RenderClothes,
            stylingStation: true);

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
