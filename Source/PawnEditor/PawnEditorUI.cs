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
    private static bool renderClothes = true;
    private static bool renderHeadgear = true;
    private static bool usePointLimit;
    private static float remainingPoints = 100000;
    private static Faction selectedFaction;
    private static Pawn selectedPawn;
    private static bool showFactionInfo;
    private static PawnCategory selectedCategory;
    private static float cachedPawnValue;
    private static List<Pawn> cachedPawnList;

    public static void DoUI(Rect inRect, Action onClose, Action onNext, bool pregame)
    {
        var headerRect = inRect.TakeTopPart(50f);
        headerRect.xMax -= 10f;
        headerRect.yMax -= 20f;
        using (new TextBlock(GameFont.Medium))
            Widgets.Label(headerRect, $"{(pregame ? "Create" : "PawnEditor.Edit")}Characters".Translate());

        if (ModsConfig.IdeologyActive)
        {
            string text = "ShowHeadgear".Translate();
            string text2 = "ShowApparel".Translate();
            var width = Mathf.Max(Text.CalcSize(text).x, Text.CalcSize(text2).x) + 4f + 24f;
            var rect2 = headerRect.TakeRightPart(width).TopPartPixels(Text.LineHeight * 2f);
            Widgets.CheckboxLabeled(rect2.TopHalf(), text, ref renderHeadgear);
            Widgets.CheckboxLabeled(rect2.BottomHalf(), text2, ref renderClothes);
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
                    Find.WindowStack.Add(new FloatMenu(Find.Maps.Select(map => PawnLister.GetTeleportOption(map, selectedPawn))
                       .Concat(Find.WorldObjects.Caravans.Select(caravan => PawnLister.GetTeleportOption(caravan, selectedPawn)))
                       .Append(PawnLister.GetTeleportOption(Find.World, selectedPawn))
                       .ToList()));
            }, pregame);

        inRect.yMin -= 10f;
        DoLeftPanel(inRect.TakeLeftPart(134), pregame);
    }

    public static void DoBottomButtons(Rect inRect, Action onLeftButton, Action onRightButton, bool pregame)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(Page.BottomButSize.x), pregame ? "Back".Translate() : "Close".Translate())) onLeftButton();

        if (Widgets.ButtonText(inRect.TakeRightPart(Page.BottomButSize.x), pregame ? "Start".Translate() : "PawnEditor.Teleport".Translate())) onRightButton();

        var randomRect = new Rect(Vector2.zero, Page.BottomButSize).CenteredOnXIn(inRect).CenteredOnYIn(inRect);

        var buttonRect = new Rect(randomRect);

        var rect = randomRect.TakeRightPart(20);
        var atlas = Widgets.ButtonBGAtlas;
        if (Mouse.IsOver(rect))
        {
            atlas = Widgets.ButtonBGAtlasMouseover;
            if (Input.GetMouseButton(0)) atlas = Widgets.ButtonBGAtlasClick;
        }

        Widgets.DrawAtlas(rect, atlas);

        GUI.DrawTexture(new Rect(Vector2.zero, Vector2.one * 12).CenteredOnXIn(rect).CenteredOnYIn(rect), TexUI.RotRightTex);

        if (Widgets.ButtonInvisible(rect)) { }

        randomRect.TakeRightPart(1);

        if (Widgets.ButtonText(randomRect, "Randomize".Translate())) { }

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
            yield return new SaveLoadItem<Faction>("PawnEditor.Selected".Translate(), selectedFaction);
        else
            yield return new SaveLoadItem<Pawn>("PawnEditor.Selected".Translate(), selectedPawn);

        if (pregame)
            yield return new SaveLoadItem<StartingThingsManager.StartingPreset>("PawnEditor.Selection".Translate(), new StartingThingsManager.StartingPreset());
        else
            yield return new SaveLoadItem<Map>("PawnEditor.Colony".Translate(), Find.CurrentMap, new SaveLoadParms<Map>
            {
                OnLoad = map => map.FinalizeLoading()
            });
    }

    public static void RecachePawnList()
    {
        if (selectedFaction == null || !Find.FactionManager.allFactions.Contains(selectedFaction)) selectedFaction = Faction.OfPlayer;
        PawnLister.UpdateCache(selectedFaction, selectedCategory);
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
}
