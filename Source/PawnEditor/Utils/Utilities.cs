using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PawnEditor;

public static class Utilities
{
    public static bool SubMenuOpen = false;

    public static void Set<T>(this List<T> list, int index, T item)
    {
        while (list.Count <= index) list.Add(default);
        list[index] = item;
    }

    public static T Get<T>(this List<T> list, int index)
    {
        while (index >= list.Count) index -= list.Count;
        while (index < 0) index += list.Count;

        return list[index];
    }

    public static void Deconstruct<T>(this T[] items, out T t0)
    {
        t0 = items.Length > 0 ? items[0] : default;
    }

    public static void Deconstruct<T>(this T[] items, out T t0, out T t1)
    {
        t0 = items.Length > 0 ? items[0] : default;
        t1 = items.Length > 1 ? items[1] : default;
    }

    public static void Deconstruct<T>(this T[] items, out T t0, out T t1, out T t2)
    {
        t0 = items.Length > 0 ? items[0] : default;
        t1 = items.Length > 1 ? items[1] : default;
        t2 = items.Length > 2 ? items[2] : default;
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, HashSet<T> without) => source.Where(v => !without.Contains(v));

    public static Dictionary<TKey, TResult>
        SelectValues<TKey, TSource, TResult>(this Dictionary<TKey, TSource> source, Func<TKey, TSource, TResult> selector) =>
        source.Select(kv => new KeyValuePair<TKey, TResult>(kv.Key, selector(kv.Key, kv.Value))).ToDictionary(kv => kv.Key, kv => kv.Value);

    public static bool NotNullAndAny<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source != null && source.Any(predicate);

    public static float StepValue(float oldValuePct, float stepPct, float min = 0, float max = 1) =>
        Mathf.Clamp((float)Math.Round(oldValuePct + stepPct, 2), min, max);

    public static T CreateDelegate<T>(this MethodInfo info) where T : Delegate => (T)info.CreateDelegate(typeof(T));

    public static T CreateDelegate<T>(this MethodInfo info, object target) where T : Delegate => (T)info.CreateDelegate(typeof(T), target);

    /// <summary>
    /// Converts CamelCase to a more readable format. Example: "CamelCase" -> "Camel case"
    /// </summary>
    /// <param name="input">The string to format.</param>
    /// <returns></returns>
    public static string ConvertCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new StringBuilder(input.Length * 2);
        result.Append(char.ToUpper(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
                result.Append(' ');

            result.Append(char.ToLower(input[i]));
        }

        return result.ToString();
    }
}