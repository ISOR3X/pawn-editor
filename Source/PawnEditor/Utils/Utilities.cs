using System.Collections.Generic;

namespace PawnEditor;

public static class Utilities
{
    public static void Set<T>(this List<T> list, int index, T item)
    {
        while (list.Count <= index) list.Add(default);
        list[index] = item;
    }
}
