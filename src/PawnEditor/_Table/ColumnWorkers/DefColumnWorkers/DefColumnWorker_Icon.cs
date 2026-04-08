using HotSwap;
using PawnEditor.Table;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class DefColumnWorker_Icon : DefColumnWorker
{
    public override void DrawCell(TaffyBuilder grid, Def row)
    {
        grid.Item(draw: r =>
        {
            var cellRect = r.ContractedBy(2f);
            var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
            if (row is HairDef or BeardDef)
                GUI.color = pawn != null ? pawn.story.HairColor : PawnHairColors.DarkReddish;
            Verse.Widgets.DefIcon(cellRect, row, scale: 1f);
            GUI.color = Color.white;
        });
    }
}
