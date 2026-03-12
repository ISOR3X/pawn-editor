using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace PawnEditor;

public static class Utility
{
    public static bool HasDoneOnce = false; // Helper bool to execute code only once.

    /// <summary>
    ///     Adds a value to a list in a dictionary. If the key does not exist, it is created.
    /// </summary>
    public static void TryAddToValueList<T1, T2>(this Dictionary<T1, List<T2>> dict, T1 key, T2 value)
    {
        if (dict.ContainsKey(key))
            dict[key].Add(value);
        else
            dict[key] = [value];
    }

    public static List<T2> ToFlatList<T1, T2>(this SortedDictionary<T1, List<T2>> dict)
    {
        var list = new List<T2>();
        foreach (var kvp in dict) list.AddRange(kvp.Value);

        return list;
    }

    /// <summary>
    ///     Returns the total number of values in a SortedDictionary where the value is a List.
    /// </summary>
    /// <param name="dict">The dictionary to count the values of.</param>
    /// <typeparam name="T1">The key type of the dictionary.</typeparam>
    /// <typeparam name="T2">The type of the items in the value list dictionary.</typeparam>
    /// <returns></returns>
    public static int ValueCount<T1, T2>(this SortedDictionary<T1, List<T2>> dict)
    {
        return dict.Sum(kvp => kvp.Value.Count);
    }

    public static string ReadableDefName(this Def def)
    {
        var parts = def.defName.Split('_');
        var result = parts.Select(part => Regex.Replace(part, @"(?<=[a-z])(?=[A-Z])", " ").ToLower());
        return string.Join(", ", result).CapitalizeFirst();
    }
}