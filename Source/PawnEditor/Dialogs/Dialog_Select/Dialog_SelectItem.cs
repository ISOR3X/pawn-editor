using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable PossibleLossOfFraction

namespace PawnEditor;

[StaticConstructorOnStartup]
public class Dialog_SelectItem : Dialog_SelectThing<ThingDef>
{
    public enum ItemType
    {
        Apparel,
        Equipment,
        Inventory,
        All
    }

    public static readonly HashSet<ThingStyle> thingStyles = new();
    private readonly IEnumerable<Thing> _activeItems;
    private readonly ItemType _itemType;
    private readonly string _thingCategoryLabel;
    private string[] _countBuffer;

    private Vector2 _scrollPosition;
    private Thing _selected;

    private readonly IEnumerable<BodyPartGroupDef> occupiableGroupsDefs;

    static Dialog_SelectItem()
    {
        foreach (var styleCategoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
        foreach (var thingDefStyle in styleCategoryDef.thingDefStyles)
        {
            if (thingStyles.Select(ts => ts.ThingDef).Contains(thingDefStyle.thingDef))
            {
                thingStyles.FirstOrDefault(ts => ts.ThingDef == thingDefStyle.thingDef).StyleDefs.Add(thingDefStyle.styleDef, styleCategoryDef);
                continue;
            }

            thingStyles.Add(new()
            {
                ThingDef = thingDefStyle.thingDef,
                StyleDefs = new()
                {
                    { thingDefStyle.styleDef, styleCategoryDef }
                }
            });
        }
    }

    public Dialog_SelectItem(List<ThingDef> thingList, Pawn curPawn, ref IEnumerable<Thing> activeItems, TreeNode_ThingCategory treeNodeThingCategory = null,
        string thingCategoryLabel = "ItemsTab",
        ItemType itemType = ItemType.Inventory, Thing selected = null) : base(thingList,
        curPawn)
    {
        _thingCategoryLabel = thingCategoryLabel;
        TreeNodeThingCategory = treeNodeThingCategory ?? ThingCategoryNodeDatabase.RootNode;
        Listing = new Listing_TreeThing(_quickSearchWidget.filter, ThingList);
        HasOptions = true;

        occupiableGroupsDefs = CurPawn.def.race.body.cachedAllParts.SelectMany(p => p.groups)
           .Distinct()
           .Where(bp => ThingList.Select(td => td.apparel.bodyPartGroups)
               .Any(bpg => bpg.Contains(bp)));


        _itemType = itemType;
        _activeItems = activeItems;

        _selected = selected ?? _activeItems.FirstOrDefault();
        OnSelected = AddItem;
    }

    protected override string PageTitle => "ChooseStuffForRelic".Translate() + " " + _thingCategoryLabel.Translate().ToLower();

    public override Vector2 InitialSize => new(900f, 700);

    protected override List<TFilter<ThingDef>> Filters()
    {
        var filters = base.Filters();
        filters.Add(new("PawnEditor.HasStyle".Translate(), false, def => thingStyles.Select(ts => ts.ThingDef).Contains(def)));
        filters.Add(new("PawnEditor.HasStuff".Translate(), false, def => def.MadeFromStuff));

        if (_itemType == ItemType.Apparel)
        {
            var bodyPartDict = occupiableGroupsDefs.ToDictionary<BodyPartGroupDef, FloatMenuOption, Func<ThingDef, bool>>(
                def => new(def.LabelCap, () => { }),
                def => td => td.apparel.bodyPartGroups.Contains(def));
            filters.Add(new("PawnEditor.WornOnBodyPart".Translate(), false, bodyPartDict));
        }

        return filters;
    }

    protected override void DrawInfoCard(ref Rect inRect)
    {
        base.DrawInfoCard(ref inRect);

        if (!_activeItems.Any()) return;
        _countBuffer = new string[_activeItems.Count()];

        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 16f, 32 * _activeItems.Count());
        var outRect = inRect.TakeTopPart(Mathf.Min(32f * 5, _activeItems.Count() * 32f)).ExpandedBy(4f);
        outRect.xMax -= 8f;

        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);

        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var itemsToRemove = new List<Thing>();

