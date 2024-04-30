using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
// ToDo: Highlight current items?
public class ListingMenu_Items : ListingMenu<ThingDef>
{
    public enum ItemType
    {
        All = 0,
        Apparel,
        Equipment,
        Possessions,
        Starting
    }

    private static ItemType type;
    private static List<ThingDef> apparel;
    private static List<ThingDef> kidApparel;
    private static List<ThingDef> equipment;
    private static List<ThingDef> items;
    private static List<ThingDef> starting;
    private static List<ThingDef> all;

    private static readonly Func<ThingDef, string> labelGetter = t => t.LabelCap;
    private static readonly Func<ThingDef, string> descGetter = t => t.DescriptionDetailed;
    private static readonly Action<ThingDef, Rect> iconDrawer = DrawThingIcon;

    public static readonly HashSet<ThingStyle> ThingStyles = new();
    private static IEnumerable<BodyPartGroupDef> occupiableGroupsDefs;

    static ListingMenu_Items()
    {
        foreach (var styleCategoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
        foreach (var thingDefStyle in styleCategoryDef.thingDefStyles) // A list of thing defs and their style def to apply.
        {
            if (ThingStyles.Select(ts => ts.ThingDef).Contains(thingDefStyle.ThingDef))
            {
                // If the def already exists in the list, add the style to the existing list.
                ThingStyles.FirstOrDefault(ts => ts.ThingDef == thingDefStyle.thingDef).StyleDefs.TryAdd(thingDefStyle.StyleDef, styleCategoryDef);
                continue;
            }

            
            ThingStyles.Add(new()
            {
                ThingDef = thingDefStyle.ThingDef,
                StyleDefs = new()
                {
                    { thingDefStyle.styleDef, styleCategoryDef }
                }
            });
        }

        MakeItemLists();
    }

    public ListingMenu_Items(ItemType itemType, Pawn pawn, TreeNode_ThingCategory treeNodeThingCategory = null) : base(t => TryAdd(t, pawn),
        GetMenuTitle(itemType), pawn)
    {
        TreeNodeThingCategory = treeNodeThingCategory ?? ThingCategoryNodeDatabase.RootNode;
        type = itemType;
        Listing = new Listing_TreeThing(GetItemList(itemType, pawn), labelGetter, iconDrawer, descGetter);

        occupiableGroupsDefs = pawn.def.race.body.cachedAllParts.SelectMany(p => p.groups)
            .Distinct()
            .Where(bp => apparel.Select(td => td.apparel.bodyPartGroups)
                .Any(bpg => bpg.Contains(bp)));
    }

    public ListingMenu_Items(List<Thing> things, ItemType itemType, Action callback = null, string menuTitle = null) : base(t => TryAdd(t, things, callback),
        menuTitle)
    {
        TreeNodeThingCategory = ThingCategoryNodeDatabase.RootNode;
        type = itemType;
        Listing = new Listing_TreeThing(GetItemList(itemType), labelGetter, iconDrawer, descGetter);
    }

    public ListingMenu_Items(Func<Thing, AddResult> adder, ItemType itemType, string menuTitle = null) : base(t => TryAdd(t, adder), menuTitle)
    {
        TreeNodeThingCategory = ThingCategoryNodeDatabase.RootNode;
        type = itemType;
        Listing = new Listing_TreeThing(GetItemList(itemType), labelGetter, iconDrawer, descGetter);
    }

    public override void PreOpen()
    {
        base.PreOpen();
        // Filters are set on open because some filters depend on the pawn.
        Listing.Filters = GetFilters();
    }

    private static void DrawThingIcon(ThingDef thingDef, Rect rect)
    {
        var color = Color.white;
        var texture = Widgets.PlaceholderIconTex;
        if (thingDef != null)
        {
            color = GenStuff.AllowedStuffsFor(thingDef).FirstOrDefault()?.stuffProps.color ?? color;
            if (thingDef.colorGenerator != null)
                color = thingDef.colorGenerator.ExemplaryColor;
            texture = Widgets.GetIconFor(thingDef);
        }

        GUI.color = color;
        Widgets.DrawTextureFitted(rect, texture, .8f);
        GUI.color = Color.white;
    }

    private static string GetMenuTitle(ItemType itemType)
    {
        var typeLabel = "PawnEditor.ItemType." + itemType;
        return "PawnEditor.Choose".Translate() + " " + typeLabel.Translate().ToLower();
    }

    private static void CheckCapacity(Pawn pawn, Thing newItem)
    {
        if (MassUtility.FreeSpace(pawn) < newItem.GetStatValue(StatDefOf.Mass))
            Messages.Message("PawnEditor.WouldMakeOverCapacity".Translate(newItem.LabelCap, pawn.NameShortColored), MessageTypeDefOf.CautionInput, false);
    }

    private static AddResult TryAdd(ThingDef thingDef, Pawn pawn)
    {
        switch (type)
        {
            case ItemType.Apparel:
            {
                if (HARCompat.Active && HARCompat.EnforceRestrictions && !HARCompat.CanWear(thingDef, pawn))
                    return "PawnEditor.HARRestrictionViolated".Translate(pawn.Named("PAWN"), pawn.def.label.Named("RACE"), "PawnEditor.Wear".Named("VERB"),
                        thingDef.label.Named("ITEM"));

                if (thingDef.IsApparel)
                    {
                        Apparel newApparel = MakeApparel(thingDef);
                        AddResult result = new ConditionalInfo(PawnEditor.CanUsePoints(newApparel), new SuccessInfo(() =>
                        {
                            CheckCapacity(pawn, newApparel);
                            pawn.apparel.Wear(newApparel, false);
                            PawnEditor.Notify_PointsUsed();
                            TabWorker_Gear.ClearCaches();
                        }));


                        if (pawn.apparel.WornApparel.FirstOrDefault(ap => !ApparelUtility.CanWearTogether(thingDef, ap.def, pawn.RaceProps.body)) is
                            { } conflictApparel)
                            result = new ConfirmInfo("PawnEditor.WearingWouldRemove".Translate(thingDef.LabelCap, conflictApparel.LabelCap), "ApparelConflict",
                                result);

                        return result;
                    }

                    break;
            }
            case ItemType.Equipment:
            {
                if (HARCompat.Active && HARCompat.EnforceRestrictions && !HARCompat.CanEquip(thingDef, pawn))
                    return "PawnEditor.HARRestrictionViolated".Translate(pawn.Named("PAWN"), pawn.def.label.Named("RACE"), "PawnEditor.Equip".Named("VERB"),
                        thingDef.label.Named("ITEM"));

                if (thingDef.equipmentType != EquipmentType.None)
                    {
                        ThingWithComps newEquipment = MakeEquipment(thingDef);
                        return new ConditionalInfo(PawnEditor.CanUsePoints(newEquipment), new SuccessInfo(() =>
                        {
                            pawn.equipment.MakeRoomFor(newEquipment);
                            pawn.equipment.AddEquipment(newEquipment);
                            PawnEditor.Notify_PointsUsed();
                            TabWorker_Gear.ClearCaches();
                        }));
                    }
                    break;
            }
            case ItemType.All:
            case ItemType.Possessions:
                var newPossession = ThingMaker.MakeThing(thingDef, thingDef.defaultStuff);
                return new ConditionalInfo(PawnEditor.CanUsePoints(newPossession), new SuccessInfo(() =>
                {
                    pawn.inventory.innerContainer.TryAdd(newPossession, 1);
                    PawnEditor.Notify_PointsUsed();
                    TabWorker_Gear.ClearCaches();
                }));
            default:
                Log.WarningOnce("No ItemType!", 15703);
                break;
        }

        return false;
    }

    private static Apparel MakeApparel(ThingDef thingDef)
    {
        if (thingDef.MadeFromStuff)
        {
            if (PawnApparelGenerator.allApparelPairs.Where(pair => pair.thing == thingDef)
                .TryRandomElement(out var thingStuffPair))
            {
                var newApparel = (Apparel)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                return newApparel;
            }
            else
            {
                var newApparel = (Apparel)ThingMaker.MakeThing(thingDef, GenStuff.DefaultStuffFor(thingDef));
                return newApparel;
            }
        }
        else
        {
            var newApparel = (Apparel)ThingMaker.MakeThing(thingDef);
            return newApparel;
        }
    }

    private static ThingWithComps MakeEquipment(ThingDef thingDef)
    {
        if (thingDef.MadeFromStuff)
        {
            if (PawnWeaponGenerator.allWeaponPairs.Where(pair => pair.thing == thingDef)
                    .TryRandomElement(out var thingStuffPair))
            {
                var newEquipment = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                return newEquipment;
            }
            else
            {
                var newEquipment = (ThingWithComps)ThingMaker.MakeThing(thingDef, GenStuff.DefaultStuffFor(thingDef));
                return newEquipment;
            }
        }
        else
        {
            var newEquipment = (ThingWithComps)ThingMaker.MakeThing(thingDef);
            return newEquipment;
        }
    }

    private static AddResult TryAdd(ThingDef thingDef, List<Thing> things, Action callback = null)
    {
        var thing = ThingMaker.MakeThing(thingDef);
        return new ConditionalInfo(PawnEditor.CanUsePoints(thing), new SuccessInfo(() =>
        {
            things.Add(thing);
            callback?.Invoke();
        }));
    }

    private static AddResult TryAdd(ThingDef thingDef, Func<Thing, AddResult> adder)
    {
        var thing = ThingMaker.MakeThing(thingDef);
        return new ConditionalInfo(PawnEditor.CanUsePoints(thing), adder(thing));
    }

    private static void MakeItemLists()
    {
        apparel = DefDatabase<ThingDef>.AllDefs.Where(td => td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Adult)).ToList();
        kidApparel = DefDatabase<ThingDef>.AllDefs.Where(td => td.IsApparel && td.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child)).ToList();
        equipment = DefDatabase<ThingDef>.AllDefs.Where(td => td.equipmentType == EquipmentType.Primary).ToList();
        items = DefDatabase<ThingDef>.AllDefs.Where(td => td.category == ThingCategory.Item).ToList();
        starting = DefDatabase<ThingDef>.AllDefs.Where(td =>
                (td.category == ThingCategory.Item && td.scatterableOnMapGen && !td.destroyOnDrop) || (td.category == ThingCategory.Building && td.Minifiable)
                                                                                                   || (td.category == ThingCategory.Building
                                                                                                       && td.scatterableOnMapGen))
            .ToList();
        all = DefDatabase<ThingDef>.AllDefs.ToList();
    }

    private static List<ThingDef> GetItemList(ItemType itemType2, Pawn pawn = null)
    {
        switch (itemType2)
        {
            case ItemType.Apparel:
                if (pawn != null && pawn.DevelopmentalStage != DevelopmentalStage.Adult)
                    return kidApparel;
                return apparel;
            case ItemType.Equipment:
                return equipment;
            case ItemType.Possessions:
                return items;
            case ItemType.Starting:
                return starting;
            default:
                Log.WarningOnce("No ItemType!", 15703);
                return all;
        }
    }

    private List<Filter<ThingDef>> GetFilters()
    {
        var list = new List<Filter<ThingDef>>();

        list.Add(new Filter_ModSource<ThingDef>());
        list.Add(new Filter_Toggle<ThingDef>("PawnEditor.HasStyle".Translate(), def => ThingStyles.Select(ts => ts.ThingDef).Contains(def)));
        list.Add(new Filter_Toggle<ThingDef>("PawnEditor.HasStuff".Translate(), def => def.MadeFromStuff));

        if (type == ItemType.Apparel && Pawn != null)
        {
            list.Add(new Filter_Dropdown<ThingDef>("PawnEditor.WornOnBodyPart".Translate(),
                Filter_Dropdown<ThingDef>.GetDefFilter((ThingDef td, BodyPartGroupDef def) => 
                td.apparel.bodyPartGroups.Contains(def), occupiableGroupsDefs)));

            list.Add(new Filter_Dropdown<ThingDef>("PawnEditor.OccupiesLayer".Translate(), 
                Filter_Dropdown<ThingDef>.GetDefFilter((ThingDef td, ApparelLayerDef def) => td.apparel.layers.Contains(def))));
        }
        
        return list;
    }

    public struct ThingStyle
    {
        public ThingDef ThingDef; // The thing def that has styles
        public Dictionary<ThingStyleDef, StyleCategoryDef> StyleDefs; // The graphic is the key, the style group is the value
    }
}