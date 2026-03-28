using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Portrait(SectionDef def) : SectionWorker(def)
{
    private const float portraitWidth = 200f;

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        col.Item(height: 200f, draw: r =>
        {
            var width = r.width;
            var cols = Mathf.FloorToInt(width / portraitWidth);

            for (var index = 0; index < cols; ++index)
            {
                var position = r.TakeLeftPart(width / cols);
                var image = PortraitsCache.Get(pawn, new Vector2(position.width, position.height), new Rot4(2 - index),
                    Dialog_StylingStation.PortraitOffset, 1.1f,
                    renderHeadgear: Window_Editor.ShowHeadgear,
                    renderClothes: Window_Editor.ShowClothes);
                GUI.DrawTexture(position, image);
            }
        });
    }
}
