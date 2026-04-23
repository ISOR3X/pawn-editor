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
        var description = faction?.def?.Description ?? "";
        var label = faction?.def?.LabelCap.Resolve();

        // REF: FactionUIUtility.DrawFactionRow
        return new TipSignal($"{name.Colorize(ColoredText.TipSectionTitleColor)}\n{label}\n\n{description}");
    }

    public static void SetFaction(Pawn pawn, Faction faction)
    {
        if (pawn.Faction == faction) return;
        pawn.SetFaction(faction);

        var editorWindow = Find.WindowStack.Windows.OfType<Window_Editor>().FirstOrDefault();
        if (editorWindow == null) return;
        
        // Explicitly select faction. Just selecting the pawn again fails since it returns when the pawn is already selected.
        editorWindow.TrySelect(faction);
        editorWindow.TrySelect(pawn);
    }
}