using System.Runtime.CompilerServices;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Taffy;

namespace PawnEditor.v2;

public class SectionWorker_Info : SectionWorker<Thing>
{
    public override void DoSectionContents(UIBranch b, Thing thing)
    {
        b.Text(AdjustedLabelFor(thing), style: new Style { fontSize = GameFont.Medium });

        if (thing is Pawn pawn)
        {
            b.Div(b2 =>
            {
                DrawInspectPaneWidget(b2, InspectPaneFiller.DrawHealth, pawn);
                if (pawn.IsGhoul && pawn.needs.food != null) DrawInspectPaneWidget(b2, InspectPaneFiller.DrawHunger, pawn);
                else DrawInspectPaneWidget(b2, InspectPaneFiller.DrawMood, pawn);
                if (pawn is { timetable: not null, IsPrisonerOfColony: false })
                    DrawInspectPaneWidget(b2, InspectPaneFiller.DrawTimetableSetting, pawn);
                if (pawn.needs?.energy != null)
                    DrawInspectPaneWidget(b2, InspectPaneFiller.DrawMechEnergy, pawn);
            }, style: new Style { gap = new TaffyAxes(Dimension.Px(GenUI.GapSmall), Dimension.Px(GenUI.GapTiny)) });
            b.Text(MakeInspectStringFor(pawn));
        }
    }

    private static string AdjustedLabelFor(Thing thing)
    {
        // The rect is only used to potentially clamp the text, but this is already handled by taffy text as well,
        // so we can just pass a dummy rect.
        var r = new Rect(0, 0, 999, 0);
        return InspectPaneUtility.AdjustedLabelFor([thing], r);
    }

    private static void DrawInspectPaneWidget(UIBranch b, Action<WidgetRow, Pawn> draw, Pawn pawn)
    {
        b.Div(draw: r => draw(new WidgetRow(r.x, r.y), pawn), style: new Style { width = Dimension.Px(93f), height = Dimension.Px(16f) });
    }

    private static string MakeInspectStringFor(Pawn pawn)
    {
        var str = pawn.GetInspectString();

        var stringLowPriority = pawn.GetInspectStringLowPriority();
        if (!stringLowPriority.NullOrEmpty())
        {
            if (!str.NullOrEmpty())
                str = str.TrimEndNewlines() + "\n";
            str += stringLowPriority;
        }
        return str;
    }
}
