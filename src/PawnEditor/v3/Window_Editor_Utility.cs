using RimWorld;
using UnityEngine;
using Verse;
using Void.Native;

class Window_Editor_Utility
{

    public static string LabelFor(object obj)
    {
        if (obj is Faction faction) return faction.Name;
        // The rect is only used to potentially clamp the text, but this is already handled by taffy text as well,
        // so we can just pass a dummy rect.
        var r = new Rect(0, 0, 999, 0);
        return InspectPaneUtility.AdjustedLabelFor([obj], r);
    }

    public static void DrawIconFor(UIBranch b, object obj, Style? style = null)
    {
        if (obj is Thing t)
            b.Div(draw: r => Widgets.ThingIcon(r, t), style: style);
        if (obj is Faction f)
            b.Icon(f.def.FactionIcon, f.Color, style: style);
    }
}
