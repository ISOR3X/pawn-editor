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

    public static string ReadableDefName(this Def def)
    {
        var parts = def.defName.Split('_');
        var result = parts.Select(part => Regex.Replace(part, @"(?<=[a-z])(?=[A-Z])", " ").ToLower());
        return string.Join(", ", result).CapitalizeFirst();
    }

    public static string ReadableCamelCase(this string text)
    {
        var result = Regex.Replace(text, "(?<!^)([A-Z])", " $1");
        return result.ToLower().CapitalizeFirst();
    }
}