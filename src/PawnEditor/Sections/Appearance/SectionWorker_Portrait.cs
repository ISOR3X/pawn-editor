using RimWorld;
using UnityEngine;
using Verse;
using Void;

namespace PawnEditor;

public class SectionWorker_Portrait(SectionDef def) : SectionWorker(def)
{
    private const float PortraitWidth = 200f;
    private readonly int idx = 0;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Item(r =>
        {
            var image = PortraitsCache.Get(pawn, new Vector2(r.width, r.height), new Rot4(2 - idx),
                Dialog_StylingStation.PortraitOffset, 1.1f,
                renderHeadgear: Window_Editor.ShowHeadgear,
                renderClothes: Window_Editor.ShowClothes);
            GUI.DrawTexture(r, image);
        }, new StyleOverride { width = PortraitWidth, height = PortraitWidth });
    }
}