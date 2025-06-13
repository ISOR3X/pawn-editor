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
    private static TabWorker_AnimalMech instance;
    private readonly PawnListerBase animalList = new();
    private readonly PawnListerBase mechList = new();
    private int animalCount;

    private Vector2 animalScrollPos;
    private UITable<Faction> animalTable;
    private int mechCount;
    private UITable<Faction> mechTable;

    public TabWorker_AnimalMech() => instance = this;

    public override void Initialize()
    {
        base.Initialize();
        animalTable = new(new()
        {
            new(35), // Icon
            new("PawnEditor.Name".Translate(), textAnchor: TextAnchor.LowerLeft),
            new("Stat_Thing_Apparel_ValidLifestage".Translate(), 100),
            new(TexPawnEditor.GendersTex, 100),
            new("PawnEditor.Age".Translate(), 100),
            new("MarketValueTip".Translate(), 100),
            // new(16), // Spacing
            // new("AnimalBonded".Translate(), 120),
            // new(16), // Spacing
            new(24), // Paste
            new(24), // Copy
            new(24), // Go to
            new(24), // Save
            new(24) // Delete
        }, GetAnimalRows);
        mechTable = new(new()
        {
            new(35), // Icon
            new("PawnEditor.Mechs".Translate().CapitalizeFirst()),
            new("MarketValueTip".Translate(), 100),
            // new(16), // Spacing
            // new("Overseer".Translate(), 120),
            // new(16), // Spacing
            new(24), // Paste
            new(24), // Copy
            new(24), // Go to
            new(24), // Save
            new(24) // Delete
        }, GetMechRows);
    }

    private IEnumerable<UITable<Faction>.Row> GetAnimalRows(Faction faction)
    {
        List<Pawn> pawns;
        if (PawnEditor.Pregame) pawns = StartingThingsManager.GetPawns(PawnCategory.Animals);
        else
        {
            animalList.UpdateCache(faction, PawnCategory.Animals);
            pawns = animalList.GetList();
        }

        animalCount = 0;

        for (var i = 0; i < pawns.Count; i++)
        {
            var pawn = pawns[i];
            if (!searchWidget.filter.Matches(pawn.Name.ToStringFull)) continue;
            animalCount++;
            var items = new List<UITable<Faction>.Row.Item>
            {
                new(Widgets.GetIconFor(pawn, Vector2.one, null, false, out _, out _, out _, out _, out _)),
                new(pawn.Name.ToStringShort, pawn.Name.ToStringShort.ToCharArray()[0], TextAnchor.MiddleLeft),
                new(pawn.ageTracker.CurLifeStageRace.GetIcon(pawn), pawn.ageTracker.CurLifeStageIndex),
                new(pawn.gender.GetIcon(), (int)pawn.gender),
                new(pawn.ageTracker.AgeNumberString, pawn.ageTracker.AgeBiologicalYears),
                new(pawn.MarketValue.ToStringMoney(), (int)pawn.MarketValue),
                // new(),
                // new(TrainableUtility.GetAllColonistBondsFor(pawn).FirstOrDefault()?.Name?.ToStringShort ?? "None".Translate(), () =>
                // {
                //     var possiblePawns = pawn.MapHeld == null
                //         ? Find.WorldPawns.AllPawnsAlive.Where(p => p.IsColonistPlayerControlled)
                //         : pawn.MapHeld.mapPawns.FreeColonists;
                //     Find.WindowStack.Add(new FloatMenu(possiblePawns.Select(p => new FloatMenuOption(p.Name.ToStringShort, () =>
                //         {
                //             foreach (var bondmate in TrainableUtility.GetAllColonistBondsFor(pawn).ToList())
                //                 pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Bond, bondmate);
                //             pawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, p);
                //         }))
                //         .ToList()));
                // }),
                // new(),
                new(TexButton.Paste, () =>
                {
                    PawnEditor.Paste(pawn);
                    mechTable.ClearCache();
                }, () => PawnEditor.CanPaste),
                new(TexButton.Copy, () => PawnEditor.Copy(pawn)),
                new(TexPawnEditor.GoToPawn, () => { PawnEditor.Select(pawn); }),
                new(TexPawnEditor.Save,
                    () => { SaveLoadUtility.SaveItem(pawn, typePostfix: PawnCategory.Animals.ToString()); }),
                new(TexButton.Delete, () =>
                {
                    Find.WindowStack.Add(new Dialog_Confirm("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored), "ConfirmDeleteAnimal",
                        () =>
                        {
                            pawn.Discard(true);
                            pawns.Remove(pawn);
                            PawnEditor.Notify_PointsUsed();
                            animalTable.ClearCache();
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
            pawns = mechList.GetList();
        }

        mechCount = 0;

        for (var i = 0; i < pawns.Count; i++)
        {
            var pawn = pawns[i];
            if (!searchWidget.filter.Matches(pawn.Name.ToStringFull)) continue;
            mechCount++;
            var items = new List<UITable<Faction>.Row.Item>
            {
                new(Widgets.GetIconFor(pawn, Vector2.one, null, false, out _, out _, out _, out _, out _)),
                new(pawn.Name.ToStringShort, pawn.Name.ToStringShort.ToCharArray()[0], TextAnchor.MiddleLeft),
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
                new(TexButton.Paste, () =>
                {
                    PawnEditor.Paste(pawn);
                    mechTable.ClearCache();
                }, () => PawnEditor.CanPaste),
                new(TexButton.Copy, () => PawnEditor.Copy(pawn)),
                new(TexPawnEditor.GoToPawn, () => { PawnEditor.Select(pawn); }),
                new(TexPawnEditor.Save,
                    () => { SaveLoadUtility.SaveItem(pawn, typePostfix: PawnCategory.Mechs.ToString()); }),
                new(TexButton.Delete, () =>
                {
                    Find.WindowStack.Add(new Dialog_Confirm("PawnEditor.ReallyDelete".Translate(pawn.NameShortColored), "ConfirmDeleteMech",
                        () =>
                        {
                            pawn.Discard(true);
                            pawns.Remove(pawn);
                            PawnEditor.Notify_PointsUsed();
                            mechTable.ClearCache();
                        }, true));
                })
            };
            yield return new(items, pawn.GetTooltip().text);
        }
    }

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoBottomButtons(inRect.TakeBottomPart(UIUtility.RegularButtonHeight));

        var viewRect = new Rect(0, 0, inRect.width - 20,
            UITable<Faction>.Heading.Height + (animalCount + mechCount) * 34 + (Text.LineHeightOf(GameFont.Small) + 4) * 2 + 32f);
        Widgets.BeginScrollView(inRect, ref animalScrollPos, viewRect);
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "AnimalsSection".Translate().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        animalTable.OnGUI(viewRect.TakeTopPart(animalCount * 34), faction);
        viewRect.xMin += -4f;
        viewRect.yMin += 32f;
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "PawnEditor.MechsSection".Translate().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        mechTable.OnGUI(viewRect.TakeTopPart(mechCount * 34), faction);
        Widgets.EndScrollView();
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Faction faction)
    {
        if (PawnEditor.Pregame)
        {
            yield return new SaveLoadItem<PawnList>("AnimalsSection".Translate(), new(
                StartingThingsManager.GetPawns(PawnCategory.Animals), PawnCategory.Animals), new()
            {
                OnLoad = pawnList =>
                {
                    var list = StartingThingsManager.GetPawns(PawnCategory.Animals);
                    list.Clear();
                    list.AddRange(pawnList.Pawns);
                    animalTable.ClearCache();
                },
                TypePostfix = PawnCategory.Animals.ToString()
            });

            yield return new SaveLoadItem<PawnList>("PawnEditor.Mechs".Translate(), new(
                StartingThingsManager.GetPawns(PawnCategory.Mechs), PawnCategory.Mechs), new()
            {
                OnLoad = pawnList =>
                {
                    var list = StartingThingsManager.GetPawns(PawnCategory.Mechs);
                    list.Clear();
                    list.AddRange(pawnList.Pawns);
                    mechTable.ClearCache();
                },
                TypePostfix = PawnCategory.Mechs.ToString()
            });
        }
    }

    public static void Notify_PawnAdded(PawnCategory category)
    {
        if (category == PawnCategory.Animals) instance.animalTable.ClearCache();
        if (category == PawnCategory.Mechs) instance.mechTable.ClearCache();
    }

    private void DoBottomButtons(Rect inRect)
    {
        if (UIUtility.DefaultButtonText(ref inRect,
                "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.PawnCategory.Animals".Translate()))
            PawnEditor.AddPawn(PawnCategory.Animals);
        inRect.xMin += 4f;
        if (UIUtility.DefaultButtonText(ref inRect,
                "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.PawnCategory.Mechs".Translate())) PawnEditor.AddPawn(PawnCategory.Mechs);
        inRect.xMin += 4f;
        searchWidget.OnGUI(inRect.TakeRightPart(250), () =>
        {
            animalTable.ClearCache();
            mechTable.ClearCache();
        });
    }

    private struct PawnList : IExposable, ISaveable
    {
        public List<Pawn> Pawns;
        private PawnCategory category;

        public PawnList(IEnumerable<Pawn> pawns, PawnCategory category)
        {
            Pawns = pawns.ToList();
            this.category = category;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Pawns, "pawns", LookMode.Deep);
            Scribe_Values.Look(ref category, nameof(category));
        }

        public string DefaultFileName() => category.ToString();
    }
}
