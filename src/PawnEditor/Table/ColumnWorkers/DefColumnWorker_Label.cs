using System;
using System.Collections.Generic;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public class DefColumnWorker_Label : DefColumnWorker_Text
{
    private const int LeftMargin = 3;
    private const float IconScale = 1f;
    private static readonly Dictionary<string, string> LabelCache = new();
    private static float labelCacheForWidth = -1f;
    protected override TextAnchor LabelAlignment => TextAnchor.MiddleLeft;

    public override void DoCell(Rect inRect, Def thing, TableWorker<Def> table)
    {
        if (Def.showIcon)
        {
            var iconRect = inRect.TakeLeftPart(inRect.height);
            inRect.xMin += 8f;

            if (Def.iconBackground) Verse.Widgets.DrawHighlight(iconRect.ContractedBy(2f));

            // TODO: Why does this column need direct access to the pawn?
            var pawn = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();

            if (thing is HairDef or BeardDef)
                GUI.color = pawn != null ? pawn.story.HairColor : PawnHairColors.DarkReddish;

            Verse.Widgets.DefIcon(iconRect, thing, scale: IconScale);
            GUI.color = Color.white;
        }

        var str = GetTextFor(thing);
        if (Math.Abs(inRect.width - (double)labelCacheForWidth) > 0.1)
        {
            labelCacheForWidth = inRect.width;
            LabelCache.Clear();
        }

        if (Text.CalcSize(str.StripTags()).x > (double)inRect.width)
            str = str.StripTags().Truncate(inRect.width, LabelCache);
        using (new TextBlock(GameFont.Small, LabelAlignment, false))
        {
            Verse.Widgets.Label(inRect, str);
        }
    }

    public override int GetMinWidth(TableWorker<Def> table)
    {
        return Mathf.Max(base.GetMinWidth(table), Def.width);
    }

    public override string GetTextFor(Def thing)
    {
        return thing.label.CapitalizeFirst();
    }

    // public override int GetOptimalWidth(Table table) => Mathf.Clamp(50, GetMinWidth(table), GetMaxWidth(table));
}