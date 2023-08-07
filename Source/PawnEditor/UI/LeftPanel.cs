using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;


public static partial class PawnEditor
{
    private static void DoLeftPanel(Rect inRect, bool pregame)
    {
        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.SelectedFaction".Translate());

        if (selectedFaction == null || pregame)
        {
            selectedFaction = Faction.OfPlayer;
            CheckChangeTabGroup();
        }

        if (!pregame && Widgets.ButtonText(inRect.TakeTopPart(30f), "PawnEditor.SelectFaction".Translate()))
        {
            Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsVisibleInViewOrder.Select(faction =>
                    new FloatMenuOption(faction.Name, delegate
                    {
                        selectedFaction = faction;
                        RecachePawnList();
                        CheckChangeTabGroup();
                    }, faction.def.FactionIcon, faction.Color))
               .ToList()));
            inRect.yMin += 2;
        }

        var factionRect = inRect.TakeTopPart(54f).ContractedBy(3);
        Widgets.DrawOptionBackground(factionRect, showFactionInfo);
        MouseoverSounds.DoRegion(factionRect);
        var color = selectedFaction.Color;
        color.a = 0.2f;
        GUI.color = color;
        GUI.DrawTexture(factionRect.ContractedBy(6).RightPart(0.25f).BottomPart(0.75f), selectedFaction.def.FactionIcon);
        GUI.color = Color.white;
        using (new TextBlock(GameFont.Small))
            Widgets.Label(factionRect.ContractedBy(5f), selectedFaction.Name);
        if (Widgets.ButtonInvisible(factionRect))
        {
            showFactionInfo = !showFactionInfo;
            CheckChangeTabGroup();
        }

        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.ClickFactionOverview".Translate().Colorize(ColoredText.SubtleGrayColor));

        inRect.yMin += 4f;

        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.SelectedCategory".Translate());

        if (Widgets.ButtonText(inRect.TakeTopPart(30f), selectedCategory.LabelCapPlural()))
            Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(PawnCategory))
               .Cast<PawnCategory>()
               .Select(category =>
                    new FloatMenuOption(category.LabelCapPlural(), delegate
                    {
                        selectedCategory = category;
                        RecachePawnList();
                        CheckChangeTabGroup();
                    }))
               .ToList()));

        if (Widgets.ButtonText(inRect.TakeTopPart(25f), "Add".Translate().CapitalizeFirst()))
        {
            void AddPawnKind(PawnKindDef pawnKind)
            {
                AddPawn(PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, selectedFaction,
                    pregame ? PawnGenerationContext.PlayerStarter : PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true)), pregame);
            }

            var list = new List<FloatMenuOption>
            {
                new("PawnEditor.Add.Saved".Translate(selectedCategory.Label()), delegate
                {
                    var pawn = new Pawn();
                    SaveLoadUtility.LoadItem(pawn, p => AddPawn(p, pregame));
                })
            };

            if (selectedCategory == PawnCategory.Humans)
                list.Insert(0, new FloatMenuOption("PawnEditor.Add.PawnKind".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_ChoosePawnKindDef((Action<PawnKindDef>) (AddPawnKind)));
                    // Find.WindowStack.Add(new FloatMenu(DefDatabase<PawnKindDef>.AllDefs.Where(pk => pk.RaceProps.Humanlike)
                    //    .Select(pk => new FloatMenuOption(pk.LabelCap, () => AddPawnKind(pk)))
                    //    .ToList()));
                }));

            list.Add(new FloatMenuOption("PawnEditor.Add.OtherSave".Translate(), delegate { }));

            Find.WindowStack.Add(new FloatMenu(list));
        }

        List<Pawn> pawns;
        List<string> sections;
        int sectionCount;
        Action<Pawn, int, int> onReorder;
        Action<Pawn> onDelete;
        if (pregame)
        {
            if (selectedCategory == PawnCategory.Humans)
            {
                pawns = Find.GameInitData.startingAndOptionalPawns;
                sections = Enumerable.Repeat<string>(null, pawns.Count).ToList();
                sections[0] = "StartingPawnsSelected".Translate();
                sections[Find.GameInitData.startingPawnCount] = "StartingPawnsLeftBehind".Translate();
                sectionCount = 2;
                onReorder = delegate(Pawn _, int from, int to)
                {
                    StartingPawnUtility.ReorderRequests(from, to);
                    TutorSystem.Notify_Event("ReorderPawn");
                    if (to < Find.GameInitData.startingPawnCount && from >= Find.GameInitData.startingPawnCount)
                        TutorSystem.Notify_Event("ReorderPawnOptionalToStarting");
                };
                onDelete = pawn => pawn.Discard(true);
            }
            else
            {
                pawns = StartingThingsManager.GetPawns(selectedCategory);
                sections = Enumerable.Repeat<string>(null, pawns.Count).ToList();
                sections[0] = "StartingPawnsSelected".Translate();
                sectionCount = 1;
                onReorder = (_, _, _) => { };
                onDelete = pawn => pawn.Discard(true);
            }
        }
        else
        {
            (pawns, sections, sectionCount) = PawnLister.GetLists();
            onReorder = PawnLister.OnReorder;
            onDelete = PawnLister.OnDelete;
        }

        if (cachedPawnList == null)
        {
            cachedPawnList = pawns;
            Notify_PointsUsed();
        }

        inRect.yMin += 12f;
        DoPawnList(inRect.TakeTopPart(415f), pawns, sections, sectionCount, onReorder, onDelete);
    }

    private static void AddPawn(Pawn addedPawn, bool pregame)
    {
        if (pregame)
            if (selectedCategory == PawnCategory.Humans)
                Find.GameInitData.startingAndOptionalPawns.Add(addedPawn);
            else
                StartingThingsManager.AddPawn(selectedCategory, addedPawn);
        else
        {
            addedPawn.teleporting = true;
            Find.WorldPawns.PassToWorld(addedPawn, PawnDiscardDecideMode.KeepForever);
            addedPawn.teleporting = false;
            PawnLister.UpdateCache(selectedFaction, selectedCategory);
        }
    }
}
