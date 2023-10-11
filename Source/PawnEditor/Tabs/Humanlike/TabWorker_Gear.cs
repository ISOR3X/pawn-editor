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
    private const float ItemHeight = 30;
    private const float tableCategorySpacing = 16f;
    private readonly Dictionary<string, string[]> countBuffers = new();
    private Vector2 oldScrollPos = Vector2.zero;

    private Vector2 scrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);

        headerRect.xMin += 7;
        DrawEquipmentInfo(headerRect, pawn);
        DoBottomOptions(rect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);

        rect.xMin += 4;

        var categoryHeaderHeight = Text.LineHeightOf(GameFont.Small) * 2;
        var apparel = pawn.apparel.WornApparel;
        var apparelHeight = categoryHeaderHeight + apparel.Count * ItemHeight;
        var equipment = pawn.equipment.AllEquipmentListForReading;
        var equipmentHeight = categoryHeaderHeight + equipment.Count * ItemHeight;
        var possessions = pawn.inventory.innerContainer.ToList();
        var possessionsHeight = categoryHeaderHeight + possessions.Count * ItemHeight;

        var height = possessionsHeight + equipmentHeight + apparelHeight + 3 * tableCategorySpacing;
        var outRect = rect.ContractedBy(0f, 4f);
        var viewRect = new Rect(0, 0, outRect.width - 20, height);

        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
        DoEquipmentList(viewRect.TakeTopPart(apparelHeight), apparel,
            "Apparel".Translate(), thing => pawn.apparel.TryDrop(thing), ListingMenu_Items.ItemType.Apparel, pawn);
        viewRect.yMin += tableCategorySpacing;
        DoEquipmentList(viewRect.TakeTopPart(equipmentHeight), equipment,
            "Equipment".Translate(), pawn.equipment.Remove, ListingMenu_Items.ItemType.Equipment, pawn);
        viewRect.yMin += tableCategorySpacing;
        DoEquipmentList(viewRect.TakeTopPart(possessionsHeight), possessions, "Possessions".Translate(),
            thing => pawn.inventory.DropCount(thing.def, thing.stackCount), ListingMenu_Items.ItemType.Possessions, pawn);
        Widgets.EndScrollView();

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

    private static (float, float, float, float) GetColumnWidths(bool doCount)
    {
        float weightWidth, hpWidth, valueWidth, countWidth;
        if (doCount)
        {
            weightWidth = 80;
            hpWidth = 80;
            valueWidth = 120;
            countWidth = 100;
        }
        else
        {
            weightWidth = 80;
            hpWidth = 150;
            valueWidth = 150;
            countWidth = 0;
        }

        return (weightWidth, hpWidth, valueWidth, countWidth);
    }

    private void DoEquipmentList<T>(Rect inRect, List<T> equipment, string label, Action<T> remove, ListingMenu_Items.ItemType itemType, Pawn pawn,
        bool doCount = false) where T : Thing
    {
        if (!countBuffers.TryGetValue(label, out var countBufferArr) || countBufferArr.Length != equipment.Count)
        {
            countBufferArr = new string[equipment.Count];
            countBufferArr.Initialize();
            countBuffers[label] = countBufferArr;
        }

        var titleRect = inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small));
        var headerRect = inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small));

        const float buttonsWidth = ItemHeight + 100 + 4;
        var (weightWidth, hpWidth, valueWidth, countWidth) = GetColumnWidths(doCount);

        Widgets.Label(titleRect, label.Colorize(ColoredText.TipSectionTitleColor));

        if (equipment.Count > 0)
        {
            using (new TextBlock(TextAnchor.LowerLeft))
                Widgets.Label(headerRect.ContractedBy(ItemHeight + 4f, 0f), "PawnEditor.Name".Translate());

            using (new TextBlock(TextAnchor.LowerCenter))
            {
                headerRect.xMax -= buttonsWidth;
                if (doCount) Widgets.Label(headerRect.TakeRightPart(countWidth), "PenFoodTab_Count".Translate());
                Widgets.Label(headerRect.TakeRightPart(valueWidth), "MarketValueTip".Translate());
                Widgets.Label(headerRect.TakeRightPart(hpWidth), "PawnEditor.Hitpoints".Translate());
                Widgets.Label(headerRect.TakeRightPart(weightWidth), "PawnEditor.Weight".Translate());
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineHorizontal(inRect.x, inRect.y, inRect.width);
                GUI.color = Color.white;
            }
        }
        else
        {
            if (Mouse.IsOver(headerRect)) Widgets.DrawHighlight(headerRect);
            Widgets.Label(headerRect, "None".Translate().Colorize(ColoredText.SubtleGrayColor));
            TooltipHandler.TipRegionByKey(headerRect, "None");
        }

        for (var i = 0; i < equipment.Count; i++)
        {
            var rect = inRect.TakeTopPart(ItemHeight);
            rect.xMin += 4;
            var totalRect = new Rect(rect);
            if (i % 2 == 1) Widgets.DrawLightHighlight(rect);

            var thing = equipment[i];

            if (Widgets.ButtonImage(rect.TakeRightPart(ItemHeight).ContractedBy(2.5f), TexButton.DeleteX))
            {
                remove(thing);
                PawnEditor.Notify_PointsUsed();
            }

            rect.xMax -= 4;
            if (Widgets.ButtonText(rect.TakeRightPart(100), "Edit".Translate() + "..."))
            {
                if (Dialog_EditItem.SelectedThing == thing)
                {
                    Find.WindowStack.TryRemove(typeof(Dialog_EditItem));
                    Dialog_EditItem.SelectedThing = null;
                }
                else
                    Find.WindowStack.Add(new Dialog_EditItem(GUIUtility.GUIToScreenPoint(new(rect.x, rect.y)), pawn, thing));
            }

            if (doCount)
            {
                var countRect = rect.TakeRightPart(countWidth);
                var oldCount = thing.stackCount;
                UIUtility.IntField(countRect, ref thing.stackCount, 1, thing.def.stackLimit, ref countBufferArr[i]);
                if (oldCount != thing.stackCount)
                    PawnEditor.Notify_PointsUsed();
            }

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(totalRect);
                var tooltip =
                    $"{thing.LabelNoParenthesisCap.AsTipTitle()}{GenLabel.LabelExtras(thing, 1, true, true)}\n\n{thing.DescriptionDetailed}";
                if (thing.def.useHitPoints) tooltip = $"{tooltip}\n{thing.HitPoints} / {thing.MaxHitPoints}";
                TooltipHandler.TipRegion(rect, tooltip);
            }

            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(rect.TakeRightPart(valueWidth), (thing.MarketValue * thing.stackCount).ToStringMoney());
                Widgets.Label(rect.TakeRightPart(hpWidth),
                    (thing.HitPoints / (float)thing.MaxHitPoints).ToStringPercent());
                Widgets.Label(rect.TakeRightPart(weightWidth),
                    (thing.GetStatValue(StatDefOf.Mass) * thing.stackCount).ToStringMass());
                GUI.color = Color.white;
            }

            Widgets.ThingIcon(rect.TakeLeftPart(ItemHeight).ContractedBy(2.5f), thing);

            using (new TextBlock(TextAnchor.MiddleLeft))
                Widgets.Label(rect, thing.LabelCap);
        }
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
