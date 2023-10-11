using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_EquipmentLoot : TabWorker<Faction>
{
    private static UITable<Faction> equipmentTable;
    private static UITable<Faction> itemsTable;
    private static UITable<Faction> lootTable;
    private readonly Dictionary<Thing, string> countBuffer = new();

    public override void Initialize()
    {
        base.Initialize();
        equipmentTable = new(GetHeadings("Equipment".Translate()), _ => GetRows(StartingThingsManager.GetStartingThingsNear()));
        lootTable = new(GetHeadings("PawnEditor.ScatteredLoot".Translate()), _ => GetRows(StartingThingsManager.GetStartingThingsFar()));
        itemsTable = new(GetHeadings("ItemsTab".Translate()), _ => GetRows(ColonyInventory.AllItemsInInventory()));
    }

    private List<UITable<Faction>.Heading> GetHeadings(string heading) =>
        new()
        {
            new(heading),
            new("PawnEditor.Weight".Translate(), 80),
            new("PawnEditor.Hitpoints".Translate(), 80),
            new("MarketValueTip".Translate(), 120),
            new("PenFoodTab_Count".Translate(), 100),
            new(100),
            new(30)
        };

    private IEnumerable<UITable<Faction>.Row> GetRows(List<Thing> things)
    {
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            var items = new List<UITable<Faction>.Row.Item>
            {
                new(thing.LabelCapNoCount, Widgets.GetIconFor(thing, new(25, 25), Rot4.South, false, out _, out _, out _, out _), i),
                new(thing.GetStatValue(StatDefOf.Mass).ToStringMass(), (int)thing.GetStatValue(StatDefOf.Mass)),
                new(((float)thing.HitPoints / thing.MaxHitPoints).ToStringPercent(), thing.HitPoints / thing.MaxHitPoints),
                new(thing.MarketValue.ToStringMoney(), (int)thing.MarketValue),
                new(countRect =>
                {
                    var count = thing.stackCount;
                    countBuffer.TryGetValue(thing, out var buffer);
                    if (count > 1 && Widgets.ButtonImage(countRect.TakeLeftPart(25).ContractedBy(0, 5),
                            TexPawnEditor.ArrowLeftHalf))
                    {
                        count--;
                        buffer = null;
                    }

                    if (Widgets.ButtonImage(countRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightHalf))
                    {
                        count++;
                        buffer = null;
                    }

                    Widgets.TextFieldNumeric(countRect, ref count, ref buffer);
                    if (count != thing.stackCount)
                    {
                        thing.stackCount = count;
                        PawnEditor.Notify_PointsUsed();
                    }

                    countBuffer[thing] = buffer;
                }),
                new("Edit".Translate() + "...", () => { }),
                new(TexButton.DeleteX, () =>
                {
                    thing.Destroy();
                    thing.Discard(true);
                    PawnEditor.Notify_PointsUsed();
                    ClearCaches();
                })
            };

            yield return new(items, thing.GetTooltip().text);
        }
    }

    public static void ClearCaches()
    {
        equipmentTable.ClearCache();
        itemsTable.ClearCache();
        lootTable.ClearCache();
    }

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoBottomButtons(inRect.TakeBottomPart(40));

        if (PawnEditor.Pregame)
        {
            using (new TextBlock(GameFont.Tiny))
                Widgets.Label(inRect.TakeBottomPart(Text.LineHeight), "PawnEditor.EquipmentLootDesc".Translate().Colorize(ColoredText.SubtleGrayColor));
            equipmentTable.OnGUI(inRect.TopHalf(), faction);
            lootTable.OnGUI(inRect.BottomHalf(), faction);
        }
        else
            itemsTable.OnGUI(inRect, faction);
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Faction faction)
    {
        if (PawnEditor.Pregame)
        {
            yield return new SaveLoadItem<ThingList>("Equipment".Translate(), new(StartingThingsManager.GetStartingThingsNear(), "Equipment"),
                new()
                {
                    OnLoad = thingList =>
                    {
                        var list = StartingThingsManager.GetStartingThingsNear();
                        list.Clear();
                        list.AddRange(thingList.Things);
                    }
                });
            yield return new SaveLoadItem<ThingList>("ScatteredLoot".Translate(), new(StartingThingsManager.GetStartingThingsFar(), "Loot"),
                new()
                {
                    OnLoad = thingList =>
                    {
                        var list = StartingThingsManager.GetStartingThingsFar();
                        list.Clear();
                        list.AddRange(thingList.Things);
                    }
                });
        }
    }

    private static void DoBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5),
                "Add".Translate().CapitalizeFirst() + " " + "Equipment".Translate())) { }

        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5),
                "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.ScatteredLoot".Translate())) { }
    }

    private struct ThingList : IExposable, ISaveable
    {
        public List<Thing> Things;
        private string label;

        public ThingList(IEnumerable<Thing> things, string label = "Things")
        {
            Things = things.ToList();
            this.label = label;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Things, "things", LookMode.Deep);
            Scribe_Values.Look(ref label, nameof(label));
        }

        public string DefaultFileName() => label;
    }
}
