using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public static partial class PawnEditor
{
    private static bool renderClothes = true;
    private static bool renderHeadgear = true;
    private static bool usePointLimit;

    private static Faction selectedFaction;
    private static Pawn selectedPawn;
    private static bool showFactionInfo;
    private static PawnCategory selectedCategory;

    public static void DoUI(Rect inRect, Action onClose, Action onNext, bool pregame)
    {
        var headerRect = inRect.TakeTopPart(50f);
        headerRect.xMax -= 10f;
        headerRect.yMax -= 20f;
        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft, null))
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
        var text5 = 100000f.ToStringMoney();
        var num = Text.CalcSize(text4).x;
        var width2 = Mathf.Max(Text.CalcSize(text3).x, num) + 4f + Mathf.Max(Text.CalcSize(text3).x, 24f);
        var rect3 = headerRect.TakeRightPart(width2).TopPartPixels(Text.LineHeight * 2f);
        Widgets.CheckboxLabeled(rect3.TopHalf(), text3, ref usePointLimit, placeCheckboxNearText: true);
        rect3 = rect3.BottomHalf();
        Widgets.Label(rect3.TakeLeftPart(num), text4);
        using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(rect3, text5.Colorize(ColoredText.CurrencyColor));

        DoBottomButtons(inRect.TakeBottomPart(Page.BottomButHeight), pregame ? "Back".Translate() : "Close".Translate(), onClose,
            pregame ? "Start".Translate() : "PawnEditor.Teleport".Translate(), pregame
                ? onNext
                : () =>
                {
                    if (!showFactionInfo && selectedPawn != null)
                        Find.WindowStack.Add(new FloatMenu(Find.Maps.Select(map => new FloatMenuOption(map.Parent.LabelCap, () =>
                            {
                                if (selectedPawn.Spawned)
                                {
                                    selectedPawn.teleporting = true;
                                    selectedPawn.ExitMap(false, Rot4.Invalid);
                                }

                                GenSpawn.Spawn(selectedPawn, CellFinder.RandomCell(map), map);
                                selectedPawn.teleporting = false;
                            }))
                           .Concat(Find.WorldObjects.Caravans.Select(caravan => new FloatMenuOption(caravan.Name, () =>
                            {
                                if (selectedPawn.Spawned)
                                {
                                    selectedPawn.teleporting = true;
                                    selectedPawn.ExitMap(false, Rot4.Invalid);
                                }

                                Find.WorldPawns.PassToWorld(selectedPawn, PawnDiscardDecideMode.KeepForever);
                                caravan.AddPawn(selectedPawn, true);
                                selectedPawn.teleporting = false;
                            })))
                           .ToList()));
                });
        inRect.yMin -= 10f;
        DoLeftPanel(inRect.TakeLeftPart(134), pregame);
    }

    public static void DoBottomButtons(Rect inRect, string leftButtonLabel, Action onLeftButton, string rightButtonLabel, Action onRightButton)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(Page.BottomButSize.x), leftButtonLabel)) onLeftButton();

        if (Widgets.ButtonText(inRect.TakeRightPart(Page.BottomButSize.x), rightButtonLabel)) onRightButton();

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

        if (Widgets.ButtonText(buttonRect, "Save".Translate())) { }

        buttonRect.x += buttonRect.width * 2 + 10;

        if (Widgets.ButtonText(buttonRect, "Load".Translate())) { }
    }
}
