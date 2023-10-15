using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Gear : TabWorker<Pawn>
{
    private Vector2 oldScrollPos = Vector2.zero;
    private Vector2 scrollPos;

    private static UITable<Pawn> apparelTable;
    private static UITable<Pawn> equipmentTable;
    private static UITable<Pawn> possessionsTable;

    public override void Initialize()
    {
        base.Initialize();
        apparelTable = new(GetHeadings(), p => GetRows(p, pawn => pawn.apparel.WornApparel.Cast<Thing>().ToList()));
        equipmentTable = new(GetHeadings(), p => GetRows(p, pawn => pawn.equipment.equipment.Cast<Thing>().ToList()));
        possessionsTable = new(GetHeadings(), p => GetRows(p, pawn => pawn.inventory.innerContainer.ToList()));
    }

    private List<UITable<Pawn>.Heading> GetHeadings() =>
        new()
        {
            new(32),
            new("Name".Translate(), textAnchor: TextAnchor.MiddleLeft),
            new("PawnEditor.Weight".Translate(), 120),
            new("PawnEditor.Hitpoints".Translate(), 120),
            new("MarketValueTip".Translate(), 120),
            new(16),
            new(100), // Edit
            new(24) // Delete
        };

    private IEnumerable<UITable<Pawn>.Row> GetRows(Pawn pawn, Func<Pawn, List<Thing>> thingsGetter)
    {
        var things = thingsGetter(pawn);
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            var items = new List<UITable<Pawn>.Row.Item>
            {
                new(Widgets.GetIconFor(thing, new(25, 25), Rot4.South, false, out _, out _, out _, out _), i),
                new(thing.LabelCap, textAnchor: TextAnchor.MiddleLeft),
                new(thing.GetStatValue(StatDefOf.Mass).ToStringMass().Colorize(ColoredText.SubtleGrayColor), (int)thing.GetStatValue(StatDefOf.Mass)),
                new(((float)thing.HitPoints / thing.MaxHitPoints).ToStringPercent().Colorize(ColoredText.SubtleGrayColor), thing.HitPoints / thing.MaxHitPoints),
                new(thing.MarketValue.ToStringMoney().Colorize(ColoredText.SubtleGrayColor), (int)thing.MarketValue),
                new(),
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

    private static void ClearCaches()
    {
        apparelTable.ClearCache();
        equipmentTable.ClearCache();
        possessionsTable.ClearCache();
    }

    private void DoTables(Rect inRect, Pawn pawn)
    {
        var apparelHeight = apparelTable.Height;
        var equipmentHeight = equipmentTable.Height;
        var possessionsHeight = possessionsTable.Height;
        var totalHeight = apparelHeight + equipmentHeight + possessionsHeight + 32f * 3f;

        var viewRect = new Rect(0, 0, inRect.width - 20f, totalHeight);

        Widgets.BeginScrollView(viewRect, ref scrollPos, viewRect);
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "Apparel".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        apparelTable.OnGUI(viewRect.TakeTopPart(apparelTable.Height), pawn);
        viewRect.xMin += -4f;
        viewRect.yMin += 32f;

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "Equipment".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        equipmentTable.OnGUI(viewRect.TakeTopPart(equipmentTable.Height), pawn);
        viewRect.xMin += -4f;
        viewRect.yMin += 32f;

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "Possessions".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        possessionsTable.OnGUI(viewRect.TakeTopPart(possessionsTable.Height), pawn);
        viewRect.xMin += -4f;
        viewRect.yMin += 32f;

        Widgets.EndScrollView();
    }


    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);

        headerRect.xMin += 7;
        DrawEquipmentInfo(headerRect, pawn);
        DoBottomOptions(rect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);

        rect.xMin += 4;

        DoTables(rect, pawn);

        // Close Dialog_EditItem on any interaction with the Dialog_PawnEditor menu.
        if (Find.WindowStack.IsOpen<Dialog_EditItem>())
            if (oldScrollPos != scrollPos || Find.WindowStack.focusedWindow is Dialog_PawnEditor)
                Find.WindowStack.TryRemove(typeof(Dialog_EditItem));

        oldScrollPos = scrollPos;
    }

    private void DrawEquipmentInfo(Rect inRect, Pawn pawn)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ColumnWidth -= listing.ColumnWidth / 2 + 16f;

        listing.ListSeparator("TabBasics".Translate());
        listing.Label("MassCarried".Translate(MassUtility.GearAndInventoryMass(pawn).ToString("0.##"),
            MassUtility.Capacity(pawn).ToString("0.##")));
        listing.Label("ComfyTemperatureRange".Translate() + ": " +
                      pawn.GetStatValue(StatDefOf.ComfyTemperatureMin).ToStringTemperature("F0") + " ~ "
                      + pawn.GetStatValue(StatDefOf.ComfyTemperatureMax).ToStringTemperature("F0"));
        listing.Label("MarketValueTip".Translate() + ": $" + pawn.GetStatValue(StatDefOf.MarketValue));
        listing.NewColumn();

        listing.ListSeparator("OverallArmor".Translate());
        DrawOverallArmor(listing, pawn, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate());
        DrawOverallArmor(listing, pawn, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate());
        DrawOverallArmor(listing, pawn, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate());
        listing.End();
    }

    private static void DrawOverallArmor(Listing_Standard listing, Pawn pawn, StatDef stat, string label)
    {
        var num = 0f;
        var num2 = Mathf.Clamp01(pawn.GetStatValue(stat) / 2f);
        var allParts = pawn.RaceProps.body.AllParts;
        var wornApparel = pawn.apparel?.WornApparel;
        foreach (var part in allParts)
        {
            var num3 = 1f - num2;
            if (wornApparel != null)
                foreach (var apparel in wornApparel)
                    if (apparel.def.apparel.CoversBodyPart(part))
                    {
                        var num4 = Mathf.Clamp01(apparel.GetStatValue(stat) / 2f);
                        num3 *= 1f - num4;
                    }

            num += part.coverageAbs * (1f - num3);
        }

        num = Mathf.Clamp(num * 2f, 0f, 2f);
        listing.LabelDouble(label.Truncate(120), num.ToStringPercent());
    }

    private void DoBottomOptions(Rect inRect, Pawn pawn)
    {
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.QuickActions".Translate(), 80f))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("PawnEditor.RepairAll".Translate(), () =>
                {
                    pawn.apparel.WornApparel.ForEach(a =>
                        {
                            a.HitPoints = a.MaxHitPoints;
                            a.wornByCorpseInt = false;
                        }
                    );
                    pawn.equipment.AllEquipmentListForReading.ForEach(e => e.HitPoints = e.MaxHitPoints);
                    foreach (var thing in pawn.inventory.innerContainer) thing.HitPoints = thing.MaxHitPoints;
                    PawnEditor.Notify_PointsUsed();
                }),
                new("PawnEditor.SetAllTo".Translate("Apparel".Translate().ToLower(), "PawnEditor.FavColor".Translate().ToLower()), () =>
                {
                    pawn.apparel.WornApparel.ForEach(a =>
                        {
                            if (a.TryGetComp<CompColorable>() != null)
                            {
                                if (pawn.story.favoriteColor != null)
                                    a.SetColor((Color)pawn.story.favoriteColor);
                                else
                                    Messages.Message("No favourite color found for pawn", MessageTypeDefOf.RejectInput);
                            }
                        }
                    );
                })
            }));

        inRect.xMin += 4f;

        if (UIUtility.DefaultButtonText(ref inRect, "Add".Translate().CapitalizeFirst() + " " + "Apparel".Translate().ToLower()))
        {
            IEnumerable<Thing> curApparel = pawn.apparel.WornApparel;
            Find.WindowStack.Add(new ListingMenu_Items(ListingMenu_Items.ItemType.Apparel, pawn,
                ThingCategoryNodeDatabase.allThingCategoryNodes.FirstOrDefault(tc => tc.catDef == ThingCategoryDefOf.Apparel)));
        }

        inRect.xMin += 4f;

        if (UIUtility.DefaultButtonText(ref inRect, "Add".Translate().CapitalizeFirst() + " " + "Equipment".Translate().ToLower()))
        {
            IEnumerable<Thing> curEquipment = pawn.equipment.AllEquipmentListForReading;
            Find.WindowStack.Add(new ListingMenu_Items(ListingMenu_Items.ItemType.Equipment, pawn,
                ThingCategoryNodeDatabase.RootNode));
        }

        inRect.xMin += 4f;

        if (UIUtility.DefaultButtonText(ref inRect, "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.Possession".Translate()))
        {
            IEnumerable<Thing> curPossessions = pawn.inventory.innerContainer;
            Find.WindowStack.Add(new ListingMenu_Items(ListingMenu_Items.ItemType.Possessions, pawn,
                ThingCategoryNodeDatabase.RootNode));
        }

        inRect.xMin += 4f;
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn)
    {
        yield return new SaveLoadItem<Pawn_ApparelTracker>("Apparel".Translate(), pawn.apparel);
        yield return new SaveLoadItem<Pawn_EquipmentTracker>("Equipment".Translate(), pawn.equipment);
        yield return new SaveLoadItem<Pawn_InventoryTracker>("Possessions".Translate(), pawn.inventory);
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn)
    {
        yield return new("Apparel".Translate(), () =>
        {
            pawn.apparel.DestroyAll();
            var apparelCandidates = PawnApparelGenerator.allApparelPairs.ListFullCopy();
            PawnApparelGenerator.workingSet.Reset(pawn);
            PawnApparelGenerator.usableApparel.Clear();
            PawnApparelGenerator.usableApparel.AddRange(apparelCandidates.Where(apparel =>
                apparel.thing.apparel.PawnCanWear(pawn) &&
                !PawnApparelGenerator.workingSet.PairOverlapsAnything(apparel)));

            ThingStuffPair workingPair;

            while (Rand.Value >= PawnApparelGenerator.workingSet.Count / 10f
                   && PawnApparelGenerator.usableApparel.TryRandomElementByWeight(pa => pa.Commonality,
                       out workingPair))
            {
                PawnApparelGenerator.workingSet.Add(workingPair);
                for (var k = PawnApparelGenerator.usableApparel.Count - 1; k >= 0; k--)
                    if (PawnApparelGenerator.workingSet.PairOverlapsAnything(PawnApparelGenerator.usableApparel[k]))
                        PawnApparelGenerator.usableApparel.RemoveAt(k);
            }
        });
        yield return new("Apparel".Translate() + " " + "PawnEditor.FromKind".Translate(), () =>
        {
            PawnApparelGenerator.GenerateStartingApparelFor(
                pawn, new(pawn.kindDef, pawn.Faction));
        });
        yield return new("Equipment".Translate(), () =>
        {
            pawn.equipment.DestroyAllEquipment();
            var thingStuffPair = PawnWeaponGenerator.allWeaponPairs.RandomElement();
            var thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
            PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
            if (thingWithComps.TryGetComp<CompEquippable>() is { } compEquippable)
            {
                if (pawn.kindDef.weaponStyleDef != null)
                    compEquippable.parent.StyleDef = pawn.kindDef.weaponStyleDef;
                else if (pawn.Ideo != null) compEquippable.parent.StyleDef = pawn.Ideo.GetStyleFor(thingWithComps.def);
            }

            pawn.equipment.AddEquipment(thingWithComps);
        });
        yield return new("Equipment".Translate() + " " + "PawnEditor.FromKind".Translate(),
            () =>
            {
                pawn.equipment.DestroyAllEquipment();
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, new(pawn.kindDef, pawn.Faction));
            });
        yield return new("Possessions".Translate(), () =>
        {
            pawn.inventory.DestroyAll();
            PawnInventoryGenerator.GenerateInventoryFor(pawn, new(pawn.kindDef, pawn.Faction));
        });
    }
}