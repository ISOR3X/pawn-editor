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

        if (selectedFaction == null || pregame) selectedFaction = Faction.OfPlayer;
        if (!pregame && Widgets.ButtonText(inRect.TakeTopPart(30f), "PawnEditor.SelectFaction".Translate()))
            Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsVisibleInViewOrder.Select(faction =>
                    new FloatMenuOption(faction.Name, () => selectedFaction = faction, faction.def.FactionIcon, faction.color ?? Color.white))
               .ToList()));

        var factionRect = inRect.TakeTopPart(68f).ContractedBy(4f);
        Widgets.DrawOptionBackground(factionRect, showFactionInfo);
        MouseoverSounds.DoRegion(factionRect);
        var color = selectedFaction.Color;
        color.a = 0.2f;
        GUI.color = color;
        GUI.DrawTexture(factionRect.ContractedBy(6).RightPart(0.2f).BottomPart(0.75f), selectedFaction.def.FactionIcon);
        GUI.color = Color.white;
        using (new TextBlock(GameFont.Small))
            Widgets.Label(factionRect.ContractedBy(2f).TopPart(0.5f).Rounded(), selectedFaction.Name);
        if (Widgets.ButtonInvisible(factionRect)) showFactionInfo = !showFactionInfo;

        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.ClickFactionOverview".Translate().Colorize(ColoredText.SubtleGrayColor));

        inRect.yMin += 4f;

        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.SelectedCategory".Translate());

        if (Widgets.ButtonText(inRect.TakeTopPart(30f), selectedCategory.LabelCapPlural()))
            Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(PawnCategory))
               .Cast<PawnCategory>()
               .Select(category =>
                    new FloatMenuOption(category.LabelCapPlural(), () => selectedCategory = category))
               .ToList()));

        if (Widgets.ButtonText(inRect.TakeTopPart(25f), "Add".Translate().CapitalizeFirst()))
        {
            void AddPawn(Pawn addedPawn)
            {
                if (pregame)
                    Find.GameInitData.startingAndOptionalPawns.Add(addedPawn);
                else
                    Find.WorldPawns.PassToWorld(addedPawn, PawnDiscardDecideMode.KeepForever);
            }

            void AddPawnKind(PawnKindDef pawnKind)
            {
                AddPawn(PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, selectedFaction,
                    pregame ? PawnGenerationContext.PlayerStarter : selectedFaction.IsPlayer ? PawnGenerationContext.All : PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true)));
            }

            var list = new List<FloatMenuOption>
            {
                new("PawnEditor.Add.Saved".Translate(selectedCategory.Label()), delegate { }),
                new("PawnEditor.Add.Random".Translate(selectedCategory.Label(), 1000), () => AddPawnKind(selectedFaction.RandomPawnKind())),
                new("PawnEditor.Add.Random".Translate(selectedCategory.Label(), 10000), () => AddPawnKind(selectedFaction.RandomPawnKind()))
            };

            if (selectedCategory is PawnCategory.Colonists or PawnCategory.Humans)
                list.Insert(0, new FloatMenuOption("PawnEditor.Add.PawnKind".Translate(), delegate
                {
                    Find.WindowStack.Add(new FloatMenu(DefDatabase<PawnKindDef>.AllDefs.Where(pk => pk.RaceProps.Humanlike)
                       .Select(pk => new FloatMenuOption(pk.LabelCap, () => AddPawnKind(pk)))
                       .ToList()));
                }));

            list.Add(new FloatMenuOption("PawnEditor.Add.OtherSave".Translate(), delegate { }));

            Find.WindowStack.Add(new FloatMenu(list));
        }

        List<Pawn> pawns;
        List<string> sections;
        Action<Pawn, int> onReorder;
        Action<Pawn> onDelete;
        if (pregame)
        {
            pawns = Find.GameInitData.startingAndOptionalPawns;
            sections = Enumerable.Repeat<string>(null, pawns.Count).ToList();
            sections[0] = "StartingPawnsSelected".Translate();
            sections[Find.GameInitData.startingPawnCount] = "StartingPawnsLeftBehind".Translate();
            onReorder = delegate(Pawn item, int to)
            {
                var from = Find.GameInitData.startingAndOptionalPawns.IndexOf(item);
                StartingPawnUtility.ReorderRequests(from, to);
                TutorSystem.Notify_Event("ReorderPawn");
                if (to < Find.GameInitData.startingPawnCount && from >= Find.GameInitData.startingPawnCount)
                    TutorSystem.Notify_Event("ReorderPawnOptionalToStarting");
            };
            onDelete = pawn => pawn.Discard(true);
        }
        else
        {
            pawns = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists;
            sections = Enumerable.Repeat<string>(null, pawns.Count).ToList();
            onReorder = (pawn, to) => { };
            onDelete = pawn => pawn.Destroy();
        }

        DoPawnList(inRect, pawns, sections, onReorder, onDelete);
    }
}
