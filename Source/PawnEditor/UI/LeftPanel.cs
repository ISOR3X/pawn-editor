using System;
using System.Collections.Generic;
using System.Linq;
using PawnEditor.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

public static partial class PawnEditor
{
    static bool needToRecacheNullFactionPawns = false;
    
    //Recaches the pawn list regardless of whether a faction is selected or not.
    public static void DoRecache()
    {
        if(selectedFaction == null)
        {
            RecachePawnListWithNoFactionPawns();
        }
        else
        {
            RecachePawnList();
        }
    }
    private static void DoLeftPanel(Rect inRect, bool pregame)
    {
        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.SelectedFaction".Translate());

        //recaches the pawn list if needed
        if (selectedFaction == null && needToRecacheNullFactionPawns)
        {
            RecachePawnListWithNoFactionPawns();
            needToRecacheNullFactionPawns = false;
        }
        else if ((pregame && selectedFaction != Faction.OfPlayer)) RecachePawnList();

        //Faction selection options include every faction with at least one humanlike (including Ancients)
        if (!pregame && Widgets.ButtonText(inRect.TakeTopPart(30f), "PawnEditor.SelectFaction".Translate()))
        {
            IEnumerable<Faction> existingFaction = PawnEditor_PawnsFinder.GetAllFactionsContainingAtLeastOneHumanLike();

            List<FloatMenuOption> options = existingFaction
               .Select(faction =>
                    new FloatMenuOption(faction.Name, delegate
                    {
                        selectedFaction = faction;
                        selectedPawn = null;
                        RecachePawnList();
                        CheckChangeTabGroup();
                        
                    }, faction.def.FactionIcon, faction.Color))
               .ToList();

            options.Add(new FloatMenuOption("No Faction", () =>
            {
                selectedFaction = null;
                selectedPawn = null;
                needToRecacheNullFactionPawns = true;
                CheckChangeTabGroup();
            }));
            Find.WindowStack.Add(new FloatMenu(options));
            inRect.yMin += 2;
        }

        var factionRect = inRect.TakeTopPart(54f).ContractedBy(3);
        Widgets.DrawOptionBackground(factionRect, showFactionInfo);
        MouseoverSounds.DoRegion(factionRect);
        var color = selectedFaction?.Color == null? Color.white : selectedFaction.Color; //since selectedFaction may be null, we need to choose a default color.
        color.a = 0.2f;
        GUI.color = color;

        if (selectedFaction != null)
        {
            if (selectedFaction.def.FactionIcon != null)
                GUI.DrawTexture(factionRect.ContractedBy(6).RightPart(0.25f).BottomPart(0.75f), selectedFaction.def.FactionIcon);
        }
        else
        {
            Widgets.DrawBox(factionRect.ContractedBy(6).RightPart(0.25f).BottomPart(0.75f));
        }

        GUI.color = Color.white;

        using (new TextBlock(GameFont.Small))
                Widgets.Label(factionRect.ContractedBy(5f), selectedFaction == null ? "PawnEditor.NoFaction".Translate().ToString() : selectedFaction.Name); //since selectedFaction may be null, we need a text to display if it is.

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

        //recache with appropriate list (null or faction) instead of always recaching with a faction.
        if (Widgets.ButtonText(inRect.TakeTopPart(30f), selectedCategory.LabelCapPlural()))
                Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(PawnCategory))
                   .Cast<PawnCategory>()
                   .Except(new() { PawnCategory.All })
                   .Select(category =>
                        new FloatMenuOption(category.LabelCapPlural(), delegate
                        {
                            selectedCategory = category;
                            DoRecache();
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
                DoRecache();
            };
        }
        else
        {
            (pawns, sections, sectionCount) = PawnList.GetLists();
                
            //set the left pawn collection to the list of pawns without faction
            if(selectedFaction == null)
            {
                pawns = PawnEditor_PawnsFinder.GetHumanPawnsWithoutFaction();
                if (!pawns.Any())
                {
                    sections = new();
                    sectionCount = 0;
                }
            }

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
                RefreshTabs();
            }
        }
        else
        {
            var pawns = StartingThingsManager.GetPawns(selectedCategory);
            if (pawns.Contains(selectedPawn) is false)
            {
                selectedPawn = pawns.FirstOrDefault();
                RefreshTabs();
            }
        }
    }

    private static void RefreshTabs()
    {
        TabWorker<Pawn>.Notify_OpenedDialog();
        TabWorker_Gear.ClearCaches();
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
        Find.WindowStack.Add(new FloatMenu(list));
    }

    public static AddResult AddPawn(Pawn addedPawn, PawnCategory category)
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
                {
                    StartingThingsManager.AddPawn(category, addedPawn);
                }
            else
                addedPawn.teleporting = true;
            Find.WorldPawns.PassToWorld(addedPawn, PawnDiscardDecideMode.KeepForever);
            addedPawn.teleporting = false;
            PawnList.UpdateCache(selectedFaction, category);

            TabWorker_AnimalMech.Notify_PawnAdded(category);
            Notify_PointsUsed();
            Select(addedPawn);
        }));
    }
}
