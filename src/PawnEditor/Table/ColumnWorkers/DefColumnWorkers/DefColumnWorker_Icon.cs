using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class DefColumnWorker_Icon : ColumnWorker_Icon<Def>
{
    protected override float MaxHeight => float.MaxValue;

    protected override void DrawIcon(Rect inRect, Def thing, TableWorker<Def> table)
    {
        var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (thing is HairDef or BeardDef)
            GUI.color = pawn != null ? pawn.story.HairColor : PawnHairColors.DarkReddish;
        Verse.Widgets.DefIcon(GetCellRect(inRect, table), thing, scale: 1f);
        GUI.color = Color.white;
    }
}