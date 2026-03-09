using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class Window_Editor
{
    public void TrySelect(Faction? faction)
    {
        if (_selectedFaction == faction) return;
        _selectedFaction = faction;
        TryRecachePawnGroup();

        if (_selectedPawn?.Faction != _selectedFaction)
            TrySelect(selectedPawnGroup.FirstOrDefault());
    }

    public void TrySelect(Pawn? pawn)
    {
        if (_selectedPawn == pawn) return;

        var prevFaction = _selectedPawn?.Faction;
        var prevCategory = PawnUtility.GetPawnCategory(_selectedPawn);

        _selectedPawn = pawn;

        selectedTabDef?.Worker.Notify_ContentChanged();

        if (pawn?.Faction != prevFaction)
        {
            TrySelect(pawn?.Faction);
        }

        if (PawnUtility.GetPawnCategory(pawn) != prevCategory) RecacheTabs();
    }

    public Pawn? GetSelectedPawn()
    {
        return _selectedPawn;
    }

    public Faction? GetSelectedFaction()
    {
        return _selectedFaction;
    }

    private void RecacheTabs()
    {
        selectedTabDefsForPawn.Clear();
        selectedTabDefsForPawn = TabUtility.GetTabDefsForPawn(_selectedPawn);
        selectedTabDef = selectedTabDefsForPawn.FirstOrDefault();
        selectedTabDef?.Worker.Notify_ContentChanged();
    }

    private void TryRecachePawnGroup()
    {
        selectedPawnGroup.Clear();
        selectedPawnGroup.AddRange(PawnLister.Pawns_ByFaction[_selectedFaction ?? Faction.OfPlayer]);
    }

    private FloatMenu FactionFloatMenu()
    {
        var pawnsByFaction = PawnLister.Pawns_ByFaction;
        var playerFaction = Faction.OfPlayer;
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
            var pawnCount = 0;
            var label = "Wildlife";
            if (faction != null)
            {
                // TODO: Why was this Ancient check needed? Potentially because there are two types of ancients?
                label = faction.def == FactionDefOf.Ancients ? faction.def.LabelCap : faction.Name;
                pawnsByFaction.TryGetValue(faction, out var pawnsInFaction);
                pawnCount = pawnsInFaction?.Count ?? 0;
                priority = pawnCount == 0
                    ? -1
                    : priority; // To ensure that empty factions are always at the bottom, excluding the wildlife faction.
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