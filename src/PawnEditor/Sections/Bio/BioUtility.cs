using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
[StaticConstructorOnStartup]
public static class BioUtility
{
    

    // REF: CharacterCardUtility.DrawCharacterCard
    public static void DoNameInputRect(Rect inRect, Pawn pawn, bool advanced = false)
    {
        // TODO: Add proper icon
        if (advanced)
        {
            var iconRect = inRect.TakeRightPart(WidgetRow.IconSize);
            if (Verse.Widgets.ButtonImage(iconRect.CenteredVertically(WidgetRow.IconSize), TexButton.Add))
                FloatWindow.ToggleState<FloatWindow_NamePawn>(iconRect);
        }

        var thirdWidth = inRect.width / 3f;
        var rect1 = inRect.TakeLeftPart(thirdWidth);
        switch (pawn.Name)
        {
            case NameTriple triple:
            {
                var rect2 = inRect.TakeLeftPart(thirdWidth);
                var rect3 = inRect.TakeLeftPart(thirdWidth);
                var first = triple.First;
                var nick = triple.Nick;
                var last = triple.Last;
                CharacterCardUtility.DoNameInputRect(rect1, ref first, 12);
                if (triple.Nick == triple.First || triple.Nick == triple.Last) GUI.color = new Color(1f, 1f, 1f, 0.5f);
                CharacterCardUtility.DoNameInputRect(rect2, ref nick, 16);
                GUI.color = Color.white;
                CharacterCardUtility.DoNameInputRect(rect3, ref last, 12);
                if (triple.First != first || triple.Nick != nick || triple.Last != last)
                    pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);

                TooltipHandler.TipRegionByKey(rect2, "ShortIdentifierDesc");
                TooltipHandler.TipRegionByKey(rect3, "LastNameDesc");
                break;
            }
            case NameSingle single:
            {
                var first = single.ToStringFull;
                CharacterCardUtility.DoNameInputRect(rect1, ref first, 16);
                if (pawn.Name.ToStringFull != first) pawn.Name = new NameSingle(first);
                break;
            }
            default:
                Verse.Widgets.Label(rect1, pawn.Name.ToStringFull);
                break;
        }

        TooltipHandler.TipRegionByKey(rect1, "FirstNameDesc");
    }



    public static void DoFavColorInputRect(Rect inRect, Pawn pawn)
    {
        inRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        var oldColor = pawn.story.favoriteColor?.color ?? Color.white;
        var favColorRect = inRect.TakeRightPart(WidgetRow.IconSize).CenteredVertically(WidgetRow.IconSize);

        Verse.Widgets.DrawLightHighlight(favColorRect);
        favColorRect = favColorRect.ContractedBy(2f);
        Verse.Widgets.DrawRectFast(favColorRect, pawn.story.favoriteColor?.color ?? Color.white);
        inRect.xMax -= 2f;
        if (Verse.Widgets.ButtonText(inRect, "Choose color"))
            Find.WindowStack.Add(new Dialog_ColorPicker(c => pawn.story.favoriteColor?.color = c, oldColor));
    }
}