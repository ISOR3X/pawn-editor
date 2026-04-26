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

        if (_selectedPawn?.Faction != _selectedFaction && PawnLister.Pawns_ByFaction[faction].Count > 0)
            TrySelect(_selectedPawnGroup.FirstOrDefault());
    }

    public void TrySelect(Pawn? pawn)
    {
        if (_selectedPawn == pawn) return;

        var prevFaction = _selectedPawn?.Faction;
        var prevCategory = PawnUtility.GetPawnCategory(_selectedPawn);

        _selectedPawn = pawn;
        _currentContext = pawn != null ? new PawnContext(pawn) : null;

        _selectedTabDef?.Worker.Notify_ContentChanged();

        if (pawn?.Faction != prevFaction && pawn != null) TrySelect(pawn.Faction);

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

    private static void RecacheTabs()
    {
        _selectedTabDefsFor.Clear();
        _selectedTabDefsFor = TabUtility.GetTabDefsFor(_currentContext);
        _selectedTabDef = _selectedTabDefsFor.FirstOrDefault();
        _selectedTabDef?.Worker.Notify_ContentChanged();
    }

    private static void TryRecachePawnGroup()
    {
        _selectedPawnGroup.Clear();
        var byFaction = PawnLister.Pawns_ByFaction;
        // _selectedFaction is static and can therefore be a stale reference on game change.
        if (!byFaction.ContainsKey(_selectedFaction))
            _selectedFaction = null;
        _selectedPawnGroup.AddRange(byFaction[_selectedFaction]);
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