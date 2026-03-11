using System;
using System.Collections.Generic;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

public static partial class Widgets
{
    public const float CarrouselCellHeight = 88f;

    public static void Carrousel<T>(Rect rect, List<T> items, ref Vector2 scrollPos, T selected, Action<T> clickAction,
        Func<T, Texture> textureFunc, Color color,
        Func<T, string>? hoverLabel = null) where T : Def
    {
        var itemSize = new Vector2(68f, 68f);
        const float itemSpacing = 4f;
        var visibleItems =
            Mathf.FloorToInt((rect.width - 64f) / (itemSize.x + itemSpacing)); // - 64f for the left and right buttons.
        visibleItems = Math.Min(items.Count, visibleItems);
        var additionalWidth = items.Count > visibleItems ? UIUtility.ScrollBarWidth + 4f : 0f;

        var rowRect = rect.TakeTopPart(itemSize.y);
        rowRect = rowRect.TakeLeftPart(64f + visibleItems * (itemSize.x + itemSpacing));
        var buttonLeftRight = rowRect.TakeLeftPart(32f).CenteredVertically(32f);
        var buttonRightRect = rowRect.TakeRightPart(32f).CenteredVertically(32f);
        rowRect.yMax += additionalWidth;
        var outRect =
            new Rect(rowRect.x, rowRect.y, rowRect.width,
                itemSize.y + additionalWidth + 4f); // outRect is the size of the viewable area.
        var viewRect = new Rect(rowRect.x, rowRect.y, (itemSize.x + itemSpacing) * items.Count - itemSpacing,
            itemSize.y); // Calculate the width of the viewRect. -1 to remove the spacing of the last item.

        Verse.Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
        foreach (var item in items)
        {
            var r = viewRect.TakeLeftPart(itemSize.x);
            var iconRect = r.ContractedBy(4f);
            viewRect.xMin += itemSpacing;
            Verse.Widgets.DrawHighlightIfMouseover(iconRect);
            Verse.Widgets.DrawHighlight(iconRect);

            using (new GUIColor(color))
            {
                GUI.DrawTexture(iconRect, textureFunc(item));
            }

            if (Verse.Widgets.ButtonInvisible(r))
            {
                clickAction(item);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            if (Mouse.IsOver(r) && hoverLabel != null) TooltipHandler.TipRegion(r, hoverLabel(item));

            if (Equals(item, selected)) Verse.Widgets.DrawBox(r);
        }

        Verse.Widgets.EndScrollView();

        // Left and right buttons
        var nextIndex = items.IndexOf(selected);
        var leftClicked = false;
        var rightClicked = false;
        if (Verse.Widgets.ButtonImage(buttonLeftRight, TexPawnEditor.ArrowLeft))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            nextIndex = items.IndexOf(selected) - 1;
            leftClicked = true;
        }
        else if (Verse.Widgets.ButtonImage(buttonRightRect, TexPawnEditor.ArrowRight))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            nextIndex = items.IndexOf(selected) + 1;
            rightClicked = true;
        }

        if (!leftClicked && !rightClicked) return;
        var xMaxIndex = (nextIndex + 1) * (itemSize.x + itemSpacing); // + 1 because 0 is also valid.
        var xMaxScroll = scrollPos.x + outRect.width;
        var xMinIndex = nextIndex * (itemSize.x + itemSpacing);
        var xMinScroll = scrollPos.x;


        if (nextIndex == -1)
        {
            scrollPos.x = viewRect.xMax; // Wrap left to right
            nextIndex = items.Count - 1;
        }
        else if (xMinIndex < xMinScroll)
        {
            scrollPos.x = xMinIndex; // Move left
        }

        if (nextIndex == items.Count)
        {
            scrollPos.x = 0; // Wrap right to left
            nextIndex = 0;
        }
        else if (xMaxIndex > xMaxScroll)
        {
            scrollPos.x = xMaxIndex - outRect.width; // Move right
        }

        clickAction(items[nextIndex]);
    }
}