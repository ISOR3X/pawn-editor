using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_NPCFactionOverview : TabWorker_FactionOverview
{
    public override void DrawTabContents(Rect inRect, Faction faction)
    {
        DoHeading(inRect.TakeTopPart(35), faction);
        DoBottomButtons(inRect.TakeBottomPart(40));
        DrawPawnTables(inRect, faction);
    }

    private static void DoBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(150).ContractedBy(5), "PawnEditor.AddColonist".Translate())) PawnEditor.AddPawn(PawnCategory.Humans);
        DoSearch(ref inRect);
    }

    public override IEnumerable<FloatMenuOption> GetRandomizationOptions(Faction faction)
    {
        foreach (var option in base.GetRandomizationOptions(faction)) yield return option;

        yield return new("PawnEditor.FactionRep".Translate(),
            () =>
            {
                var relation = faction.RelationWith(Faction.OfPlayer);
                relation.baseGoodwill = Rand.Range(-200, 200);
                relation.CheckKindThresholds(faction, false, "DEBUG", GlobalTargetInfo.Invalid, out _);
                faction.SetRelation(relation);
                var relation2 = Faction.OfPlayer.RelationWith(faction);
                var kind = relation2.kind;
                relation2.baseGoodwill = relation.baseGoodwill;
                relation2.kind = relation.kind;
                if (kind != relation2.kind) Faction.OfPlayer.Notify_RelationKindChanged(faction, kind, false, null, GlobalTargetInfo.Invalid, out _);
            });
    }

    private static void DoHeading(Rect inRect, Faction faction)
    {
        var factionName = "PawnEditor.FactionName".Translate();
        var factionReputation = "PawnEditor.FactionRep".Translate();
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var leftLeftWidth = UIUtility.ColumnWidth(6, factionName);
            var rightLeftWidth = UIUtility.ColumnWidth(6, factionReputation);
            var factionNameRect = inRect.LeftHalf();
            factionNameRect.width *= 0.75f;
            Widgets.Label(factionNameRect.TakeLeftPart(leftLeftWidth), factionName);
            faction.Name = Widgets.TextField(factionNameRect, faction.Name);

            if (faction.HasGoodwill && !faction.def.permanentEnemy)
            {
                var factionReputationRect = inRect.RightHalf();
                factionReputationRect.width *= 0.75f;
                Widgets.Label(factionReputationRect.TakeLeftPart(rightLeftWidth), factionReputation);
                float goodwill = faction.PlayerGoodwill;
                var oldGoodwill = faction.PlayerGoodwill;
                Widgets.HorizontalSlider(factionReputationRect, ref goodwill, new(-200, 200),
                    $"{faction.PlayerRelationKind.GetLabelCap()} ({goodwill})".Colorize(faction.PlayerRelationKind.GetColor()));
                if (Mathf.RoundToInt(goodwill) != oldGoodwill)
                {
                    var relation = faction.RelationWith(Faction.OfPlayer);
                    relation.baseGoodwill = Mathf.RoundToInt(goodwill);
                    relation.CheckKindThresholds(faction, false, "DEBUG", GlobalTargetInfo.Invalid, out _);
                    faction.SetRelation(relation);
                    var relation2 = Faction.OfPlayer.RelationWith(faction);
                    var kind = relation2.kind;
                    relation2.baseGoodwill = relation.baseGoodwill;
                    relation2.kind = relation.kind;
                    if (kind != relation2.kind) Faction.OfPlayer.Notify_RelationKindChanged(faction, kind, false, null, GlobalTargetInfo.Invalid, out _);
                }
            }
        }
    }
}
