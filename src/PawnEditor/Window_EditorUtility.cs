using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class Window_Editor
{
    private static void TrySelect(Faction? faction)
    {
        if (selectedFaction == faction) return;
        selectedFaction = faction;
        TryRecachePawnGroup();
        TrySelect(selectedPawnGroup.FirstOrDefault());
    }

    public static void TrySelect(Pawn? pawn)
    {
        if (pawn == selectedPawn) return;
        var prevSelectedPawn = selectedPawn;
        selectedPawn = pawn;

        // selectedTabDef?.Worker.Notify_ContentChanged();
        // secondarySelectedTabDef?.Worker.Notify_ContentChanged();

        if (selectedPawn?.Faction != prevSelectedPawn?.Faction)
        {
            selectedFaction = pawn?.Faction;
            TryRecachePawnGroup();
        }

        if (PawnUtility.GetPawnCategory(pawn) != PawnUtility.GetPawnCategory(prevSelectedPawn)) RecacheTabs();
    }

    public static Pawn? GetSelectedPawn()
    {
        return selectedPawn;
    }

    public static Faction? GetSelectedFaction()
    {
        return selectedFaction;
    }

    private static void RecacheTabs()
    {
        selectedTabDefsForPawn.Clear();
        selectedTabDefsForPawn = TabUtility.GetTabDefsForPawn(selectedPawn);
        selectedTabDef = selectedTabDefsForPawn.FirstOrDefault();
        selectedTabDef?.Worker.Notify_ContentChanged();
        var tab = selectedTabDefsForPawn.ElementAtOrDefault(1);
        secondarySelectedTabDef = tab;
        if (tab != null && secondarySelectedTabDef != null) secondarySelectedTabDef.Worker.Notify_ContentChanged();
    }

    private static void TryRecachePawnGroup()
    {
        var flag = selectedFaction == null
            ? selectedPawnGroup == Pawns_NoFaction
            : selectedPawnGroup == Pawns_ByFaction[selectedFaction];
        if (!flag) selectedPawnGroup = selectedFaction == null ? Pawns_NoFaction : Pawns_ByFaction[selectedFaction];
    }

    private static FloatMenu FactionFloatMenu()
    {
        var playerFaction = Pawns_ByFaction.Keys.First(f => f.IsPlayer);
        var playerOption = new FloatMenuOption(
            playerFaction.Name,
            () => TrySelect(playerFaction),
            GetFactionIcon(playerFaction),
            playerFaction.Color,
            orderInPriority: 100
        );
        var (nullLabel, nullIcon, nullPriority) = GetFactionInfo(null);
        var nullOption = new FloatMenuOption(nullLabel, () => TrySelect(faction: null), nullIcon, Color.white,
            orderInPriority: nullPriority);

        return new FloatMenu(Find.FactionManager.AllFactions
            .Where(faction => !faction.IsPlayer)
            .OrderBy(faction => GetFactionInfo(faction).Item1)
            .Select(faction =>
            {
                var (label, icon, priority) = GetFactionInfo(faction);
                return new FloatMenuOption(label, () => TrySelect(faction), icon, faction.Color,
                    orderInPriority: priority);
            }).Append(playerOption).Append(nullOption).ToList());

        (string, Texture2D, int) GetFactionInfo(Faction? faction)
        {
            var priority = 0;
            int pawnCount;
            var label = "Wildlife";
            if (faction != null)
            {
                label = faction.def == FactionDefOf.Ancients ? faction.def.LabelCap : faction.Name;
                Pawns_ByFaction.TryGetValue(faction, out var pawnsInFaction);
                pawnCount = pawnsInFaction?.Count ?? 0;
                priority = pawnCount == 0
                    ? -1
                    : priority; // To ensure that empty factions are always at the bottom, excluding the wildlife faction.
            }
            else
            {
                pawnCount = Pawns_NoFaction.Count;
            }

            return (label.Colorize(pawnCount == 0 ? ColoredText.SubtleGrayColor : Color.white), GetFactionIcon(faction),
                priority);
        }

        Texture2D GetFactionIcon(Faction? faction)
        {
            var icon = faction != null ? faction.def.FactionIcon : Widgets.PlaceholderIconTex;
            icon = icon == BaseContent.BadTex ? Widgets.PlaceholderIconTex : icon;
            return icon;
        }
    }
}