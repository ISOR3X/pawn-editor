using System.Collections.Generic;
using System.Linq;
using PawnEditor.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    private void DoGroups(Rect inRect, Pawn pawn)
    {
        Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.Groups".Translate().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 6;
        inRect.yMin += 4f;

        var faction = "Faction".Translate();
        var ideo = "DifficultyIdeologySection".Translate();
        var certainty = "Certainty".Translate().CapitalizeFirst();
        var title = "PawnEditor.EmpireTitle".Translate();
        var honor = "PawnEditor.Honor".Translate();
        var favColor = "PawnEditor.FavColor".Translate();
        var role = "Role".Translate().CapitalizeFirst();
        var leftWidth = UIUtility.ColumnWidth(3, faction, ideo, certainty, title, honor, favColor) + 4f;
        var factionRect = inRect.TakeTopPart(30);
        inRect.yMin += 4;
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(factionRect.TakeLeftPart(leftWidth), faction);

        //Creates the dropdown menu of the factions. On selection, it calls a Lambda Func. In this case it selects the faction.
        if (Widgets.ButtonText(factionRect, "PawnEditor.SelectFaction".Translate()))
        {
            //create dropdown options
            List<FloatMenuOption> options = PawnEditor_PawnsFinder.GetAllFactionsContainingAtLeastOneHumanLike().Select(newFaction =>
                    new FloatMenuOption(newFaction.Name, delegate
                    {
                        if (newFaction != pawn.Faction) pawn.SetFaction(newFaction);
                        PawnEditor.selectedFaction = newFaction;
                        PawnEditor.DoRecache();
                    }, newFaction.def.FactionIcon, newFaction.Color)).ToList();

            //Create a new option for each of the random faction selectors
            var randomAllyOption = new FloatMenuOption("PawnEditor.RandomAlly".Translate(), () =>
            {
                var randFac = Find.FactionManager.RandomAlliedFaction();
                if (randFac != pawn.Faction) pawn.SetFaction(randFac);
                PawnEditor.selectedFaction = randFac;
                PawnEditor.DoRecache();

            });
            randomAllyOption.Disabled = Find.FactionManager.RandomAlliedFaction() == null;
            options.Add(randomAllyOption);

            var randomEnemyFactionOption = new FloatMenuOption("PawnEditor.RandomEnemy".Translate(), () =>
            {

                var randFac = Find.FactionManager.RandomEnemyFaction();
                if (randFac != pawn.Faction) pawn.SetFaction(randFac);
                    PawnEditor.selectedFaction = randFac;
                PawnEditor.DoRecache();
            });
            randomEnemyFactionOption.Disabled = Find.FactionManager.RandomEnemyFaction() == null;
            options.Add(randomEnemyFactionOption);

            var randomNonHostileOption = new FloatMenuOption("PawnEditor.RandomNonHostile".Translate(), () =>
            {
                var randFac = Find.FactionManager.RandomNonHostileFaction();

                if (randFac != pawn.Faction) pawn.SetFaction(randFac);
                    PawnEditor.selectedFaction = randFac;
                PawnEditor.DoRecache();

            });
            randomNonHostileOption.Disabled = Find.FactionManager.RandomNonHostileFaction() == null;
            options.Add(randomNonHostileOption);

            var randomNonColonistOption = new FloatMenuOption("PawnEditor.RandomNonColonist".Translate(), () =>
            {

                List<Faction> factions = Find.FactionManager.AllFactionsVisibleInViewOrder.ToList();
                Faction randFac = factions[Random.Range(0, factions.Count - 1)];
                if (randFac != pawn.Faction) pawn.SetFaction(randFac);
                    PawnEditor.selectedFaction = randFac;
                PawnEditor.DoRecache();

            });
            randomNonColonistOption.Disabled = !(Find.FactionManager.RandomEnemyFaction() != null && Find.FactionManager.RandomNonHostileFaction() != null);
            options.Add(randomNonColonistOption);

            //All factions including colony
            var randomOption = new FloatMenuOption("PawnEditor.RandomFaction".Translate(), () =>
            {
                List<Faction> factions = Find.FactionManager.AllFactionsVisibleInViewOrder.ToList();
                factions.Add(Find.FactionManager.ofPlayer);
                var randFac = factions[Random.Range(0, factions.Count - 1)];
                pawn.SetFaction(randFac);
                PawnEditor.selectedFaction = randFac;
                PawnEditor.DoRecache();

            });
            randomOption.Disabled = (Find.FactionManager.AllFactionsVisibleInViewOrder.ToList().Count <= 1);
            options.Add(randomOption);

            //add option to faction to null 
            options.Add(new FloatMenuOption("PawnEditor.NoFaction".Translate(), () =>
            {
                pawn.SetFaction(null);
                PawnEditor.selectedFaction = null;
                PawnEditor.DoRecache();
            }));

            //add dropdown to window stack
            Find.WindowStack.Add(new FloatMenu(options));
        }

        factionRect = inRect.TakeTopPart(30);
        inRect.yMin += 4;
        factionRect.TakeLeftPart(leftWidth);

        //How to draw the faction label depending on mod options
        if (pawn.Faction != null)
        {
            if (PawnEditor.Pregame)
            {
                if (!PawnEditorMod.Settings.HideFactions)
                {
                    DrawFactionLabel(factionRect, pawn);
                }
                else
                {
                    if (pawn.Faction != Find.FactionManager.ofPlayer) 
                        Widgets.Label(factionRect, "PawnEditor.FactionHidden".Translate());
                    else 
                        DrawFactionLabel(factionRect, pawn);
                }

            }
            else 
                DrawFactionLabel(factionRect, pawn);
        }
        else 
            Widgets.Label(factionRect, "PawnEditor.NoFaction".Translate());
        
        inRect.yMin += 16;

        var empire = Faction.OfEmpire;
        if (ModsConfig.RoyaltyActive && empire != null)
        {
            var titleRect = inRect.TakeTopPart(30);
            inRect.yMin += 10;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(titleRect.TakeLeftPart(leftWidth), title);
            var curTitle = pawn.royalty.GetCurrentTitle(empire);
            if (Widgets.ButtonText(titleRect, curTitle?.GetLabelCapFor(pawn) ?? "None".Translate()))
            {
                var list = new List<FloatMenuOption>
                {
                    new("None".Translate(), () => { pawn.royalty.SetTitle(empire, null, false, false, false); })
                };
                list.AddRange(empire.def.RoyalTitlesAllInSeniorityOrderForReading.Select(royalTitle =>
                    new FloatMenuOption(royalTitle.GetLabelCapFor(pawn), () => pawn.royalty.SetTitle(empire, royalTitle, true, false, false))));
                Find.WindowStack.Add(new FloatMenu(list));
            }

            if (curTitle?.GetNextTitle(empire) != null)
            {
                var honorRect = inRect.TakeTopPart(30);
                inRect.yMin += 4;
                using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(honorRect.TakeLeftPart(leftWidth), honor);
                float favor = pawn.royalty.GetFavor(empire);
                honorRect.yMax -= Mathf.Round((honorRect.height - 10f) / 2f);
                Widgets.HorizontalSlider(honorRect, ref favor, new(0, curTitle.GetNextTitle(empire).favorCost - 1), favor.ToString());
                pawn.royalty.SetFavor(empire, (int)favor, false);
            }

            inRect.yMin += 16;
        }

        if (ModsConfig.IdeologyActive && pawn.Ideo != null)
        {
            var ideoRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(ideoRect.TakeLeftPart(leftWidth), ideo);
            if (Widgets.ButtonText(ideoRect, "PawnEditor.SelectIdeo".Translate()))
                Find.WindowStack.Add(new FloatMenu(Find.IdeoManager.IdeosInViewOrder.Select(newIdeo =>
                        new FloatMenuOption(newIdeo.name, delegate { pawn.ideo.SetIdeo(newIdeo); }, newIdeo.Icon, newIdeo.Color))
                   .ToList()));

            ideoRect = inRect.TakeTopPart(30);
            inRect.yMin += 10f;
            ideoRect.TakeLeftPart(leftWidth);
            Widgets.DrawHighlight(ideoRect);
            Widgets.DrawHighlightIfMouseover(ideoRect);
            if (Widgets.ButtonInvisible(ideoRect)) IdeoUIUtility.OpenIdeoInfo(pawn.Ideo);

            GUI.color = pawn.Ideo.Color;
            GUI.DrawTexture(ideoRect.TakeLeftPart(30).ContractedBy(6), pawn.Ideo.Icon);
            GUI.color = Color.white;
            using (new TextBlock(TextAnchor.MiddleLeft))
                Widgets.Label(ideoRect, pawn.Ideo.name.Truncate(ideoRect.width, truncateCache));

            var certaintyRect = inRect.TakeTopPart(30);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(certaintyRect.TakeLeftPart(leftWidth), certainty);
            certaintyRect.yMax -= Mathf.Round((certaintyRect.height - 10f) / 2f);
            pawn.ideo.certaintyInt = Widgets.HorizontalSlider(certaintyRect, pawn.ideo.certaintyInt, 0, 1, true, pawn.ideo.Certainty.ToStringPercent(),
                "0%", "100%");


            var roleRect = inRect.TakeTopPart(30);
            inRect.yMin += 4;
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(roleRect.TakeLeftPart(leftWidth), role);
            var curTitle = pawn.Ideo.GetRole(pawn);
            if (Widgets.ButtonText(roleRect, curTitle?.LabelForPawn(pawn) ?? "None".Translate()))
            {
                var list = new List<FloatMenuOption>
                {
                    // ToDo: Removing roles has some odd behaviour where it sometimes changes the role to a different one instead of removing it.
                    new("None".Translate(), () => { curTitle?.Unassign(pawn, false); })
                };
                list.AddRange(pawn.Ideo.cachedPossibleRoles.Select(ideoRole =>
                    new FloatMenuOption(ideoRole.LabelForPawn(pawn),
                    delegate
                    {
                        if (curTitle != ideoRole)
                        {
                            curTitle?.Unassign(pawn, false);
                            ideoRole.Assign(pawn, true);
                        }
                    }
                    )));
                Find.WindowStack.Add(new FloatMenu(list));
            }

            inRect.yMin += 16;
        }
    }

    void DrawFactionLabel(Rect factionRect, Pawn pawn)
    {
        Widgets.DrawHighlight(factionRect);
        Widgets.DrawHighlightIfMouseover(factionRect);
        if (Widgets.ButtonInvisible(factionRect)) Find.WindowStack.Add(new Dialog_InfoCard(pawn.Faction));

        GUI.color = pawn.Faction.Color;
        GUI.DrawTexture(factionRect.TakeLeftPart(30).ContractedBy(6), pawn.Faction.def.FactionIcon);
        GUI.color = Color.white;
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(factionRect, pawn.Faction.Name);
    }
}
