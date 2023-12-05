using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using MonoMod.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[ModCompat("erdelf.humanoidalienraces", "erdelf.humanoidalienraces.dev")]
// ReSharper disable once InconsistentNaming
public static class HARCompat
{
    public static bool Active;
    private static Action<Rect> doRaceTabs;
    private static Action<Pawn> constructorPostfix;
    private static Func<IEnumerable<HeadTypeDef>, Pawn, IEnumerable<HeadTypeDef>> headTypeFilter;
    private static Type thingDef_AlienRace;
    private static AccessTools.FieldRef<object, object> alienRace;
    private static AccessTools.FieldRef<object, object> alienPartGenerator;
    private static AccessTools.FieldRef<object, object> generalSettings;

    private static AccessTools.FieldRef<object, List<BodyTypeDef>> bodyTypes;
//    private static AccessTools.FieldRef<object, object> styleSettings;
//    private static Func<object, StyleItemDef, Pawn, bool, bool> isValidStyle;

    public static string Name = "Humanoid Alien Races";

    [UsedImplicitly]
    public static void Activate()
    {
        var type = AccessTools.TypeByName("AlienRace.StylingStation");
        doRaceTabs = AccessTools.Method(type, "DoRaceTabs", new[] { typeof(Rect) }).CreateDelegate<Action<Rect>>();
        constructorPostfix = AccessTools.Method(type, "ConstructorPostfix", new[] { typeof(Pawn) }).CreateDelegate<Action<Pawn>>();
        type = AccessTools.TypeByName("AlienRace.HarmonyPatches");
        headTypeFilter = AccessTools.Method(type, "HeadTypeFilter").CreateDelegate<Func<IEnumerable<HeadTypeDef>, Pawn, IEnumerable<HeadTypeDef>>>();
//        type = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");

        type = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");
        thingDef_AlienRace = type;
        alienRace = AccessTools.FieldRefAccess<object>(type, "alienRace");
        type = AccessTools.Inner(type, "AlienSettings");
        generalSettings = AccessTools.FieldRefAccess<object>(type, "generalSettings");
        type = AccessTools.TypeByName("AlienRace.GeneralSettings");
        alienPartGenerator = AccessTools.FieldRefAccess<object>(type, "alienPartGenerator");
//        styleSettings = AccessTools.FieldRefAccess<object>(type, "styleSettings");
        type = AccessTools.TypeByName("AlienRace.AlienPartGenerator");
        bodyTypes = AccessTools.FieldRefAccess<List<BodyTypeDef>>(type, "bodyTypes");
//        type = AccessTools.TypeByName("AlienRace.StyleSettings");
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

//    public static bool AllowStyleItem(StyleItemDef item, Pawn pawn)
//    {
//
//    }
}
