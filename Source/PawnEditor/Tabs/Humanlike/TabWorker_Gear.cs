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
    private readonly Dictionary<string, string[]> countBuffers = new();

    private Vector2 scrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        PawnEditor.DrawPawnPortrait(portraitRect);

        headerRect.xMin += 7;
        DrawEquipmentInfo(headerRect, pawn);
        DoAddButtons(rect.TakeBottomPart(30), pawn);

        rect.xMin += 4;
        
        var apparel = pawn.apparel.WornApparel;
        var equipment = pawn.equipment.AllEquipmentListForReading;
        var possessions = pawn.inventory.innerContainer.ToList();

        var textHeight = Text.LineHeightOf(GameFont.Medium);
        var height = textHeight * 3 + (apparel.Count + equipment.Count + possessions.Count) * ItemHeight;
        var viewRect = new Rect(0, 0, rect.width - 20, height);
        Widgets.BeginScrollView(rect, ref scrollPos, viewRect);
        viewRect.yMin += 4f;
        DoEquipmentList(viewRect.TakeTopPart(textHeight + (apparel.Count + 1) * ItemHeight), apparel,
            "Apparel".Translate(), thing => pawn.apparel.TryDrop(thing));
        DoEquipmentList(viewRect.TakeTopPart(textHeight + (equipment.Count + 1) * ItemHeight), equipment,
            "Equipment".Translate(), pawn.equipment.Remove);
        DoEquipmentList(viewRect, possessions, "Possessions".Translate(),
            thing => pawn.inventory.DropCount(thing.def, thing.stackCount), true);
        Widgets.EndScrollView();
    }

    private void DrawEquipmentInfo(Rect inRect, Pawn pawn)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.ColumnWidth /= 2;
        listing.Label("MassCarried".Translate(MassUtility.GearAndInventoryMass(pawn).ToString("0.##"),
            MassUtility.Capacity(pawn).ToString("0.##")));
        listing.Label("ComfyTemperatureRange".Translate() + ": " +
                      pawn.GetStatValue(StatDefOf.ComfyTemperatureMin).ToStringTemperature("F0") + " ~ "
                      + pawn.GetStatValue(StatDefOf.ComfyTemperatureMax).ToStringTemperature("F0"));
        listing.Gap(15);
        listing.ColumnWidth /= 2;
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

    private void DoEquipmentList<T>(Rect inRect, List<T> equipment, string label, Action<T> remove,
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
                remove(thing);
            rect.xMax -= 4;
            if (Widgets.ButtonText(rect.TakeRightPart(100), "Edit".Translate() + "..."))
            {
            }
            
            if (doCount && !Utilities.SubMenuOpen)
            {
                var countRect = rect.TakeRightPart(countWidth);
                ref var count = ref thing.stackCount;
                if (count > 1 && Widgets.ButtonImage(countRect.TakeLeftPart(25).ContractedBy(0, 5),
                        TexPawnEditor.ArrowLeftHalf))
                {
                    count--;
                    countBufferArr[i] = null;
                }
            
                if (Widgets.ButtonImage(countRect.TakeRightPart(25).ContractedBy(0, 5), TexPawnEditor.ArrowRightHalf))
                {
                    count++;
                    countBufferArr[i] = null;
                }
            
                Widgets.TextFieldNumeric(countRect, ref count, ref countBufferArr[i]);
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

        inRect.yMin += Text.LineHeightOf(GameFont.Small);
    }

    private static void DoAddButtons(Rect inRect, Pawn pawn)
    {
        inRect = inRect.LeftHalf();
        var (apparel, equipment, possessions) = inRect.Split1D(3, false, 6);
        if (Widgets.ButtonText(apparel, "Add".Translate().CapitalizeFirst() + " " + "Apparel".Translate().ToLower()))
        {
            IEnumerable<Thing> curApparel = pawn.apparel.WornApparel;
            Find.WindowStack.Add(new Dialog_SelectItem(DefDatabase<ThingDef>.AllDefs.Where(td => td.IsApparel).ToList(), pawn, ref curApparel, ThingCategoryNodeDatabase.allThingCategoryNodes
                .FirstOrDefault(tc => tc.catDef == ThingCategoryDefOf.Apparel), thingCategoryLabel: "Apparel", itemType: Dialog_SelectItem.ItemType.Apparel));
        }

        if (Widgets.ButtonText(equipment,
                "Add".Translate().CapitalizeFirst() + " " + "Equipment".Translate().ToLower()))
        {
            IEnumerable<Thing> curEquipment = pawn.equipment.AllEquipmentListForReading;
            Find.WindowStack.Add(new Dialog_SelectItem(DefDatabase<ThingDef>.AllDefs.Where(td => td.comps.Any(c => c.compClass == typeof(CompEquippable))).ToList(), pawn, ref curEquipment,
                thingCategoryLabel: "Equipment", itemType: Dialog_SelectItem.ItemType.Equipment));
        }

        if (Widgets.ButtonText(possessions,
                "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.Possession".Translate()))
        {
            IEnumerable<Thing> curPossessions = pawn.inventory.innerContainer;
            Find.WindowStack.Add(new Dialog_SelectItem(DefDatabase<ThingDef>.AllDefs.Where(td => td.category == ThingCategory.Item).ToList(), pawn, ref curPossessions, thingCategoryLabel: "PawnEditor.Possession"));
        }
    }

    public override IEnumerable<SaveLoadItem> GetSaveLoadItems(Pawn pawn)
    {
        yield return new SaveLoadItem<Pawn_ApparelTracker>("Apparel".Translate(), pawn.apparel);
        yield return new SaveLoadItem<Pawn_EquipmentTracker>("Equipment".Translate(), pawn.equipment);
        yield return new SaveLoadItem<Pawn_InventoryTracker>("Possessions".Translate(), pawn.inventory);
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Pawn pawn)
    {
        yield return new FloatMenuOption("Apparel".Translate(), () =>
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
        yield return new FloatMenuOption("Apparel".Translate() + " " + "PawnEditor.FromKind".Translate(), () =>
            PawnApparelGenerator.GenerateStartingApparelFor(
                pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction)));
        yield return new FloatMenuOption("Equipment".Translate(), () =>
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
        yield return new FloatMenuOption("Equipment".Translate() + " " + "PawnEditor.FromKind".Translate(),
            () =>
            {
                pawn.equipment.DestroyAllEquipment();
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction));
            });
        yield return new FloatMenuOption("Possessions".Translate(), () =>
        {
            pawn.inventory.DestroyAll();
            PawnInventoryGenerator.GenerateInventoryFor(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction));
        });
    }
}