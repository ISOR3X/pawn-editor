using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static partial class Widgets
{
    public static void InspectPane(Rect inRect, Thing thing)
    {
        var label = InspectPaneUtility.AdjustedLabelFor([thing], inRect.TopPartPixels(50f));
        using (new TextBlock(GameFont.Medium, TextAnchor.UpperLeft))
        {
            Verse.Widgets.Label(inRect.TakeTopPart(50f), label);
        }

        inRect.yMin -= 24f;
        InspectPaneFiller.DoPaneContentsFor(thing, inRect);
    }
}