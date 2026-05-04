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
        SelectedFactionWeak.SetTarget(faction);


        if (SelectedPawn?.Faction != _selectedFaction && PawnLister.Pawns_ByFaction[faction].Count > 0)
            TrySelect(PawnLister.Pawns_ByFaction[faction].FirstOrDefault());
    }

    public void TrySelect(Pawn? pawn)
    {
        if (SelectedPawn == pawn) return;

        var prevFaction = SelectedPawn?.Faction;
        var prevCategory = PawnUtility.GetPawnCategory(SelectedPawn);

        _currentContext = pawn != null ? new PawnContext(pawn) : null;
        SelectedContextWeak.SetTarget(_currentContext);

        _selectedTabDef?.Worker.Notify_ContentChanged();

        if (pawn?.Faction != prevFaction && pawn != null) TrySelect(pawn.Faction);

        if (PawnUtility.GetPawnCategory(pawn) != prevCategory) RecacheTabs();
    }

    private void RecacheTabs()
    {
        _selectedTabDefsFor.Clear();
        _selectedTabDefsFor = TabUtility.GetTabDefsFor(_currentContext);
        _selectedTabDef = _selectedTabDefsFor.FirstOrDefault();
        _selectedTabDef?.Worker.Notify_ContentChanged();
    }

    private FloatMenu FactionFloatMenu()
    {
        var pawnsByFaction = PawnLister.Pawns_ByFaction;
        var playerFaction = Faction.OfPlayer;
        var playerOption = new FloatMenuOption(
            playerFaction.Name,
            () => TrySelect(playerFaction),
            playerFaction.def.FactionIcon,
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
            var (label, icon, _) = FactionUtility.GetFactionMeta(faction);
            if (faction != null)
            {
                // TODO: Why was this Ancient check needed? Potentially because there are two types of ancients?
                pawnsByFaction.TryGetValue(faction, out var pawnsInFaction);
                pawnCount = pawnsInFaction?.Count ?? 0;
                priority = pawnCount == 0
                    ? -1
                    : priority; // To ensure that empty factions are always at the bottom, excluding the wildlife faction.
            }

            return (label.Colorize(pawnCount == 0 ? ColoredText.SubtleGrayColor : Color.white), icon,
                priority);
        }
    }
}