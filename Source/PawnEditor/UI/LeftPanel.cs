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

        if (selectedFaction == null || (pregame && selectedFaction != Faction.OfPlayer)) RecachePawnList();

        if (!pregame && Widgets.ButtonText(inRect.TakeTopPart(30f), "PawnEditor.SelectFaction".Translate()))
        {
            // Reversed so player faction is at the top of the float menu.
            Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsVisibleInViewOrder.Reverse()
               .Select(faction =>
                    new FloatMenuOption(faction.Name, delegate
                    {
                        selectedFaction = faction;
                        selectedPawn = null;
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

        var labelRect = inRect.TakeTopPart(Text.LineHeight);
        labelRect.xMax += 12f;
        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(labelRect, "PawnEditor.ClickFactionOverview".Translate().Colorize(ColoredText.SubtleGrayColor));

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

        if (Widgets.ButtonText(inRect.TakeTopPart(25f), "Add".Translate().CapitalizeFirst())) AddPawn(selectedCategory);

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
                if (sections.Count > 0)
                    sections[0] = "StartingPawnsSelected".Translate();
                sectionCount = 1;
                onReorder = (_, _, _) => { };
                onDelete = pawn => pawn.Discard(true);
            }
        }
        else
        {
            (pawns, sections, sectionCount) = PawnList.GetLists();
            onReorder = PawnList.OnReorder;
            onDelete = PawnList.OnDelete;
        }

        inRect.yMin += 12f;
        DoPawnList(inRect.TakeTopPart(415f), pawns, sections, sectionCount, onReorder, onDelete);
    }

    public static void AddPawn(PawnCategory category)
    {
        void AddPawnKind(PawnKindDef pawnKind)
        {
            AddPawn(PawnGenerator.GeneratePawn(new(pawnKind, selectedFaction,
                Pregame ? PawnGenerationContext.PlayerStarter : PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true)), category);
        }

        var list = new List<FloatMenuOption>
        {
            new("PawnEditor.Add.Saved".Translate(category.Label()), delegate
            {
                var pawn = new Pawn();
                SaveLoadUtility.LoadItem(pawn, p => AddPawn(p, category), typePostfix: category.ToString());
            })
        };

        if (category == PawnCategory.Humans)
        {
            list.Insert(0,
                new("PawnEditor.Add.PawnKind".Translate(), () => Find.WindowStack.Add(new ListingMenu_PawnKindDef(PawnCategory.Humans, AddPawnKind))));
            list.Add(new("PawnEditor.Add.Backer".Translate(), delegate
            {
                Find.WindowStack.Add(new ListingMenu<PawnBio>(SolidBioDatabase.allBios, bio => bio.name.ToStringFull, bio =>
                {
                    var pawn = PawnGenerator.GeneratePawn(new(selectedFaction.def.basicMemberKind, selectedFaction,
                        Pregame ? PawnGenerationContext.PlayerStarter : PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true, fixedBirthName: bio.name.First, fixedLastName: bio.name.Last, fixedGender: bio.gender switch
                        {
                            GenderPossibility.Male => Gender.Male,
                            GenderPossibility.Female => Gender.Female,
                            GenderPossibility.Either => null,
                            _ => throw new ArgumentOutOfRangeException()
                        }));
                    pawn.Name = bio.name;
                    pawn.story.Childhood = bio.childhood;
                    pawn.story.Adulthood = bio.adulthood;
                    AddPawn(pawn, category);
                }, "Add backer pawn"));
            }));
        }
        else if (selectedCategory is PawnCategory.Animals or PawnCategory.Mechs)
            list.Insert(0,
                new("PawnEditor.Add.PawnKind".Translate(),
                    () => Find.WindowStack.Add(new ListingMenu_PawnKindDef(selectedCategory, AddPawnKind))));
        // list.Add(new("PawnEditor.Add.OtherSave".Translate(), delegate { }));

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static void AddPawn(Pawn addedPawn, PawnCategory category)
    {
        if (!CanUsePoints(addedPawn))
        {
            addedPawn.Discard(true);
            return;
        }

        if (Pregame)
            if (category == PawnCategory.Humans)
            {
                Find.GameInitData.startingAndOptionalPawns.Add(addedPawn);
                Find.GameInitData.startingPossessions.Add(addedPawn, new());
            }
            else
                StartingThingsManager.AddPawn(category, addedPawn);
        else
        {
            addedPawn.teleporting = true;
            Find.WorldPawns.PassToWorld(addedPawn, PawnDiscardDecideMode.KeepForever);
            addedPawn.teleporting = false;
            PawnList.UpdateCache(selectedFaction, category);
        }


        Notify_PointsUsed();
        Select(addedPawn);
    }
}
