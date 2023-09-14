using System;
using System.Collections.Generic;
using System.Linq;

namespace PawnEditor;

public static class Utilities
{
    public static void Set<T>(this List<T> list, int index, T item)
    {
        while (list.Count <= index) list.Add(default);
        list[index] = item;
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
}
