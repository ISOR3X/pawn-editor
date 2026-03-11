using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public static class FactionUtility
{
    public static (string, Texture2D, Color) GetFactionMeta(Faction? faction)
    {
        var label = "Wildlife";
        var tex = Verse.Widgets.PlaceholderIconTex;
        var color = Color.white;
        if (faction != null)
        {
            label = faction.def == FactionDefOf.Ancients ? faction.def.LabelCap : faction.Name;
            tex = faction.def.FactionIcon;
            tex = tex == BaseContent.BadTex ? Verse.Widgets.PlaceholderIconTex : tex;
            color = faction.Color;
        }

        return (label, tex, color);
    }

    public static TipSignal GetFactionTooltip(Faction? faction)
    {
        var name = faction?.Name ?? "Wildlife";
        var loadId = faction?.loadID ?? 0;
        var description = faction?.def?.Description ?? "";
        var label = faction?.def?.LabelCap.Resolve();

        // REF: FactionUIUtility.DrawFactionRow
        return new TipSignal(
            (Func<string>)(() => $"{name.Colorize(ColoredText.TipSectionTitleColor)}\n{label}\n\n{description}"),
            loadId ^ 1938473043);
    }

    public static void SetFaction(Pawn pawn, Faction faction)
    {
        if (pawn.Faction == faction) return;
        pawn.SetFaction(faction);

        var editorWindow = Find.WindowStack.Windows.OfType<Window_Editor>().FirstOrDefault();
        editorWindow?.TrySelect(pawn);
    }
}