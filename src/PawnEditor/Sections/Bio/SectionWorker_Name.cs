using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Name(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return p is { Faction: not null, Name: not null } && base.ShowSection(p);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var isHuman = PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike;
        var nameRect = listing.RectLabeled("Name");
        DoNameInputRect(nameRect, pawn, isHuman);
    }

    // REF: CharacterCardUtility.DrawCharacterCard
    private static void DoNameInputRect(Rect inRect, Pawn pawn, bool advanced = false)
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
}