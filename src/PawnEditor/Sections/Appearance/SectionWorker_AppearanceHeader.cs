using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_AppearanceHeader(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        var contentRect = listing.GetRect(200f);
        var width = contentRect.width;
        for (var index = 0; index < 3; ++index)
        {
            var position = contentRect.TakeLeftPart(width / 3);
            var image = PortraitsCache.Get(pawn, new Vector2(position.width, position.height), new Rot4(2 - index),
                Dialog_StylingStation.PortraitOffset, 1.1f,
                renderHeadgear: Window_Editor.ShowHeadgear,
                renderClothes: Window_Editor.ShowClothes);
            GUI.DrawTexture(position, image);
        }

        const string headgear = "Show headgear";
        const string clothing = "Show clothing";

        var footerRect = listing.GetRect(UIUtility.ButtonHeight);
        Verse.Widgets.CheckboxLabeled(
            footerRect.TakeLeftPart(headgear.GetWidthCached() + UIUtility.ButtonPadding),
            headgear, ref Window_Editor.ShowHeadgear);
        footerRect.xMin += UIUtility.LabelPadding;
        Verse.Widgets.CheckboxLabeled(
            footerRect.TakeLeftPart(clothing.GetWidthCached() + UIUtility.ButtonPadding),
            clothing, ref Window_Editor.ShowClothes);
    }
}