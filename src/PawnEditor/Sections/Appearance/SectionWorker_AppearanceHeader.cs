using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_AppearanceHeader : SectionWorker
{
    public SectionWorker_AppearanceHeader(SectionDef def) : base(def)
    {
    }

    protected override void DoSectionContents(ref Rect inRect, Pawn pawn)
    {
        var width = inRect.width;
        var contentRect = inRect.TakeTopPart(200f);
        for (int index = 0; index < 3; ++index)
        {
            Rect position = contentRect.TakeLeftPart(width / 3);
            RenderTexture image = PortraitsCache.Get(pawn, new Vector2(position.width, position.height), new Rot4(2 - index), Dialog_StylingStation.PortraitOffset,
                1.1f, renderHeadgear: Window_Editor.showHeadgear, renderClothes: Window_Editor.showClothes);
            GUI.DrawTexture(position, image);
        }
        
        var headgear = "Show headgear";
        var clothing = "Show clothing";
        
        Rect footerRect = inRect.TakeTopPart(UIUtility.ButtonHeight);
        Widgets.CheckboxLabeled(footerRect.TakeLeftPart(headgear.GetWidthCached() + UIUtility.ButtonPadding), headgear, ref Window_Editor.showHeadgear);
        inRect.xMin += UIUtility.LabelPadding;
        Widgets.CheckboxLabeled(footerRect.TakeLeftPart(clothing.GetWidthCached() + UIUtility.ButtonPadding), clothing, ref Window_Editor.showClothes);
    }
}