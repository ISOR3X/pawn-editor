using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_FactionSettlements : TabWorker<Faction>
{
    private readonly Dictionary<Settlement, int> distances = new();
    private UITable<Faction> settlementTable;

    public override void Initialize()
    {
        base.Initialize();
        settlementTable = new(new()
        {
            new("PawnEditor.Settlements".Translate()),
            new("PawnEditor.DistanceFromColony".Translate()),
            new(80),
            new(30)
        }, faction => Find.WorldObjects.Settlements.Where(s => s.Faction == faction)
           .Select(s => new UITable<Faction>.Row(new List<UITable<Faction>.Row.Item>
            {
                new(rect => { s.Name = Widgets.TextField(rect, s.Name); }),
                new(distances.TryGetValue(s, out var dist) ? dist.ToStringTicksToDays() : ""),
                new("JumpToTargetCustom".Translate("..."), () =>
                {
                    Find.WindowStack.RemoveWindowsOfType(typeof(Dialog_PawnEditor_InGame));
                    CameraJumper.TryJumpAndSelect(s, CameraJumper.MovementMode.Cut);
                }),
                new(TexButton.DeleteX, () =>
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PawnEditor.ReallyDelete".Translate(s.Name),
                        () =>
                        {
                            s.Destroy();
                            Find.WorldObjects.Remove(s);
                        }, true));
                })
            }, s.Name + "\n\n" + s.GetInspectString())));
    }

    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoBottomButtons(inRect.TakeBottomPart(40), faction);
        settlementTable.OnGUI(inRect, faction);
    }

    private void DoBottomButtons(Rect inRect, Faction faction)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(300).ContractedBy(5), "PawnEditor.DistanceFromColony.Calculate".Translate()))
        {
            distances.Clear();
            var startingTile = Find.CurrentMap.Tile;
            var pawns = Find.CurrentMap.mapPawns.FreeColonists;
            var caravanInfo = new CaravanTicksPerMoveUtility.CaravanInfo
            {
                pawns = pawns,
                massUsage = CollectionsMassCalculator.MassUsage(pawns, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload),
                massCapacity = CollectionsMassCalculator.Capacity(pawns)
            };
            foreach (var settlement in Find.WorldObjects.Settlements.Where(s => s.Faction == faction))
                using (var worldPath = Find.WorldPathFinder.FindPath(startingTile, settlement.Tile, null))
                    distances.Add(settlement, CaravanArrivalTimeEstimator.EstimatedTicksToArrive(startingTile, settlement.Tile, worldPath, 0f,
                        CaravanTicksPerMoveUtility.GetTicksPerMove(caravanInfo), Find.TickManager.TicksAbs));
            settlementTable.ClearCache();
        }
    }
}
