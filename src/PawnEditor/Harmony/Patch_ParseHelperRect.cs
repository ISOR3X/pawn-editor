using System.Globalization;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace PawnEditor;

/// <summary>
/// Fixes Rect serialization: Rect.ToString() emits "(x:N, y:N, width:N, height:N)" but
/// ParseHelper.FromStringRect expects a different format and throws a FormatException.
/// We replace it with a parser that handles Unity's labelled format.
/// </summary>
[HarmonyPatch(typeof(ParseHelper), nameof(ParseHelper.FromStringRect))]
public static class Patch_ParseHelperRect
{
    public static bool Prefix(string str, ref Rect __result)
    {
        str = str.Trim('(', ')').Trim();
        var parts = str.Split(',');
        __result = new Rect(ParseValue(parts[0]), ParseValue(parts[1]), ParseValue(parts[2]), ParseValue(parts[3]));
        return false;

        static float ParseValue(string part)
        {
            var colon = part.IndexOf(':');
            var num = colon >= 0 ? part[(colon + 1)..].Trim() : part.Trim();
            return float.Parse(num, CultureInfo.InvariantCulture);
        }
    }
}
