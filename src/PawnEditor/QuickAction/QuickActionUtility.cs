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
        foreach (var method in GenTypes.AllTypes.SelectMany(allType => allType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)))
        {
            if (method.TryGetAttribute(out QuickActionAttribute attribute))
            {
                actions.TryAddToValueList(attribute.tabDefName, (attribute, method));
            }
        }
    }

    public static FloatMenuOption ToFloatMenuOption(this QuickActionAttribute attribute, MethodInfo methodinfo)
    {
        string str = string.IsNullOrEmpty(attribute.name) ? GenText.SplitCamelCase(methodinfo.Name) : attribute.name;
        attribute.action = (Action)Delegate.CreateDelegate(typeof(Action), methodinfo);
        return new FloatMenuOption(str, () => attribute.action(), orderInPriority: attribute.displayPriority);
    }

    public static bool CanUseQuickAction(this QuickActionAttribute attribute)
    {
        bool correctProgramState = !attribute.hideAtEntryProgramState || Current.ProgramState != ProgramState.Entry;
        bool isRequiredDLCPresent = (!attribute.requiresRoyalty || ModsConfig.RoyaltyActive) && (!attribute.requiresIdeology || ModsConfig.IdeologyActive) &&
                                    (!attribute.requiresBiotech || ModsConfig.BiotechActive) && (!attribute.requiresAnomaly || ModsConfig.AnomalyActive);
        return correctProgramState && isRequiredDLCPresent;
    }
}