using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[ModCompat("erdelf.humanoidalienraces", "erdelf.humanoidalienraces.dev")]
// ReSharper disable once InconsistentNaming
public static class HARCompat
{
    public static bool Active;
    public static bool EnforceRestrictions = true;
    private static Action<Rect> doRaceTabs;
    private static Action<Pawn> constructorPostfix;
    private static Func<IEnumerable<HeadTypeDef>, Pawn, IEnumerable<HeadTypeDef>> headTypeFilter;
    private static Type thingDef_AlienRace;
    private static AccessTools.FieldRef<object, object> alienRace;
    private static AccessTools.FieldRef<object, object> alienPartGenerator;
    private static AccessTools.FieldRef<object, object> generalSettings;

    private static AccessTools.FieldRef<object, List<BodyTypeDef>> bodyTypes;
    private static AccessTools.FieldRef<object, object> styleSettings;
    private static Func<object, StyleItemDef, Pawn, bool, bool> isValidStyle;

    private static Func<ThingDef, ThingDef, bool> canWear;
    private static Func<ThingDef, ThingDef, bool> canEquip;
    private static Func<TraitDef, Pawn, int, bool> canGetTrait;
    private static Func<GeneDef, ThingDef, bool, bool> canHaveGene;
    private static Func<XenotypeDef, ThingDef, bool> canUseXenotype;

    public static string Name = "Humanoid Alien Races";

    [UsedImplicitly]
    public static void Activate()
    {
        var stylingStation = AccessTools.TypeByName("AlienRace.StylingStation");
        doRaceTabs = AccessTools.Method(stylingStation, "DoRaceTabs", new[] { typeof(Rect) }).CreateDelegate<Action<Rect>>();
        constructorPostfix = AccessTools.Method(stylingStation, "ConstructorPostfix", new[] { typeof(Pawn) }).CreateDelegate<Action<Pawn>>();

        var patches = AccessTools.TypeByName("AlienRace.HarmonyPatches");
        headTypeFilter = AccessTools.Method(patches, "HeadTypeFilter").CreateDelegate<Func<IEnumerable<HeadTypeDef>, Pawn, IEnumerable<HeadTypeDef>>>();

        thingDef_AlienRace = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");
        alienRace = AccessTools.FieldRefAccess<object>(thingDef_AlienRace, "alienRace");
        var alienSettings = AccessTools.Inner(thingDef_AlienRace, "AlienSettings");
        generalSettings = AccessTools.FieldRefAccess<object>(alienSettings, "generalSettings");
        alienPartGenerator = AccessTools.FieldRefAccess<object>(AccessTools.TypeByName("AlienRace.GeneralSettings"), "alienPartGenerator");
        bodyTypes = AccessTools.FieldRefAccess<List<BodyTypeDef>>(AccessTools.TypeByName("AlienRace.AlienPartGenerator"), "bodyTypes");

        styleSettings = AccessTools.FieldRefAccess<object>(alienSettings, "styleSettings");
        isValidStyle = AccessTools.Method(AccessTools.TypeByName("AlienRace.StyleSettings"), "IsValidStyle")
            .CreateDelegateCasting<Func<object, StyleItemDef, Pawn, bool, bool>>();

        var raceRestrictionSettings = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
        canWear = AccessTools.Method(raceRestrictionSettings, "CanWear").CreateDelegate<Func<ThingDef, ThingDef, bool>>();
        canEquip = AccessTools.Method(raceRestrictionSettings, "CanEquip").CreateDelegate<Func<ThingDef, ThingDef, bool>>();
        canGetTrait = AccessTools.Method(raceRestrictionSettings, "CanGetTrait", new[] { typeof(TraitDef), typeof(Pawn), typeof(int) })
            .CreateDelegate<Func<TraitDef, Pawn, int, bool>>();
        canHaveGene = AccessTools.Method(raceRestrictionSettings, "CanHaveGene").CreateDelegate<Func<GeneDef, ThingDef, bool, bool>>();
        canUseXenotype = AccessTools.Method(raceRestrictionSettings, "CanUseXenotype").CreateDelegate<Func<XenotypeDef, ThingDef, bool>>();
    }

    public static void Notify_AppearanceEditorOpen(Pawn pawn)
    {
        constructorPostfix(pawn);
    }

    public static void DoRaceTabs(Rect inRect)
    {
        doRaceTabs(inRect);
    }

    public static IEnumerable<HeadTypeDef> FilterHeadTypes(IEnumerable<HeadTypeDef> headTypes, Pawn pawn) => headTypeFilter(headTypes, pawn);

    public static List<BodyTypeDef> AllowedBodyTypes(Pawn pawn)
    {
        if (thingDef_AlienRace.IsInstanceOfType(pawn.def))
        {
            var obj = alienRace(pawn.def);
            if (obj == null) return null;
            obj = generalSettings(obj);
            if (obj == null) return null;
            obj = alienPartGenerator(obj);
            if (obj == null) return null;
            return bodyTypes(obj);
        }

        return null;
    }

    public static bool AllowStyleItem(StyleItemDef item, Pawn pawn)
    {
        if (thingDef_AlienRace.IsInstanceOfType(pawn.def))
        {
            var obj = alienRace(pawn.def);
            if (obj == null) return true;
            var settings = styleSettings(obj);
            if (settings is not Dictionary<Type, object> dict || !dict.TryGetValue(item.GetType(), out var typeSettings)) return true;
            return isValidStyle(typeSettings, item, pawn, false);
        }

        return true;
    }

    public static bool CanWear(ThingDef apparel, Pawn pawn) => canWear(apparel, pawn.def);
    public static bool CanEquip(ThingDef weapon, Pawn pawn) => canEquip(weapon, pawn.def);
    public static bool CanGetTrait(ListingMenu_Trait.TraitInfo trait, Pawn pawn) => canGetTrait(trait.Trait.def, pawn, trait.Trait.degree);
    public static bool CanHaveGene(GeneDef gene, Pawn pawn) => canHaveGene(gene, pawn.def, false);
    public static bool CanUseXenotype(XenotypeDef xenotype, Pawn pawn) => canUseXenotype(xenotype, pawn.def);
}