            for (var i = 0; i < _activeItems.Count(); i++)
            {
                var thing = _activeItems.ToList()[i];
                var rowRect = viewRect.TakeTopPart(32f);
                Widgets.ThingIcon(rowRect.TakeLeftPart(32f).ContractedBy(2.5f), thing);

                if (Widgets.ButtonImage(rowRect.TakeRightPart(32f).ContractedBy(2.5f), TexButton.DeleteX)) itemsToRemove.Add(thing);

                if (Widgets.ButtonImage(rowRect.TakeRightPart(32f).ContractedBy(2.5f), TexButton.Info)) Find.WindowStack.Add(new Dialog_InfoCard(thing.def));

                if (Widgets.RadioButton(rowRect.xMax - 32f, rowRect.y + (32f - Widgets.RadioButtonSize) / 2,
                        _selected != null && _selected.def == thing.def)) _selected = thing;

                rowRect.xMax -= 32f + 16f; // 32f from radiobutton, and 16f for spacing

                if (_itemType == ItemType.Inventory)
                {
                    ref var count = ref thing.stackCount;
                    UIUtility.IntField(rowRect.TakeRightPart(120f), ref count, 1, 999, ref _countBuffer[i]);
                }

                Widgets.Label(rowRect, thing.LabelCap);

                if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);
                if (Widgets.ButtonInvisible(rowRect)) _selected = thing;
            }

            RemoveItems(itemsToRemove);
            itemsToRemove.Clear();
            _countBuffer = null;
        }

        // UpdateInventory();
        Widgets.EndScrollView();

        // Free BodyPartGroups label
        inRect.yMin += 4f;
        var occupiedGroups = occupiableGroupsDefs.Where(bp => CurPawn.apparel.WornApparel.Any(a => a.def.apparel.bodyPartGroups.Contains(bp)));
        var allGroups = occupiableGroupsDefs;
        var freeGroups = allGroups.Except(occupiedGroups);
        var freeGroupsString = string.Join(", ", freeGroups.Select(g => g.LabelCap));

        var labelRect = inRect.TakeTopPart(Text.LineHeight);
        var label = $"Unoccupied body parts: {freeGroupsString}";
        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(labelRect, label.Truncate(inRect.width, truncateCache).Colorize(ColoredText.SubtleGrayColor));
        TooltipHandler.TipRegion(labelRect, label);
        
        inRect.TakeTopPart(16f);
    }

    protected override void DrawOptions(ref Rect inRect)
    {
        base.DrawOptions(ref inRect);
        const float labelWidthPct = 0.3f;
        if (_selected == null) return;

        var cellCount = 0;
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            // Stuff
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "StatsReport_Material".Translate());
            if (_selected.def.stuffCategories is { Count: > 1 })
            {
                var options = new List<FloatMenuOption>();
                foreach (var stuff in GenStuff.AllowedStuffsFor(_selected.def))
                    options.Add(new(stuff.LabelCap, () =>
                    {
                        _selected.SetStuffDirect(stuff);
                        _selected.Notify_ColorChanged();
                    }, Widgets.GetIconFor(stuff), stuff.uiIconColor));

                if (UIUtility.ButtonTextImage(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), _selected.Stuff))
                    Find.WindowStack.Add(new FloatMenu(options));
            }
            else
                NoOption(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct));

            cellCount++;

            // Quality
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Quality".Translate());
            if (_selected.TryGetComp<CompQuality>() != null)
            {
                var compQuality = _selected.TryGetComp<CompQuality>();
                var buttonLabel = compQuality.Quality.GetLabel().CapitalizeFirst();

                var options3 = new List<FloatMenuOption>();
                foreach (var quality in QualityUtility.AllQualityCategories)
                    options3.Add(new(quality.GetLabel().CapitalizeFirst(), () =>
                    {
                        buttonLabel = quality.GetLabel().CapitalizeFirst();
                        compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
                    }));

                if (Widgets.ButtonText(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct), buttonLabel))
                    Find.WindowStack.Add(new FloatMenu(options3));
            }
            else
                NoOption(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct));

            cellCount++;

            // Color
            // ToDo: Set favourite color or reset color to default.
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Color".Translate());
            if (_selected is Apparel apparel2 && _selected.TryGetComp<CompColorable>() != null)
            {
                var widgetRect = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
                var colorRect = widgetRect.TakeRightPart(24f);
                widgetRect.xMax -= 4f;
                colorRect.height = 24f;
                colorRect.y += 3f;
                var curColor = apparel2.GetComp<CompColorable>().color;
                curColor = curColor == Color.white ? apparel2.Stuff.stuffProps.color : curColor;

                if (Widgets.ButtonText(widgetRect, "PawnEditor.PickColor".Translate()))
                    Find.WindowStack.Add(new Dialog_ColorPicker(color => apparel2.SetColor(color),
                        DefDatabase<ColorDef>.AllDefs.Select(cd => cd.color).ToList(), curColor, apparel2.Stuff.stuffProps.color,
                        CurPawn.story.favoriteColor));

                Widgets.DrawRectFast(colorRect, curColor);
            }
            else
                NoOption(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct));

            cellCount++;

            // Style
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "Stat_Thing_StyleLabel".Translate());
            if (thingStyles.Select(ts => ts.ThingDef).Contains(_selected.def))
            {
                List<FloatMenuOption> options2 = new();
                var styleOptions = thingStyles.FirstOrDefault(ts => ts.ThingDef == _selected.def).StyleDefs;
                foreach (var style in styleOptions)
                    options2.Add(new(style.Value.LabelCap, () =>
                    {
                        _selected.SetStyleDef(style.Key);
                        _selected.Notify_ColorChanged();
                    }, style.Value.Icon, Color.white));

                options2.Add(new("None", () =>
                {
                    _selected.SetStyleDef(null);
                    _selected.Notify_ColorChanged();
                }));

                if (UIUtility.ButtonTextImage(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct),
                        styleOptions.FirstOrDefault(so => so.Key == _selected.GetStyleDef()).Value)) Find.WindowStack.Add(new FloatMenu(options2));
            }
            else
                NoOption(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct));

            cellCount++;


            // Hit Points
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "HitPointsBasic".Translate().CapitalizeFirst());
            float hitPoints = _selected.HitPoints;
            var widgetRect2 = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
            widgetRect2.y += widgetRect2.height / 2f;
            Widgets.HorizontalSlider(widgetRect2, ref hitPoints, new(0, _selected.MaxHitPoints));
            _selected.HitPoints = (int)hitPoints;
            cellCount++;

            // Tainted
            Widgets.Label(UIUtility.CellRect(cellCount, inRect).LeftPart(labelWidthPct), "PawnEditor.Tainted".Translate());
            if (_selected is Apparel apparel)
            {
                var widgetRect = UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct);
                var isTainted = apparel.WornByCorpse;
                Widgets.Checkbox(new(widgetRect.x + (widgetRect.width - Widgets.CheckboxSize) / 2, widgetRect.y + 3f), ref isTainted);
                apparel.wornByCorpseInt = isTainted;
            }
            else
                NoOption(UIUtility.CellRect(cellCount, inRect).RightPart(1 - labelWidthPct));

            cellCount++;
        }

        inRect.TakeTopPart(Mathf.CeilToInt(cellCount / 2f) * 38f + 16f);
    }

    private static void NoOption(Rect inRect)
    {
        using (new TextBlock(TextAnchor.MiddleCenter))
            Widgets.Label(inRect, "PawnEditor.Unavailable".Translate().Colorize(ColoredText.SubtleGrayColor));
        if (Mouse.IsOver(inRect))
        {
            Widgets.DrawHighlight(inRect);
            TooltipHandler.TipRegion(inRect, "PawnEditor.UnavailableDesc".Translate());
        }
    }

    private void RemoveItems(List<Thing> things)
    {
        switch (_itemType)
        {
            case ItemType.Apparel:
                foreach (var thing in things) CurPawn.apparel.Remove((Apparel)thing);

                break;
            case ItemType.Equipment:
                foreach (var thing in things) CurPawn.equipment.Remove((ThingWithComps)thing);

                break;
            case ItemType.Inventory:
                foreach (var thing in things) CurPawn.inventory.RemoveCount(thing.def, thing.stackCount);

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AddItem(ThingDef thingDef)
    {
        switch (_itemType)
        {
            case ItemType.Apparel:
                if (thingDef.IsApparel)
                {
                    // ApparelUtility.CanWearTogether()
                    PawnApparelGenerator.allApparelPairs.Where(pair => pair.thing == thingDef).TryRandomElement(out var thingStuffPair);
                    var apparel = (Apparel)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                    CurPawn.apparel.Wear(apparel, false);
                }

                break;
            case ItemType.Equipment:
                if (thingDef.equipmentType != EquipmentType.None)
                {
                    PawnWeaponGenerator.allWeaponPairs.Where(pair => pair.thing == thingDef).TryRandomElement(out var thingStuffPair);
                    var equipment = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                    CurPawn.equipment.MakeRoomFor(equipment);
                    CurPawn.equipment.AddEquipment(equipment);
                }

                break;
            case ItemType.Inventory:
                var thing = ThingMaker.MakeThing(thingDef, thingDef.defaultStuff);
                CurPawn.inventory.innerContainer.TryAdd(thing, 1);
                break;
        }
    }

    public struct ThingStyle
    {
        public ThingDef ThingDef;
        public Dictionary<ThingStyleDef, StyleCategoryDef> StyleDefs;
    }
}
