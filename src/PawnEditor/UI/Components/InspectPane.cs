using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class UIComponents
{
    public static void InspectPane(Rect inRect, Thing thing)
    {
        var label = InspectPaneUtility.AdjustedLabelFor([thing], inRect.TopPartPixels(50f));
        using (new TextBlock(GameFont.Medium, TextAnchor.UpperLeft))
        {
            Widgets.Label(inRect.TakeTopPart(50f), label);
        }

        inRect.yMin -= 24f;
        InspectPaneFiller.DoPaneContentsFor(thing, inRect);
    }
}