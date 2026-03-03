using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public static class QuickActionUtility
{
    public static Dictionary<string, List<(QuickActionAttribute, MethodInfo)>> actions = new();

    static QuickActionUtility()
    {
        GenerateCacheForMethod();
    }

    private static void GenerateCacheForMethod()
    {
        foreach (var method in GenTypes.AllTypes.SelectMany(allType =>
                     allType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)))
            if (method.TryGetAttribute(out QuickActionAttribute attribute))
                actions.TryAddToValueList(attribute.tabDefName, (attribute, method));
    }

    extension(QuickActionAttribute attribute)
    {
        public FloatMenuOption ToFloatMenuOption(MethodInfo methodInfo)
        {
            var str = string.IsNullOrEmpty(attribute.name) ? GenText.SplitCamelCase(methodInfo.Name) : attribute.name;
            attribute.action = (Action)Delegate.CreateDelegate(typeof(Action), methodInfo);
            return new FloatMenuOption(str, () => attribute.action(), orderInPriority: attribute.displayPriority);
        }

        public bool CanUseQuickAction()
        {
            var correctProgramState = !attribute.hideAtEntryProgramState || Current.ProgramState != ProgramState.Entry;
            var isRequiredDlcPresent = (!attribute.requiresRoyalty || ModsConfig.RoyaltyActive) &&
                                       (!attribute.requiresIdeology || ModsConfig.IdeologyActive) &&
                                       (!attribute.requiresBiotech || ModsConfig.BiotechActive) &&
                                       (!attribute.requiresAnomaly || ModsConfig.AnomalyActive);
            return correctProgramState && isRequiredDlcPresent;
        }
    }
}