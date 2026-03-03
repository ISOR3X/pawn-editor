using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class Window_Editor
{
    public static void TrySelect([CanBeNull] Faction faction)
    {
        if (selectedFaction == faction) return;
        selectedFaction = faction;
        TryRecachePawnGroup();
        TrySelect(selectedPawnGroup.FirstOrDefault());
    }

    public static void TrySelect([CanBeNull] Pawn pawn)
    {
        if (pawn == selectedPawn) return;
        var currentSelectedPawn = selectedPawn;
        selectedPawn = pawn;

        // selectedTabDef?.Worker.Notify_ContentChanged();
        // secondarySelectedTabDef?.Worker.Notify_ContentChanged();

        if (pawn?.Faction != selectedPawn?.Faction)
        {
            selectedFaction = pawn?.Faction;
            TryRecachePawnGroup();
        }

        if (PawnUtility.GetPawnCategory(pawn) != PawnUtility.GetPawnCategory(currentSelectedPawn)) RecacheTabs();
    }

    [CanBeNull]
    public static Pawn GetSelectedPawn()
    {
        return selectedPawn;
    }

    [CanBeNull]
    public static Faction GetSelectedFaction()
    {
        return selectedFaction;
    }

    public static void RecacheTabs()
    {
        selectedTabDefsForPawn.Clear();
        selectedTabDefsForPawn = TabUtility.GetTabDefsForPawn(selectedPawn);
        selectedTabDef = selectedTabDefsForPawn.First();
        selectedTabDef.Worker.Notify_ContentChanged();
        var tab = selectedTabDefsForPawn.ElementAtOrDefault(1);
        secondarySelectedTabDef = tab;
        if (tab != null) secondarySelectedTabDef.Worker.Notify_ContentChanged();
        
    }

    public static void TryRecachePawnGroup()
    {
        bool flag = selectedFaction == null ? selectedPawnGroup == Pawns_NoFaction : selectedPawnGroup == Pawns_ByFaction[selectedFaction];
        if (!flag)
        {
            selectedPawnGroup = selectedFaction == null ? Pawns_NoFaction : Pawns_ByFaction[selectedFaction];
        }
    }

    private static FloatMenu FactionFloatMenu()
    {
        var playerFaction = Pawns_ByFaction.Keys.First(f => f.IsPlayer);
        var playerOption = new FloatMenuOption(
            playerFaction.Name,
            delegate { TrySelect(faction: playerFaction); })
        {
            iconTex = GetFactionIcon(playerFaction),
            iconColor = playerFaction.Color,
            orderInPriority = 100
        };
        var (nullLabel, nullIcon, nullPriority) = GetFactionInfo(null);
        var nullOption = new FloatMenuOption(nullLabel, delegate { TrySelect(faction: null); })
        {
            iconTex = nullIcon,
            iconColor = Color.white,
            orderInPriority = nullPriority
        };

        return new FloatMenu(Find.FactionManager.AllFactions
            .Where(faction => !faction.IsPlayer)
            .OrderBy(faction => GetFactionInfo(faction).Item1)
            .Select(faction =>
            {
                var (label, icon, priority) = GetFactionInfo(faction);
                return new FloatMenuOption(label, delegate { TrySelect(faction); })
                {
                    iconTex = icon,
                    iconColor = faction.Color,
                    orderInPriority = priority
                };
            }).Append(playerOption).Append(nullOption).ToList());
        (string, Texture2D, int) GetFactionInfo(Faction faction)
        {
            int priority = 0;
            int pawnCount;
            string label = "Wildlife";
            if (faction != null)
            {
                label = faction.def == FactionDefOf.Ancients ? faction.def.LabelCap : faction.Name;
                pawnCount = Pawns_ByFaction[faction].Count;
                priority = pawnCount == 0 ? -1 : priority; // To ensure that empty factions are always at the bottom, excluding the wildlife faction.
            }
            else
            {
                pawnCount = Pawns_NoFaction.Count;
            }

            return (label.Colorize(pawnCount == 0 ? ColoredText.SubtleGrayColor : Color.white), GetFactionIcon(faction), priority);
        }

        Texture2D GetFactionIcon(Faction faction)
        {
            Texture2D icon = faction != null ? faction.def.FactionIcon : Widgets.PlaceholderIconTex;
            icon = icon == BaseContent.BadTex ? Widgets.PlaceholderIconTex : icon;
            return icon;
        }
    }
}