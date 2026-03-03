using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ColumnWorker_Label : ColumnWorker_Text
{
    private const int LeftMargin = 3;
    private const float IconScale = 1f;
    private static Dictionary<string, string> labelCache = new();
    private static float labelCacheForWidth = -1f;
    protected override TextAnchor LabelAlignment => TextAnchor.MiddleLeft;

    public override void DoCell(Rect inRect, Def thing, DefTable defTable)
    {
        if (def.showIcon)
        {
            Rect iconRect = inRect.TakeLeftPart(inRect.height);
            inRect.xMin += 8f;

            if (def.iconBackground) Widgets.DrawHighlight(iconRect.ContractedBy(2f));

            var pawn = Window_Editor.GetSelectedPawn();

            if (thing is HairDef or BeardDef)
            {
                GUI.color = pawn != null ? pawn.story.hairColor : PawnHairColors.DarkReddish;
            }

            Widgets.DefIcon(iconRect, thing, scale: IconScale);
            GUI.color = Color.white;
        }

        string str = GetTextFor(thing);
        if (inRect.width != (double)labelCacheForWidth)
        {
            labelCacheForWidth = inRect.width;
            labelCache.Clear();
        }

        if (Text.CalcSize(str.StripTags()).x > (double)inRect.width)
            str = str.StripTags().Truncate(inRect.width, labelCache);
        using (new TextBlock(GameFont.Small, LabelAlignment, false))
            Widgets.Label(inRect, str);
    }

    public override int GetMinWidth(DefTable defTable) => Mathf.Max(base.GetMinWidth(defTable), def.width);

    public override string GetTextFor(Def thing)
    {
        return thing.label.CapitalizeFirst();
    }

    // public override int GetOptimalWidth(Table table) => Mathf.Clamp(50, GetMinWidth(table), GetMaxWidth(table));
}