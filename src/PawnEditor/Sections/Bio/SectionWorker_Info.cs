using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Info(SectionDef def) : SectionWorker(def)
{
    /// <summary>
    /// Shadows <see cref="InspectPaneFiller.DoPaneContentsFor"/>, without interactive elements.
    /// </summary>
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text(AdjustedLabelFor(pawn), font: GameFont.Medium);
        builder.Div(new Style { gap = Taffy.Gap(GenUI.GapSmall, GenUI.GapTiny) }, row =>
        {
            DrawInspectPaneWidget(row, InspectPaneFiller.DrawHealth, pawn);
            if (pawn.IsGhoul && pawn.needs.food != null) DrawInspectPaneWidget(row, InspectPaneFiller.DrawHunger, pawn);
            else DrawInspectPaneWidget(row, InspectPaneFiller.DrawMood, pawn);
            if (pawn.timetable != null && !pawn.IsPrisonerOfColony)
                DrawInspectPaneWidget(row, InspectPaneFiller.DrawTimetableSetting, pawn);
            if (pawn.needs?.energy != null)
                DrawInspectPaneWidget(row, InspectPaneFiller.DrawMechEnergy, pawn);
        });
        builder.Text(MakeInspectStringFor(pawn));
    }

    private static string AdjustedLabelFor(Pawn pawn)
    {
        // The rect is only used to potentially clamp the text, but this is already handled by taffy text as well,
        // so we can just pass a dummy rect.
        var r = new Rect(0, 0, 999, 0);
        return InspectPaneUtility.AdjustedLabelFor([pawn], r);
    }

    private static void DrawInspectPaneWidget(TaffyBuilder builder, Action<WidgetRow, Pawn> draw, Pawn pawn)
    {
        builder.Item(new Style { size = new Size<Dimension>(93f, 16f) },
            draw: r => draw(new WidgetRow(r.x, r.y), pawn));
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