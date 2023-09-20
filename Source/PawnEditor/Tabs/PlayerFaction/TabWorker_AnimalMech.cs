using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_AnimalMech : TabWorker<Faction>
{
    private static readonly QuickSearchWidget searchWidget = new();
    private readonly PawnLister animalList = new();
    private readonly PawnLister mechList = new();
    private int animalCount;

    private Vector2 animalScrollPos;
    private UITable<Faction> animalTable;
    private int mechCount;
    private Vector2 mechScrollPos;
    private UITable<Faction> mechTable;

    public override void Initialize()
    {
        base.Initialize();
        animalTable = new(new()
        {
            new("AnimalsSection".Translate()),
            new(TexPawnEditor.GendersTex),
            new("PawnEditor.Age".Translate()),
            new("StatsReport_LifeStage".Translate()),
            new("MarketValueTip".Translate()),
            new("AnimalBonded".Translate()),
            new(100),
            new(30),
            new(30),
            new(30),
            new(30)
        }, GetAnimalRows);
        mechTable = new(new()
        {
            new("MechsSection".Translate()),
            new("MarketValueTip".Translate()),
            new("Overseer".Translate()),
            new(100),
            new(30),
            new(30),
            new(30),
            new(30)
        }, GetMechRows);
    }

    private IEnumerable<UITable<Faction>.Row> GetAnimalRows(Faction faction)
    {
        List<Pawn> pawns;
        if (PawnEditor.Pregame) pawns = StartingThingsManager.GetPawns(PawnCategory.Animals);
        else
        {
            animalList.UpdateCache(faction, PawnCategory.Animals);
            (pawns, _, _) = animalList.GetLists();
        }

        animalCount = 0;

        for (var i = 0; i < pawns.Count; i++)
        {
            var pawn = pawns[i];
            if (!searchWidget.filter.Matches(pawn.Name.ToStringFull)) continue;
            animalCount++;
            var items = new List<UITable<Faction>.Row.Item>
            {
                new(pawn.Name.ToStringShort, i),
                new(pawn.gender.GetIcon(), (int)pawn.gender),
                new(pawn.ageTracker.AgeNumberString, pawn.ageTracker.AgeBiologicalYears),
                new(pawn.ageTracker.CurLifeStage.iconTex, pawn.ageTracker.CurLifeStageIndex),
                new(pawn.MarketValue.ToStringMoney(), (int)pawn.MarketValue),
                new(TrainableUtility.GetAllColonistBondsFor(pawn).FirstOrDefault()?.Name?.ToStringShort ?? "None".Translate(), () =>
                {
                    var possiblePawns = pawn.MapHeld == null
                        ? Find.WorldPawns.AllPawnsAlive.Where(p => p.IsColonistPlayerControlled)
                        : pawn.MapHeld.mapPawns.FreeColonists;
                    Find.WindowStack.Add(new FloatMenu(possiblePawns.Select(p => new FloatMenuOption(p.Name.ToStringShort, () =>
                        {
                            foreach (var bondmate in TrainableUtility.GetAllColonistBondsFor(pawn).ToList())
                                pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Bond, bondmate);
                            pawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, p);
                        }))
                       .ToList()));
                }),
                new("Edit".Translate() + "...", () => PawnEditor.Select(pawn)),
                new(TexButton.Save, () => { SaveLoadUtility.SaveItem(pawn); }),
                new(TexButton.Copy, () => { }),
                new(TexButton.Paste, () => { }),
                new(TexButton.DeleteX, () =>
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored),
                        () =>
                        {
                            pawn.Discard(true);
                            pawns.Remove(pawn);
                        }, true));
                })
            };
            yield return new(items, pawn.GetTooltip().text);
        }
    }

    private IEnumerable<UITable<Faction>.Row> GetMechRows(Faction faction)
    {
        List<Pawn> pawns;
        if (PawnEditor.Pregame) pawns = StartingThingsManager.GetPawns(PawnCategory.Mechs);
        else
        {
            mechList.UpdateCache(faction, PawnCategory.Mechs);
            (pawns, _, _) = mechList.GetLists();
        }

        mechCount = 0;

        for (var i = 0; i < pawns.Count; i++)
        {
            var pawn = pawns[i];
            if (!searchWidget.filter.Matches(pawn.Name.ToStringFull)) continue;
            mechCount++;
            var items = new List<UITable<Faction>.Row.Item>
            {
                new(pawn.Name.ToStringShort, i),
                new(pawn.MarketValue.ToStringMoney(), (int)pawn.MarketValue),
                new(pawn.GetOverseer()?.Name?.ToStringShort ?? "OverseerNone".Translate(), () =>
                {
                    var possiblePawns = (pawn.MapHeld == null
                        ? Find.WorldPawns.AllPawnsAlive.Where(p => p.IsColonistPlayerControlled)
                        : pawn.MapHeld.mapPawns.FreeColonists).Where(p => MechanitorUtility.CanControlMech(p, pawn));
                    Find.WindowStack.Add(new FloatMenu(possiblePawns.Select(p => new FloatMenuOption(p.Name.ToStringShort, () =>
                        {
                            var old = pawn.GetOverseer();
                            if (old != null) pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, old);
                            pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, p);
                        }))
                       .ToList()));
                }),
                new("Edit".Translate() + "...", () => PawnEditor.Select(pawn)),
                new(TexButton.Save, () => { SaveLoadUtility.SaveItem(pawn); }),
                new(TexButton.Copy, () => { }),
                new(TexButton.Paste, () => { }),
                new(TexButton.DeleteX, () =>
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored),
                        () =>
                        {
                            pawn.Discard(true);
                            pawns.Remove(pawn);
                        }, true));
                })
            };
            yield return new(items, pawn.GetTooltip().text);
        }
    }

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoBottomButtons(inRect.TakeBottomPart(40));

        var viewRect = new Rect(0, 0, inRect.width - 20, UITable<Faction>.Heading.Height + animalCount * 34);
        Widgets.BeginScrollView(inRect.TopHalf(), ref animalScrollPos, viewRect);
        animalTable.OnGUI(viewRect, faction);
        Widgets.EndScrollView();

        viewRect = new(0, 0, inRect.width - 20, UITable<Faction>.Heading.Height + mechCount * 34);
        Widgets.BeginScrollView(inRect.BottomHalf(), ref mechScrollPos, viewRect);
        mechTable.OnGUI(viewRect, faction);
        Widgets.EndScrollView();
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Faction faction)
    {
        if (PawnEditor.Pregame)
        {
            yield return new SaveLoadItem<PawnList>("AnimalsSection".Translate(), new(
                StartingThingsManager.GetPawns(PawnCategory.Animals)), new()
            {
                OnLoad = pawnList =>
                {
                    var list = StartingThingsManager.GetPawns(PawnCategory.Animals);
                    list.Clear();
                    list.AddRange(pawnList.Pawns);
                }
            });

            yield return new SaveLoadItem<PawnList>("MechsSection".Translate(), new(
                StartingThingsManager.GetPawns(PawnCategory.Mechs)), new()
            {
                OnLoad = pawnList =>
                {
                    var list = StartingThingsManager.GetPawns(PawnCategory.Mechs);
                    list.Clear();
                    list.AddRange(pawnList.Pawns);
                }
            });
        }
    }

    private void DoBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5),
                "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.PawnCategory.Animals".Translate())) PawnEditor.AddPawn(PawnCategory.Animals);
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5),
                "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.PawnCategory.Mechs".Translate())) PawnEditor.AddPawn(PawnCategory.Mechs);
        searchWidget.OnGUI(inRect.TakeRightPart(250).ContractedBy(5), () =>
        {
            animalTable.ClearCache();
            mechTable.ClearCache();
        });
    }

    private struct PawnList : IExposable
    {
        public List<Pawn> Pawns;

        public PawnList(IEnumerable<Pawn> pawns) => Pawns = pawns.ToList();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Pawns, "pawns", LookMode.Deep);
        }
    }
}
