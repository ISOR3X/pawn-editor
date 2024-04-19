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
               .Except(new() { PawnCategory.All })
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
            if (selectedFaction != Faction.OfPlayer && selectedCategory == PawnCategory.Mechs)
                Messages.Message("PawnEditor.NoAddMechWarning".Translate(), MessageTypeDefOf.RejectInput);
            else
                AddPawn(selectedCategory);
        }

        List<Pawn> pawns = null;
        List<string> sections = null;
        int sectionCount = 0;
        Action<Pawn, int, int> onReorder = null;
        Action<Pawn> onDelete;

        if (pregame)
        {
            TryRefreshSelectedPawn();
            if (selectedCategory == PawnCategory.Humans)
            {
                pawns = Find.GameInitData.startingAndOptionalPawns;
                if (pawns.Any())
                {
                    sections = Enumerable.Repeat<string>(null, pawns.Count).ToList();
                    sections[0] = "StartingPawnsSelected".Translate();
                    if (Find.GameInitData.startingPawnCount < sections.Count)
                        sections[Find.GameInitData.startingPawnCount] = "StartingPawnsLeftBehind".Translate();
                    sectionCount = 2;
                    // onReorder = delegate (Pawn _, int from, int to)
                    // {
                    //     StartingPawnUtility.ReorderRequests(from, to);
                    //     var a = Find.GameInitData.startingPawnsRequired;
                    //     TutorSystem.Notify_Event("ReorderPawn");
                    //     if (to < Find.GameInitData.startingPawnCount && from >= Find.GameInitData.startingPawnCount)
                    //         TutorSystem.Notify_Event("ReorderPawnOptionalToStarting");
                    // };
                    onReorder = PawnList.OnReorder;
                }

            }
            else
            {
                pawns = StartingThingsManager.GetPawns(selectedCategory);
                if (pawns.Any())
                {
                    sections = Enumerable.Repeat<string>(null, pawns.Count).ToList();
                    if (sections.Count > 0)
                        sections[0] = "StartingPawnsSelected".Translate();
                    sectionCount = 1;
                    onReorder = (_, _, _) => { };
                }
            }

            onDelete = pawn =>
            {
                DeletePawn(pawn, pawns);
                TabWorker_FactionOverview.RecachePawns(selectedFaction);
            };
        }
        else
        {
            (pawns, sections, sectionCount) = PawnList.GetLists();
            onReorder = PawnList.OnReorder;
            onDelete = pawn =>
            {
                if (selectedPawn == pawn) selectedPawn = pawns.Get(pawns.IndexOf(pawn) + 1);
                PawnList.OnDelete(pawn);
            };
        }

        inRect.yMin += 12f;
        if (pawns.Any())
        {
            DoPawnList(inRect.TakeTopPart(415f), pawns, sections, sectionCount, onReorder, onDelete);
        }
    }

    private static void DeletePawn(Pawn pawn, List<Pawn> pawns)
    {
        if (selectedPawn == pawn) selectedPawn = pawns.Get(pawns.IndexOf(pawn) + 1);
        pawns.Remove(pawn);
        if (pawns.Empty()) selectedPawn = null;
        if (pawn.Discarded is false)
        {
            pawn.Discard(true);
        }
    }

    private static void TryRefreshSelectedPawn()
    {
        // If selectedPawn isn't even present in the pawn list on the left, we have to force refresh it
        if (selectedCategory == PawnCategory.Humans)
        {
            if (Find.GameInitData.startingAndOptionalPawns.Contains(selectedPawn) is false)
            {
                selectedPawn = Find.GameInitData.startingAndOptionalPawns.FirstOrDefault();
            }
        }
        else
        {
            var pawns = StartingThingsManager.GetPawns(selectedCategory);
            if (pawns.Contains(selectedPawn) is false)
            {
                selectedPawn = pawns.FirstOrDefault();
            }
        }
    }

    public static void AddPawn(PawnCategory category)
    {
        AddResult AddPawnKind(PawnKindDef pawnKind) =>
            AddPawn(PawnGenerator.GeneratePawn(new(pawnKind, selectedFaction,
                Pregame ? PawnGenerationContext.PlayerStarter : PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true)), category);

        var list = new List<FloatMenuOption>
        {
            new("PawnEditor.Add.PawnKind".Translate(), () => Find.WindowStack.Add(new ListingMenu_PawnKindDef(category, AddPawnKind))),
            new("PawnEditor.Add.Saved".Translate(category.Label()), delegate
            {
                var pawn = new Pawn();
                SaveLoadUtility.LoadItem(pawn, p => AddPawn(p, category).HandleResult(), typePostfix: category.ToString());
            })
        };

        if (category == PawnCategory.Humans)
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
                    return AddPawn(pawn, category);
                }, "Add backer pawn"));
            }));

        // list.Add(new("PawnEditor.Add.OtherSave".Translate(), delegate { }));

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static AddResult AddPawn(Pawn addedPawn, PawnCategory category)
    {
        return new ConditionalInfo(CanUsePoints(addedPawn), new SuccessInfo(() =>
        {
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

            TabWorker_AnimalMech.Notify_PawnAdded(category);
            Notify_PointsUsed();
            Select(addedPawn);
        }));
    }
}
