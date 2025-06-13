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
    private static UITable<Pawn> apparelTable;
    private static UITable<Pawn> equipmentTable;
    private static UITable<Pawn> possessionsTable;
    private Vector2 oldScrollPos = Vector2.zero;
    private Vector2 scrollPos;

    public override void Initialize()
    {
        base.Initialize();
        apparelTable = new(GetHeadings(), p => GetRows(p, apparelTable, pawn => pawn.apparel.WornApparel.Cast<Thing>().ToList()));
        equipmentTable = new(GetHeadings(), p => GetRows(p, equipmentTable, pawn => pawn.equipment.equipment.Cast<Thing>().ToList()));
        possessionsTable = new(GetHeadings(), p => GetRows(p, possessionsTable, pawn => pawn.inventory.innerContainer.ToList()));
    }

    private List<UITable<Pawn>.Heading> GetHeadings() =>
        new()
        {
            new(32),
            new("PawnEditor.Name".Translate(), textAnchor: TextAnchor.MiddleLeft),
            new("PawnEditor.Weight".Translate(), 120),
            new("PawnEditor.Hitpoints".Translate(), 120),
            new("MarketValueTip".Translate(), 120),
            new(16),
            new(100), // Edit
            new(24) // Delete
        };

    private IEnumerable<UITable<Pawn>.Row> GetRows(Pawn pawn, UITable<Pawn> table, Func<Pawn, List<Thing>> thingsGetter)
    {
        var things = thingsGetter(pawn);
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            var items = new List<UITable<Pawn>.Row.Item>
            {
                new(iconRect =>
                {
                    Widgets.ThingIcon(iconRect.ContractedBy(4f), thing);
                    iconRect.xMin += 4f;
                    if (Mouse.IsOver(iconRect))
                    {
                        Widgets.DrawHighlight(iconRect);
                        TooltipHandler.TipRegion(iconRect, "PawnEditor.ClickToOpen".Translate());
                    }

                    if (Widgets.ButtonInvisible(iconRect)) Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }),
                new(thing.LabelCap, thing.LabelCap.ToCharArray()[0], TextAnchor.MiddleLeft),
                new(thing.GetStatValue(StatDefOf.Mass).ToStringMass().Colorize(ColoredText.SubtleGrayColor), (int)thing.GetStatValue(StatDefOf.Mass)),
                new(((float)thing.HitPoints / thing.MaxHitPoints).ToStringPercent().Colorize(ColoredText.SubtleGrayColor),
                    thing.HitPoints / thing.MaxHitPoints),
                new(thing.MarketValue.ToStringMoney().Colorize(ColoredText.SubtleGrayColor), (int)thing.MarketValue),
                new(),
                new(editRect => EditUtility.EditButton(editRect, thing, pawn, table)),
                new(TexButton.Delete, () =>
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
        apparelTable.ClearCache();
        equipmentTable.ClearCache();
        possessionsTable.ClearCache();
    }

    private void DoTables(Rect inRect, Pawn pawn)
    {
        var apparelHeight = apparelTable.Height;
        var equipmentHeight = equipmentTable.Height;
        var possessionsHeight = possessionsTable.Height;
        var totalHeight = apparelHeight + equipmentHeight + possessionsHeight +
                          16f * 2f + 3f * 4f + 3 * Text.LineHeightOf(GameFont.Small);
                         // Actual table height + table padding + table title separation + table title height;

        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 20f, totalHeight);

        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "Apparel".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        apparelTable.OnGUI(viewRect.TakeTopPart(apparelHeight), pawn);
        viewRect.xMin += -4f;
        viewRect.yMin += 16f;

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "Equipment".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        equipmentTable.OnGUI(viewRect.TakeTopPart(equipmentHeight), pawn);
        viewRect.xMin += -4f;
        viewRect.yMin += 16f;

        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(viewRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)),
                "Possessions".Translate().CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor));
        viewRect.xMin += 4f;
        viewRect.yMin += 4f;
        possessionsTable.OnGUI(viewRect.TakeTopPart(possessionsHeight), pawn);

        Widgets.EndScrollView();
    }


    public override void DrawTabContents(Rect inRect, Pawn pawn)
    {
        var headerRect = inRect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);

        headerRect.xMin += 7;
        DrawEquipmentInfo(headerRect, pawn);
        DoBottomOptions(inRect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);

        inRect = inRect.ContractedBy(4f);
        DoTables(inRect, pawn);

        // Close Dialog_EditItem on any interaction with the Dialog_PawnEditor menu.
        if (Find.WindowStack.IsOpen<Dialog_EditItem>())
            if (oldScrollPos != scrollPos || Find.WindowStack.focusedWindow is Dialog_PawnEditor)
                Find.WindowStack.TryRemove(typeof(Dialog_EditItem));
        // Dialog_EditItem.SelectedThing = null;
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
                                {
                                    a.SetColor(pawn.story.favoriteColor.color);
                                }
                                else
                                {
                                    Messages.Message("No favourite color found for pawn", MessageTypeDefOf.RejectInput);
                                }
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
            PawnApparelGenerator.workingSet.Reset(pawn);
            PawnApparelGenerator.usableApparel.Clear();
            PawnApparelGenerator.usableApparel.AddRange(PawnApparelGenerator.allApparelPairs.Where(apparel =>
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

            PawnApparelGenerator.workingSet.GiveToPawn(pawn);
            PawnApparelGenerator.workingSet.Reset(null, null);

            apparelTable.ClearCache();
        });
        yield return new("Apparel".Translate() + " " + "PawnEditor.FromKind".Translate(), () =>
        {
            PawnApparelGenerator.GenerateStartingApparelFor(
                pawn, new(pawn.kindDef, pawn.Faction));

            apparelTable.ClearCache();
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
            equipmentTable.ClearCache();
        });
        yield return new("Equipment".Translate() + " " + "PawnEditor.FromKind".Translate(),
            () =>
            {
                pawn.equipment.DestroyAllEquipment();
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, new(pawn.kindDef, pawn.Faction));
                equipmentTable.ClearCache();
            });
        yield return new("Possessions".Translate(), () =>
        {
            pawn.inventory.DestroyAll();
            PawnInventoryGenerator.GenerateInventoryFor(pawn, new(pawn.kindDef, pawn.Faction));
            possessionsTable.ClearCache();
        });
    }
}
